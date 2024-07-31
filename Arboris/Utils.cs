namespace Arboris;

internal static class Utils
{
    /// <summary>
    /// Searches for files with specified extensions in a directory and its subdirectories.
    /// </summary>
    /// <param name="directoryPath">The root directory to start the search from.</param>
    /// <param name="extensions">An array of file extensions to search for.</param>
    /// <returns>A list of file paths that match the specified extensions.</returns>
    public static List<string> GetFilesWithExtensions(string directoryPath, string[] extensions)
    {
        List<string> files = [];

        try
        {
            foreach (string extension in extensions)
                files.AddRange(Directory.GetFiles(directoryPath, extension, SearchOption.AllDirectories));
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
