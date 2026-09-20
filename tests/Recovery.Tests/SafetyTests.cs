using Recovery.Import;
using Xunit;

namespace Recovery.Tests;

public class SafetyTests
{
    [Fact]
    public void IsPathSafeFromLiveStore_WhenPathIsInsideLazerFilesDirectory_ReturnsFalse()
    {
        var fakeDataDir = @"C:\Users\Player\AppData\Roaming\osu";
        var install = new LazerInstallation(@"C:\Users\Player\AppData\Local\osulazer\osu!.exe", fakeDataDir, true);
        var handoff = new LazerImportHandoff(install);

        var hazardousPath = @"C:\Users\Player\AppData\Roaming\osu\files\ab\abcdef123456";
        var isSafe = handoff.IsPathSafeFromLiveStore(hazardousPath);

        Assert.False(isSafe);
    }

    [Fact]
    public void IsPathSafeFromLiveStore_WhenPathIsClientRealm_ReturnsFalse()
    {
        var fakeDataDir = @"C:\Users\Player\AppData\Roaming\osu";
        var install = new LazerInstallation(@"C:\Users\Player\AppData\Local\osulazer\osu!.exe", fakeDataDir, true);
        var handoff = new LazerImportHandoff(install);

        var hazardousPath = @"C:\Users\Player\AppData\Roaming\osu\client.realm";
        var isSafe = handoff.IsPathSafeFromLiveStore(hazardousPath);

        Assert.False(isSafe);
    }

    [Fact]
    public void IsPathSafeFromLiveStore_WhenPathIsInUserStagingDirectory_ReturnsTrue()
    {
        var fakeDataDir = @"C:\Users\Player\AppData\Roaming\osu";
        var install = new LazerInstallation(@"C:\Users\Player\AppData\Local\osulazer\osu!.exe", fakeDataDir, true);
        var handoff = new LazerImportHandoff(install);

        var safePath = @"C:\Users\Player\Downloads\RecoveryStaging\1234.osz";
        var isSafe = handoff.IsPathSafeFromLiveStore(safePath);

        Assert.True(isSafe);
    }
}
