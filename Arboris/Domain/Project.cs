using Arboris.Aggregate;
using Arboris.Models.Graph.CXX;
using FluentResults;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Arboris.Domain;

public class Project(ILogger<Project> logger, CxxAggregate cxxAggregate)
{
    public async Task SyncFromReport(Guid id, string reportFolder)
    {
        string indexFilePath = Path.Combine(reportFolder, "Index.bson");
        if (!File.Exists(indexFilePath))
            logger.LogWarning("Project id: {Id}, Index file not found: {IndexFilePath}", id, indexFilePath);

        Dictionary<string, Dictionary<string, string>>? index = null;
        try
        {
            using FileStream fs = File.OpenRead(indexFilePath);
            using BsonDataReader reader = new(fs) { ReadRootValueAsArray = false };
            JsonSerializer serializer = new();
            index = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(reader);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Project id: {Id}, Error reading index file: {IndexFilePath}", id, indexFilePath);
        }

        if (index is null || index.Count is 0)
        {
            logger.LogWarning("Project id: {Id}, Index file is empty: {IndexFilePath}", id, indexFilePath);
            return;
        }

        // Get all value from index
        string[] values = index.Values.SelectMany(dict => dict.Values).ToArray();
        await Parallel.ForEachAsync(values, async (value, _) =>
        {
            string filePath = Path.Combine(reportFolder, $"{value}.bson");
            if (!File.Exists(filePath))
            {
                logger.LogWarning("Project id: {Id}, File not found: {FilePath}", id, filePath);
                return;
            }

            try
            {
                using FileStream fs = File.OpenRead(filePath);
                using BsonDataReader reader = new(fs) { ReadRootValueAsArray = false };
                JsonSerializer serializer = new();
                CodeInfo? codeInfo = serializer.Deserialize<CodeInfo>(reader);
                if (codeInfo is null)
                {
                    logger.LogWarning("Project id: {Id}, CodeInfo is null: {FilePath}", id, filePath);
                    return;
                }

                // Sync code info
                Result result = await cxxAggregate.UpdateUserDescription(id, codeInfo.VcProjectName, codeInfo.NameSpace, codeInfo.ClassName, codeInfo.Spelling, codeInfo.CxType, codeInfo.Description);
                if (result.IsFailed)
                    logger.LogWarning("Project id: {Id}, Node not found in database: {NameSpace}, {ClassName}, {Spelling}, {CxType}", id, codeInfo.NameSpace, codeInfo.ClassName, codeInfo.Spelling, codeInfo.CxType);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Project id: {Id}, Error process node file: {FilePath}", id, filePath);
            }
        });
    }
}
