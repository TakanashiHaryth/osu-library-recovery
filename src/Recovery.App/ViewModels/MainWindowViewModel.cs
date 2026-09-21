using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Recovery.Core.Models;
using Recovery.Core.Services;
using Recovery.Downloads;
using Recovery.OsuApi;
using Recovery.Packaging;
using Recovery.Local;

namespace Recovery.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private List<BeatmapSet> _rawDiscoveredSets = new();
    private CancellationTokenSource? _recoveryCts;
    private readonly RecoveryQueueManager _queueManager = new();
    private readonly HashSet<int> _installedSetIds = new();
    private readonly LocalLibraryScanner _localScanner = new();
    private readonly LocalArtefactScanner _artefactScanner = new();

    [ObservableProperty]
    private string _username = "Haryth";

    [ObservableProperty]
    private string _clientId = "";

    [ObservableProperty]
    private string _clientSecret = "";

    [ObservableProperty]
    private bool _useMockData = false;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _selectedRulesetIndex = 0; // 0=All, 1=osu!, 2=Taiko, 3=Catch, 4=Mania

    [ObservableProperty]
    private string _selectedStatus = "All";

    [ObservableProperty]
    private int _minPlayCount = 0;

    [ObservableProperty]
    private bool _hideInstalled = false;

    [ObservableProperty]
    private string _statusMessage = "Ready. Enter an account username and click Discover.";

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private string _statsSummary = "No beatmaps discovered yet.";

    [ObservableProperty]
    private string _sizeEstimateSummary = "Est. Download: 0 MB";

    // Local Storage & Target Client Options
    public ObservableCollection<string> ClientTypeOptions { get; } = new()
    {
        "osu!lazer",
        "osu! (Stable)"
    };

    [ObservableProperty]
    private int _selectedClientTypeIndex = 0; // 0=lazer, 1=stable

    [ObservableProperty]
    private string _storagePathLabel = "osu!lazer storage";

    [ObservableProperty]
    private string _storagePathPlaceholder = OperatingSystem.IsWindows() ? "Storage folder path" : "e.g. ~/.local/share/osu or Flatpak";

    [ObservableProperty]
    private string _osuStoragePath = "";

    [ObservableProperty]
    private string _localScanStatus = "Not scanned yet";

    [ObservableProperty]
    private bool _isScanningLocal = false;

    // Recovery Options
    [ObservableProperty]
    private int _selectedOutputModeIndex = 0; // 0=Folder, 1=ZIP, 2=AutoImport

    [ObservableProperty]
    private string _destinationPath = "";

    [ObservableProperty]
    private int _globalVariantIndex = 0; // 0=Full, 1=No Video

    [ObservableProperty]
    private bool _isRecovering = false;

    [ObservableProperty]
    private double _recoveryProgress = 0;

    [ObservableProperty]
    private string _recoveryProgressText = "Ready for recovery.";

    [ObservableProperty]
    private int _totalBeatmaps;

    [ObservableProperty]
    private int _installedCount;

    [ObservableProperty]
    private int _missingCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _hasDisplayedItems;

    public ObservableCollection<BeatmapItemViewModel> DisplayedItems { get; } = new();

    public ObservableCollection<string> StatusOptions { get; } = new()
    {
        "All", "ranked", "loved", "graveyard"
    };

    public ObservableCollection<string> RulesetOptions { get; } = new()
    {
        "All Rulesets", "osu!", "osu!taiko", "osu!catch", "osu!mania"
    };

    public ObservableCollection<string> OutputModeOptions { get; } = new()
    {
        "Download to folder",
        "ZIP recovery bundle",
        "Auto-import to osu!lazer"
    };

    public ObservableCollection<string> VariantOptions { get; } = new()
    {
        "Full (with Video)",
        "No Video"
    };

    public bool IsDestinationEditable => SelectedOutputModeIndex != 2;

    public string SafeModeDescription => SelectedClientTypeIndex == 1
        ? "Safe mode: direct .osz handoff into osu! Songs directory (zero risk to osu!.db)."
        : "Safe mode: no direct writes to client.realm or hashed storage.";

    public MainWindowViewModel()
    {
        OsuStoragePath = OsuStorageLocator.TryFindOsuStorageDirectory() ?? OsuStorageLocator.GetDefaultOsuDirectory();
        OutputModeOptions[2] = SelectedClientTypeIndex == 1 ? "Auto-import to osu! (Stable)" : "Auto-import to osu!lazer";
        UpdateDefaultDestinationPath();
    }

    private void UpdateDefaultDestinationPath()
    {
        var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var clientName = SelectedClientTypeIndex == 1 ? "osu! (Stable)" : "osu!lazer";
        var clientTarget = SelectedClientTypeIndex == 1 ? "Songs/ folder" : "lazer client";
        DestinationPath = SelectedOutputModeIndex switch
        {
            1 => Path.Combine(downloadsFolder, "osu_recovery_bundle.zip"),
            2 => $"({clientName} Auto-Import -> {clientTarget})",
            _ => Path.Combine(downloadsFolder, "OsuRecovered")
        };
    }

    partial void OnSelectedOutputModeIndexChanged(int value)
    {
        UpdateDefaultDestinationPath();
        OnPropertyChanged(nameof(IsDestinationEditable));
    }

    partial void OnSelectedClientTypeIndexChanged(int value)
    {
        if (value == 1)
        {
            StoragePathLabel = OperatingSystem.IsWindows() ? "osu! Stable folder" : "osu! Stable (Wine/Proton)";
            StoragePathPlaceholder = OperatingSystem.IsWindows() ? "e.g. %LOCALAPPDATA%/osu!" : "e.g. ~/.wine/drive_c/osu! or Songs/";
            OsuStoragePath = OsuStorageLocator.TryFindOsuStableDirectory() ?? OsuStorageLocator.GetDefaultOsuStableDirectory();
            LocalScanStatus = "Target client set to osu! (Stable). Ready to scan Songs.";
            OutputModeOptions[2] = "Auto-import to osu! (Stable)";
        }
        else
        {
            StoragePathLabel = "osu!lazer storage";
            StoragePathPlaceholder = OperatingSystem.IsWindows() ? "Storage folder path" : "e.g. ~/.local/share/osu or Flatpak";
            OsuStoragePath = OsuStorageLocator.TryFindOsuStorageDirectory() ?? OsuStorageLocator.GetDefaultOsuDirectory();
            LocalScanStatus = "Target client set to osu!lazer. Ready to scan files.";
            OutputModeOptions[2] = "Auto-import to osu!lazer";
        }
        UpdateDefaultDestinationPath();
        OnPropertyChanged(nameof(SafeModeDescription));
    }

    [RelayCommand]
    public async Task DiscoverAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            StatusMessage = "Please enter an osu! username or user ID.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Starting discovery...";

        try
        {
            IOsuApiClient client;

            if (UseMockData)
            {
                client = new MockOsuApiClient();
            }
            else
            {
                var liveClient = new OsuApiClient();
                if (int.TryParse(ClientId, out int cid) && !string.IsNullOrWhiteSpace(ClientSecret))
                {
                    StatusMessage = "Authenticating with osu! API v2...";
                    var authSuccess = await liveClient.AuthenticateAsync(cid, ClientSecret);
                    if (!authSuccess)
                    {
                        StatusMessage = "Failed to authenticate with osu! API v2. Falling back to public web resolution...";
                    }
                }
                client = liveClient;
            }

            var progress = new Progress<string>(msg => StatusMessage = msg);
            var selectedRulesets = GetSelectedRulesets();

            _rawDiscoveredSets = await client.DiscoverPlayerLibraryAsync(Username, selectedRulesets, progress);

            // Sync installed status with any previously scanned local set IDs
            foreach (var set in _rawDiscoveredSets)
            {
                set.IsInstalled = _installedSetIds.Contains(set.SetId);
            }

            ApplyFilter();
            UpdateStats();
            StatusMessage = $"Discovery completed: {_rawDiscoveredSets.Count} beatmap sets discovered.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discovery failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task StartRecoveryAsync()
    {
        var selectedSets = _rawDiscoveredSets.Where(s => s.IsSelected).ToList();
        if (selectedSets.Count == 0)
        {
            StatusMessage = "No beatmaps selected. Please select at least one set.";
            return;
        }

        OutputMode mode = SelectedOutputModeIndex switch
        {
            1 => OutputMode.ZipBundle,
            2 => OutputMode.AutoImport,
            _ => OutputMode.DownloadFolder
        };

        if (mode != OutputMode.AutoImport && string.IsNullOrWhiteSpace(DestinationPath))
        {
            StatusMessage = "Please specify a destination folder or file path.";
            return;
        }

        IsRecovering = true;
        _recoveryCts = new CancellationTokenSource();
        var variant = GlobalVariantIndex == 1 ? DownloadVariant.NoVideo : DownloadVariant.Full;

        var progress = new Progress<QueueProgress>(p =>
        {
            RecoveryProgress = p.Percentage;
            RecoveryProgressText = $"{p.StatusMessage} ({p.CompletedCount}/{p.TotalCount})";
            StatusMessage = p.StatusMessage;

            // Refresh status of displayed item rows
            foreach (var item in DisplayedItems)
            {
                item.RefreshItemStatus();
            }
        });

        try
        {
            StatusMessage = $"Running recovery for {selectedSets.Count} sets in mode: {OutputModeOptions[SelectedOutputModeIndex]}...";
            await _queueManager.RunRecoveryAsync(
                selectedSets,
                mode,
                DestinationPath,
                Username,
                variant,
                progress,
                _recoveryCts.Token,
                targetClient: SelectedClientTypeIndex == 1 ? TargetClient.Stable : TargetClient.Lazer,
                clientStoragePath: OsuStoragePath
            );

            // Refresh rows after completion
            foreach (var item in DisplayedItems)
            {
                item.RefreshItemStatus();
            }

            StatusMessage = $"Recovery finished: {selectedSets.Count(s => s.RecoveryStatus == RecoveryItemStatus.Completed)} completed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Recovery cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Recovery error: {ex.Message}";
        }
        finally
        {
            IsRecovering = false;
            _recoveryCts = null;
        }
    }

    [RelayCommand]
    public void CancelRecovery()
    {
        if (_recoveryCts != null && !_recoveryCts.IsCancellationRequested)
        {
            StatusMessage = "Cancelling recovery...";
            _recoveryCts.Cancel();
        }
    }

    [RelayCommand]
    public async Task ScanLocalLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(OsuStoragePath) || !Directory.Exists(OsuStoragePath))
        {
            LocalScanStatus = "Directory not found. Please verify the storage folder path.";
            return;
        }

        IsScanningLocal = true;
        var clientName = SelectedClientTypeIndex == 1 ? "osu! (Stable)" : "osu!lazer";
        LocalScanStatus = $"Scanning local {clientName} library (read-only)...";
        var progress = new Progress<string>(msg => LocalScanStatus = msg);

        try
        {
            var result = SelectedClientTypeIndex == 1
                ? await _localScanner.ScanStableLibraryAsync(OsuStoragePath, progress)
                : await _localScanner.ScanInstalledLibraryAsync(OsuStoragePath, progress);

            _installedSetIds.Clear();
            foreach (var id in result.InstalledSetIds)
            {
                _installedSetIds.Add(id);
            }

            // Mark installed flag on discovered sets
            foreach (var set in _rawDiscoveredSets)
            {
                set.IsInstalled = _installedSetIds.Contains(set.SetId);
            }

            ApplyFilter();
            UpdateStats();
            LocalScanStatus = $"Found {result.InstalledSetIds.Count} installed sets in {clientName} library.";
        }
        catch (Exception ex)
        {
            LocalScanStatus = $"Local scan failed: {ex.Message}";
        }
        finally
        {
            IsScanningLocal = false;
        }
    }

    [RelayCommand]
    public async Task ScanLocalArtefactsAsync()
    {
        if (string.IsNullOrWhiteSpace(OsuStoragePath) || !Directory.Exists(OsuStoragePath))
        {
            LocalScanStatus = "Directory not found. Please verify the osu! storage folder.";
            return;
        }

        IsScanningLocal = true;
        LocalScanStatus = "Scanning logs & artefacts for recoverable maps...";
        var progress = new Progress<string>(msg => LocalScanStatus = msg);

        try
        {
            var logsDir = Path.Combine(OsuStoragePath, "logs");
            var logSets = await _artefactScanner.ScanLogsAsync(logsDir, progress);

            if (logSets.Count > 0)
            {
                var combined = new List<BeatmapSet>(_rawDiscoveredSets);
                combined.AddRange(logSets);
                _rawDiscoveredSets = BeatmapSetDeduplicator.Deduplicate(combined);

                foreach (var set in _rawDiscoveredSets)
                {
                    set.IsInstalled = _installedSetIds.Contains(set.SetId);
                }

                ApplyFilter();
                UpdateStats();
                LocalScanStatus = $"Artefacts scan: merged {logSets.Count} sets from logs.";
            }
            else
            {
                LocalScanStatus = "No new beatmap sets found in logs.";
            }
        }
        catch (Exception ex)
        {
            LocalScanStatus = $"Artefacts scan failed: {ex.Message}";
        }
        finally
        {
            IsScanningLocal = false;
        }
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        Ruleset? rulesetFilter = SelectedRulesetIndex switch
        {
            1 => Ruleset.Osu,
            2 => Ruleset.Taiko,
            3 => Ruleset.Catch,
            4 => Ruleset.Mania,
            _ => null
        };

        var criteria = new FilterCriteria(
            SearchText: SearchText,
            SelectedRuleset: rulesetFilter,
            Status: SelectedStatus,
            MinPlayCount: MinPlayCount > 0 ? MinPlayCount : null,
            HideInstalled: HideInstalled ? true : null
        );

        var filtered = BeatmapFilter.ApplyFilter(_rawDiscoveredSets, criteria);

        DisplayedItems.Clear();
        foreach (var set in filtered)
        {
            DisplayedItems.Add(new BeatmapItemViewModel(set, UpdateStats));
        }

        HasDisplayedItems = DisplayedItems.Count > 0;

        UpdateStats();
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var item in DisplayedItems)
        {
            item.IsSelected = true;
        }
        UpdateStats();
    }

    [RelayCommand]
    public void SelectMissingOnly()
    {
        foreach (var item in DisplayedItems)
        {
            item.IsSelected = !item.IsInstalled;
        }
        UpdateStats();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        foreach (var item in DisplayedItems)
        {
            item.IsSelected = false;
        }
        UpdateStats();
    }

    [RelayCommand]
    public void InvertSelection()
    {
        foreach (var item in DisplayedItems)
        {
            item.IsSelected = !item.IsSelected;
        }
        UpdateStats();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedRulesetIndex = 0;
        SelectedStatus = "All";
        MinPlayCount = 0;
        HideInstalled = false;
        ApplyFilter();
    }

    [RelayCommand]
    public void ExportJsonManifest()
    {
        var selectedSets = _rawDiscoveredSets.Where(s => s.IsSelected).ToList();
        if (selectedSets.Count == 0)
        {
            StatusMessage = "No sets selected for manifest export.";
            return;
        }

        var json = ManifestGenerator.GenerateJsonManifest(selectedSets, Username);
        var fileName = $"manifest_{Sanitize(Username)}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var exportDir = GetSafeExportDirectory();
        var outPath = Path.Combine(exportDir, fileName);

        File.WriteAllText(outPath, json);
        StatusMessage = $"Saved manifest.json to {Path.GetFileName(exportDir)}: {fileName}";
    }

    [RelayCommand]
    public void ExportCsvManifest()
    {
        var selectedSets = _rawDiscoveredSets.Where(s => s.IsSelected).ToList();
        if (selectedSets.Count == 0)
        {
            StatusMessage = "No sets selected for manifest export.";
            return;
        }

        var csv = ManifestGenerator.GenerateCsvManifest(selectedSets);
        var fileName = $"manifest_{Sanitize(Username)}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var exportDir = GetSafeExportDirectory();
        var outPath = Path.Combine(exportDir, fileName);

        File.WriteAllText(outPath, csv);
        StatusMessage = $"Saved manifest.csv to {Path.GetFileName(exportDir)}: {fileName}";
    }

    private static string GetSafeExportDirectory()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
        {
            return desktop;
        }

        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (Directory.Exists(downloads))
        {
            return downloads;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private IEnumerable<Ruleset> GetSelectedRulesets()
    {
        return SelectedRulesetIndex switch
        {
            1 => new[] { Ruleset.Osu },
            2 => new[] { Ruleset.Taiko },
            3 => new[] { Ruleset.Catch },
            4 => new[] { Ruleset.Mania },
            _ => new[] { Ruleset.Osu, Ruleset.Taiko, Ruleset.Catch, Ruleset.Mania }
        };
    }

    private void UpdateStats()
    {
        int total = _rawDiscoveredSets.Count;
        int selected = _rawDiscoveredSets.Count(s => s.IsSelected);
        int visible = DisplayedItems.Count;
        int installed = _rawDiscoveredSets.Count(s => s.IsInstalled);
        int missing = Math.Max(0, total - installed);

        TotalBeatmaps = total;
        InstalledCount = installed;
        MissingCount = missing;
        SelectedCount = selected;

        // Estimate size: Full is ~15 MB, NoVideo is ~8 MB average per set
        double avgSetMb = GlobalVariantIndex == 1 ? 8.0 : 15.0;
        double estMb = selected * avgSetMb;

        StatsSummary = $"Total Discovered: {total} | Installed: {installed} | Missing: {missing} | Visible: {visible} | Selected: {selected}";
        SizeEstimateSummary = selected > 0
            ? $"Est. Size: ~{estMb:N0} MB ({selected} sets)"
            : "Est. Size: 0 MB";
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedRulesetIndexChanged(int value) => ApplyFilter();
    partial void OnSelectedStatusChanged(string value) => ApplyFilter();
    partial void OnMinPlayCountChanged(int value) => ApplyFilter();
    partial void OnHideInstalledChanged(bool value) => ApplyFilter();
    partial void OnGlobalVariantIndexChanged(int value) => UpdateStats();
}
