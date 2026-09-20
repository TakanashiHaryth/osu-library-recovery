namespace Recovery.Core.Models;

public enum Ruleset
{
    Osu = 0,
    Taiko = 1,
    Catch = 2,
    Mania = 3
}

public enum OutputMode
{
    ZipBundle = 0,
    DownloadFolder = 1,
    AutoImport = 2,
    ManifestOnly = 3
}

public enum DownloadVariant
{
    Full = 0,
    NoVideo = 1
}

public enum RecoveryItemStatus
{
    Pending = 0,
    Downloading = 1,
    Validating = 2,
    Staged = 3,
    Importing = 4,
    Completed = 5,
    Skipped = 6,
    Unavailable = 7,
    Cancelled = 8,
    Failed = 9
}
