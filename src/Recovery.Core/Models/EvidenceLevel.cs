namespace Recovery.Core.Models;

/// <summary>
/// Defines the level of confidence for discovered beatmaps per PRD Section 8.
/// </summary>
public enum EvidenceLevel
{
    /// <summary>
    /// Item cannot be matched with an available beatmap set.
    /// </summary>
    Unresolved = 0,

    /// <summary>
    /// Log, replay filename, collection reference, or local manifest.
    /// </summary>
    ProbableLocal = 1,

    /// <summary>
    /// Readable metadata from a copied/backup client.realm or exported artefact.
    /// </summary>
    ConfirmedLocal = 2,

    /// <summary>
    /// Server-side most played or recent score record from the official osu! API.
    /// </summary>
    ConfirmedOnline = 3
}
