using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Recovery.Core.Models;

namespace Recovery.App.ViewModels;

public partial class BeatmapItemViewModel : ObservableObject
{
    private static readonly HttpClient ThumbnailClient = CreateThumbnailClient();
    private static readonly ConcurrentDictionary<int, Task<byte[]?>> ThumbnailCache = new();
    private readonly Action? _selectionChanged;

    public BeatmapSet Model { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private DownloadVariant _requestedVariant;

    [ObservableProperty]
    private RecoveryItemStatus _itemStatus = RecoveryItemStatus.Pending;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private bool _isThumbnailLoaded;

    public int SetId => Model.SetId;
    public string Title => Model.Title;
    public string Artist => Model.Artist;
    public string Mapper => Model.Mapper;
    public string Status => Model.Status;
    public bool HasVideo => Model.HasVideo;
    public int TotalPlayCount => Model.TotalPlayCount;
    public string EvidenceLevel => Model.OverallEvidenceLevel.ToString();
    public string OfficialUrl => Model.OfficialUrl;
    public string SetIdDisplay => $"#{Model.SetId}";
    public string StatusDisplay => ToDisplayText(Model.Status);
    public string EvidenceDisplay => Model.OverallEvidenceLevel switch
    {
        Recovery.Core.Models.EvidenceLevel.ConfirmedOnline => "Online",
        Recovery.Core.Models.EvidenceLevel.ConfirmedLocal => "Local",
        Recovery.Core.Models.EvidenceLevel.ProbableLocal => "Probable",
        _ => "Unresolved"
    };
    public string RulesetDisplay
    {
        get
        {
            var modes = Model.Difficulties.Select(d => d.RulesetId).Distinct().OrderBy(id => id).Select(id => id switch
            {
                1 => "Taiko",
                2 => "Catch",
                3 => "Mania",
                _ => "osu!"
            });
            return string.Join(", ", modes);
        }
    }
    public string DifficultySummary => Model.Difficulties.Count == 1
        ? "1 difficulty"
        : $"{Model.Difficulties.Count} difficulties";
    public string ThumbnailUrl => $"https://assets.ppy.sh/beatmaps/{SetId}/covers/list@2x.jpg";

    public void RefreshItemStatus()
    {
        ItemStatus = Model.RecoveryStatus;
        IsInstalled = Model.IsInstalled;
    }

    public BeatmapItemViewModel(BeatmapSet model, Action? selectionChanged = null)
    {
        Model = model;
        _selectionChanged = selectionChanged;
        _isSelected = model.IsSelected;
        _requestedVariant = model.RequestedVariant;
        _isInstalled = model.IsInstalled;
        _itemStatus = model.RecoveryStatus;
        _ = LoadThumbnailAsync();
    }

    partial void OnIsSelectedChanged(bool value)
    {
        Model.IsSelected = value;
        _selectionChanged?.Invoke();
    }

    partial void OnRequestedVariantChanged(DownloadVariant value)
    {
        Model.RequestedVariant = value;
    }

    private async Task LoadThumbnailAsync()
    {
        try
        {
            var bytes = await ThumbnailCache.GetOrAdd(SetId, DownloadThumbnailAsync);
            if (bytes is null || bytes.Length == 0)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using var stream = new MemoryStream(bytes);
                Thumbnail = new Bitmap(stream);
                IsThumbnailLoaded = true;
            });
        }
        catch
        {
            // A thumbnail is decorative. Keep the row usable with its fallback tile.
        }
    }

    private static async Task<byte[]?> DownloadThumbnailAsync(int setId)
    {
        using var response = await ThumbnailClient.GetAsync(
            $"https://assets.ppy.sh/beatmaps/{setId}/covers/list@2x.jpg",
            HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
            return null;

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static HttpClient CreateThumbnailClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BeatmapLibraryRecovery", "0.1"));
        return client;
    }

    private static string ToDisplayText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}
