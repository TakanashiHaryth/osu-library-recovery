namespace Recovery.Import;

public interface IClientImportHandoff
{
    Task<ImportResult> HandoffOszAsync(string oszFilePath, CancellationToken ct = default);
}
