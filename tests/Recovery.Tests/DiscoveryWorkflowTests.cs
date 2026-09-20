using System.Text.Json;
using Recovery.Core.Models;
using Recovery.Core.Services;
using Recovery.OsuApi;
using Recovery.Packaging;
using Xunit;

namespace Recovery.Tests;

public class DiscoveryWorkflowTests
{
    [Fact]
    public async Task FullDiscoveryWorkflow_FromMockClientToManifestExport_Succeeds()
    {
        // 1. Initialize Client & Discover
        IOsuApiClient client = new MockOsuApiClient();
        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        var discovered = await client.DiscoverPlayerLibraryAsync("PeppyPlayer", new[] { Ruleset.Osu, Ruleset.Taiko }, progress);

        Assert.NotEmpty(discovered);
        Assert.True(discovered.Count >= 5);
        Assert.NotEmpty(progressMessages);

        // 2. Apply Filters (e.g. Ranked only)
        var filtered = BeatmapFilter.ApplyFilter(discovered, new FilterCriteria(Status: "ranked"));
        Assert.True(filtered.Count >= 3);
        Assert.All(filtered, s => Assert.Equal("ranked", s.Status));

        // 3. Selection
        SelectionManager.ClearSelection(discovered);
        SelectionManager.SelectAll(filtered);
        Assert.Equal(filtered.Count, discovered.Count(s => s.IsSelected));

        // 4. Generate JSON Manifest
        var jsonManifest = ManifestGenerator.GenerateJsonManifest(discovered.Where(s => s.IsSelected), "PeppyPlayer");
        Assert.NotNull(jsonManifest);
        using var doc = JsonDocument.Parse(jsonManifest);
        Assert.True(doc.RootElement.TryGetProperty("Username", out var userProp));
        Assert.Equal("PeppyPlayer", userProp.GetString());

        // 5. Generate CSV Manifest
        var csvManifest = ManifestGenerator.GenerateCsvManifest(discovered.Where(s => s.IsSelected));
        Assert.NotNull(csvManifest);
        Assert.StartsWith("SetId,Artist,Title,Mapper,Status", csvManifest);
        Assert.Contains("FREEDOM DiVE", csvManifest);
    }
}
