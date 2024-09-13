namespace Arboris;

internal static class Utils
{
    /// <summary>
    /// Searches for files with specified extensions in a directory and its subdirectories.
    /// </summary>
    /// <param name="directoryPath">The root directory to start the search from.</param>
    /// <param name="extensions">An array of file extensions to search for.</param>
    /// <returns>A list of file paths that match the specified extensions.</returns>
    public static List<string> GetFilesWithExtensions(string directoryPath, string[] extensions, string[]? excludePaths = null)
    {
        List<string> files = [];

        try
        {
            foreach (string extension in extensions)
                files.AddRange(Directory.GetFiles(directoryPath, extension, SearchOption.AllDirectories));

            if (excludePaths is not null && excludePaths.Length > 0)
            {
                excludePaths = excludePaths.Select(path => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).ToArray();
                string[][] excludePathPartsList = excludePaths.Select(excludePath => excludePath.Split(Path.DirectorySeparatorChar)).ToArray();
                files.RemoveAll(filePath =>
                {
                    string normalizedFilePath = filePath.Replace('/', Path.DirectorySeparatorChar)
                                                        .Replace('\\', Path.DirectorySeparatorChar);

                    string[] filePathParts = normalizedFilePath.Split(Path.DirectorySeparatorChar);

                    foreach (string[] excludePathParts in excludePathPartsList)
                    {
                        for (int i = 0; i <= filePathParts.Length - excludePathParts.Length; i++)
                        {
                            bool match = true;
                            for (int j = 0; j < excludePathParts.Length; j++)
                            {
                                if (!filePathParts[i + j].Equals(excludePathParts[j], StringComparison.OrdinalIgnoreCase))
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                });
            }
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied to {directoryPath}: {e.Message}");
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine($"Directory not found: {directoryPath}: {e.Message}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"I/O error occurred: {e.Message}");
        }

        return files;
    }

    /// <summary>
    /// Recursively searches for directories containing files with specified wildcard extensions.
    /// </summary>
    /// <param name="directoryPath">The root directory to start the search from.</param>
    /// <param name="extensions">An array of wildcard file extensions to search for.</param>
    /// <returns>A list of directory paths that contain files matching the specified wildcard extensions.</returns>
    public static HashSet<string> GetDirectoriesWithFiles(string directoryPath, string[] extensions)
    {
        HashSet<string> directories = [];

        try
        {
            foreach (string extension in extensions)
            {
                // Get all files matching the current extension in the directory and its subdirectories
                string[] files = Directory.GetFiles(directoryPath, extension, SearchOption.AllDirectories);

                // Add the directory of each matching file to the HashSet (to avoid duplicates)
                foreach (string file in files)
                    directories.Add(Path.GetDirectoryName(file)!);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied to {directoryPath}: {e.Message}");
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine($"Directory not found: {directoryPath}: {e.Message}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"I/O error occurred: {e.Message}");
        }

        return directories;
    }
}
