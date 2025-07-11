using ClangSharp.Interop;
using Microsoft.Extensions.Logging;

namespace Arboris.Analyze.CXX.Clang;

internal abstract class ScanAbstract
{
    protected readonly ILogger logger;
    protected readonly Dictionary<string, bool> isFilesScaned;
    protected readonly Func<string, string> getRelativePath;
    private readonly HashSet<string> isFilesScanedInScope;

    public ScanAbstract(ILogger logger, Dictionary<string, bool> isFilesScaned, Func<string, string> getRelativePath)
        => (this.logger, this.isFilesScaned, this.getRelativePath, isFilesScanedInScope)
        = (logger, isFilesScaned, getRelativePath, []);

    protected abstract Task Scan(string codeFile, CancellationToken ct = default);

    public async Task ScanFile(string codeFile, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!isFilesScaned.TryGetValue(codeFile, out bool value) || value)
            return;

        isFilesScanedInScope.Clear();
        logger.LogDebug("ScanFile -> CodeFile: {CodeFile}", codeFile);
        await Scan(codeFile, ct);

        isFilesScaned[codeFile] = true;
        foreach (string file in isFilesScanedInScope)
            if (isFilesScaned.ContainsKey(file))
                isFilesScaned[file] = true;
    }

    protected bool CheckIsProjectFileAndNotScanned(CXSourceLocation sourceLocation)
    {
        sourceLocation.GetExpansionLocation(out CXFile file, out uint _, out uint _, out uint _);
        using CXString fileName = file.Name;
        string relativePath = getRelativePath(fileName.ToString());
        if (string.IsNullOrEmpty(relativePath) || !isFilesScaned.TryGetValue(relativePath, out bool value) || value)
            return false;
        isFilesScanedInScope.Add(relativePath);
        return true;
    }
}
