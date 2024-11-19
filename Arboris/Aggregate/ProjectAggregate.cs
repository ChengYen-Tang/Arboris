using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using Arboris.Repositories;
using FluentResults;
using System.Text;

namespace Arboris.Aggregate;

public class ProjectAggregate(ICxxRepository cxxRepository, IProjectRepository projectRepository)
{
    public async Task<Result<ProjectReport[]>> GetReportAsync(Guid id)
    {
        Result<GetProject> projectResult = await projectRepository.GetProjectAsync(id);
        if (projectResult.IsFailed)
            return projectResult.ToResult();
        NodeInfoWithLocation[] nodes = await cxxRepository.GetNodesFromProjectAsync(id);
        ProjectReport[] report = nodes.AsParallel().Select(node =>
        {
            string? className = cxxRepository.GetClassFromNodeAsync(node.Id).Result;

            StringBuilder sb = new();
            if (!string.IsNullOrEmpty(node.NameSpace))
                sb.Append(node.NameSpace).Append("::");
            if (!string.IsNullOrEmpty(className))
                sb.Append(className).Append("::");
            sb.Append(node.Spelling);

            List<string> relativePaths = [];

            if (node.DefineLocation is not null)
                relativePaths.Add(node.DefineLocation!.FilePath);

            if (node.ImplementationLocation is not null)
                relativePaths.Add(node.ImplementationLocation!.FilePath);

            string? description = string.IsNullOrWhiteSpace(node.UserDescription) ? node.LLMDescription : node.UserDescription;
            return new ProjectReport(node.Id, node.VcProjectName, sb.ToString(), node.Spelling, node.CxType, className, node.NameSpace, description, [.. relativePaths]);
        }).ToArray();

        return report;
    }
}
