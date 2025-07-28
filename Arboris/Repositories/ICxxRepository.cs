using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using FluentResults;
using System.Collections.Concurrent;

namespace Arboris.Repositories;

public interface ICxxRepository
{
    /// <summary>
    /// Insert a new node into the database
    /// </summary>
    /// <param name="addNode"> addNode dto </param>
    /// <returns> node id </returns>
    Task<Guid> AddNodeAsync(AddNode addNode);

    /// <summary>
    /// Check if the node + define location exists in the database
    /// </summary>
    /// <param name="addNode"> addNode dto </param>
    /// <returns></returns>
    Task<Result<Guid>> CheckDefineNodeExistsAsync(AddNode addNode);

    /// <summary>
    /// Check if the node + implementation location exists in the database
    /// </summary>
    /// <param name="addNode"> addNode dto </param>
    /// <returns></returns>
    Task<bool> CheckImplementationNodeExistsAsync(AddNode addNode);

    Task<Result<GetAllNodeDto[]>> GetAllNodeAsync(Guid projectId);

    Task<Result<Guid[]>> GetNodeDependenciesIdAsync(Guid nodeId);

    /// <summary>
    /// Get node infos, only class and struct
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <returns></returns>
    Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName);

    /// <summary>
    /// Get node from define location
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="location"> Define location </param>
    /// <returns></returns>
    Task<Result<Node>> GetNodeFromDefineLocationAsync(Guid projectId, string vcProjectName, Location location);
    /// <summary>
    /// Get node from define location
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="location"> Define location </param>
    /// <returns></returns>
    Task<Result<Node>> GetNodeFromDefineLocationCanCrossProjectAsync(Guid projectId, string vcProjectName, Location location);

    /// <summary>
    /// Get all nodes from a project
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <returns></returns>
    Task<NodeInfoWithLocation[]> GetNodesFromProjectAsync(Guid projectId);

    /// <summary>
    /// Get class name from member node id
    /// </summary>
    /// <param name="nodeId"> Member node id </param>
    /// <returns></returns>
    Task<string?> GetClassFromNodeAsync(Guid nodeId);

    /// <summary>
    /// Get node from node id
    /// </summary>
    /// <param name="nodeId"> Node id </param>
    /// <returns></returns>
    Task<Result<Node>> GetNodeAsync(Guid nodeId);

    Task<Result<(string? NameSpace, string? Spelling, string? AccessSpecifiers, Guid? ClassNodeId, string? CursorKindSpelling, bool NeedGenerate, string VcProjectName)>> GetNodeInfoWithClassIdAsync(Guid nodeId);

    Task<Result<NodeSourceCode[]>> GetNodeSourceCodeAsync(Guid nodeId);

    /// <summary>
    /// Link a member to a class or struct
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="classLocation"> Location of class or struct node </param>
    /// <param name="node"> member node </param>
    /// <returns></returns>
    Task LinkMemberAsync(Guid projectId, Models.Analyze.CXX.Location classLocation, ConcurrentDictionary<Guid, IReadOnlyList<string>> node);

    /// <summary>
    /// Link a dependency to a class, struct, function, method, field, etc.
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="fromLocation"> Uesd node location </param>
    /// <returns></returns>
    Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation);

    /// <summary>
    /// Link a dependency to a class, struct
    /// Because clang returns features of operator= under certain conditions that do not meet our requirements.
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="fromLocation"> Uesd node location </param>
    /// <returns></returns>
    Task<Result> LinkDependencyCallExprOperatorEqualAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation);

    /// <summary>
    /// Link a type to a class, struct, function, method, field, etc.
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="typeLocation"> Type node location </param>
    /// <returns></returns>
    Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location typeLocation);

    Task<Result> MoveTypeDeclarationLinkAsync(Guid projectId, NodeInfo nodeInfo);

    /// <summary>
    /// Remove type declarations
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <returns></returns>
    Task<Result> RemoveTypeDeclarations(Guid projectId, string vcProjectName);

    /// <summary>
    /// Update node location(DefineLocation or ImplementationLocation)
    /// </summary>
    /// <param name="node"> Update node location dto </param>
    /// <returns></returns>
    Task<Result> UpdateNodeLocationAsync(NodeWithLocationDto node);

    Task<Result> UpdateNodeAsync(Node node);

    /// <summary>
    /// Update the description of the node, whith the description generated by the LLM
    /// </summary>
    /// <param name="id"> Node id </param>
    /// <param name="description"> Description generated by the LLM </param>
    /// <returns></returns>
    Task<Result> UpdateLLMDescriptionAsync(Guid id, string description);

    /// <summary>
    /// Update the user description of the node
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nameSpace"></param>
    /// <param name="className"></param>
    /// <param name="spelling"></param>
    /// <param name="cxType"></param>
    /// <param name="description"> User description </param>
    /// <returns></returns>
    Task<Result> UpdateUserDescriptionAsync(Guid projectId, string vcProjectName, string? nameSpace, string? className, string? spelling, string? cxType, string? description);

    Task<Result<NodeLines>> GetSourceCodeFromFilePath(Guid projectId, string filePath, int line);
}
