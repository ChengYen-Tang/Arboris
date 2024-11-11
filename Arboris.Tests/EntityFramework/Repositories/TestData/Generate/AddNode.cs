using Arboris.Models.Analyze.CXX;

namespace Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

internal static partial class Generator
{
    public static AddNode GenetateAddNodeWithDefineLocation(Guid projectId)
    {
        Location location = new("Node1.h", (uint)Random.Shared.Next(), 1)
        {
            DisplayName = "Node1",
            SourceCode = "Type1 Node1;"
        };
        return new(projectId, "Arboris", "ClassDecl", "Node1", "Type1", "Namespace1", location, null);
    }

    public static AddNode GenetateAddNodeWithImplementationLocation(Guid projectId)
    {
        Location location = new("Node2.cpp", (uint)Random.Shared.Next(), 2)
        {
            DisplayName = "Node2",
            SourceCode = "Type2 Node2;"
        };
        return new(projectId, "Arboris", "ClassDecl", "Node2", "Type2", "Namespace2", null, location);
    }

    public static AddNode GenerateAddNodeWithMemberNode(Guid projectId)
    {
        Location location = new("MemberNode.h", (uint)Random.Shared.Next(), 3)
        {
            DisplayName = "MemberNode",
            SourceCode = "Type3 MemberNode;"
        };
        return new(projectId, "Arboris", "FunctionDecl", "MemberNode", "Type3", "Namespace3", location, null);
    }

    public static AddNode GenerateAddNodeWithDependencyNode(Guid projectId)
    {
        Location location = new("DependencyNode.h", (uint)Random.Shared.Next(), 4)
        {
            DisplayName = "DependencyNode",
            SourceCode = "Type4 DependencyNode;"
        };
        return new(projectId, "Arboris", "ClassDecl", "DependencyNode", "Type4", "Namespace4", location, null);
    }

    public static AddNode GenerateAddNodeWithDependencyFunctionDeclNode(Guid projectId)
    {
        Location location = new("DependencyNode.h", (uint)Random.Shared.Next(), 4)
        {
            DisplayName = "DependencyNode",
            SourceCode = "Type4 DependencyNode;"
        };
        return new(projectId, "Arboris", "FunctionDecl", "DependencyNode", "Type4", "Namespace4", location, null);
    }

    public static AddNode GenerateAddNodeWithTypeNode(Guid projectId)
    {
        Location location = new("TypeNode.h", (uint)Random.Shared.Next(), 5)
        {
            DisplayName = "TypeNode",
            SourceCode = "Type5 TypeNode;"
        };
        return new(projectId, "Arboris", "FunctionDecl", "TypeNode", "Type5", "Namespace5", location, null);
    }
}
