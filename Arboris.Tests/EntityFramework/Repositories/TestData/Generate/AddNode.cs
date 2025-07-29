using Arboris.Models.Analyze.CXX;

namespace Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

internal static partial class Generator
{
    public static AddNode GenetateAddNodeWithDefineLocation(Guid projectId)
    {
        Location location = new("Node1.h", (uint)Random.Shared.Next(), 0, 1, 0)
        {
            DisplayName = new("Node1"),
            SourceCode = new("Type1 Node1;")
        };
        return new(projectId, "Arboris", "ClassDecl", "Node1", "Type1", "Namespace1", null, location, null);
    }

    public static AddNode GenetateAddNodeWithImplementationLocation(Guid projectId)
    {
        Location location = new("Node2.cpp", (uint)Random.Shared.Next(), 0, 2, 0)
        {
            DisplayName = new("Node2"),
            SourceCode = new("Type2 Node2;")
        };
        return new(projectId, "Arboris", "ClassDecl", "Node2", "Type2", "Namespace2", null, null, location);
    }

    public static AddNode GenerateAddNodeWithMemberNode(Guid projectId)
    {
        Location location = new("MemberNode.h", (uint)Random.Shared.Next(), 0, 3, 0)
        {
            DisplayName = new("MemberNode"),
            SourceCode = new("Type3 MemberNode;")
        };
        return new(projectId, "Arboris", "FunctionDecl", "MemberNode", "Type3", "Namespace3", null, location, null);
    }

    public static AddNode GenerateAddNodeWithDependencyNode(Guid projectId)
    {
        Location location = new("DependencyNode.h", (uint)Random.Shared.Next(), 0, 4, 0)
        {
            DisplayName = new("DependencyNode"),
            SourceCode = new("Type4 DependencyNode;")
        };
        return new(projectId, "Arboris", "ClassDecl", "DependencyNode", "Type4", "Namespace4", null, location, null);
    }

    public static AddNode GenerateAddNodeWithDependencyFunctionDeclNode(Guid projectId)
    {
        Location location = new("DependencyNode.h", (uint)Random.Shared.Next(), 0, 4, 0)
        {
            DisplayName = new("DependencyNode"),
            SourceCode = new("Type4 DependencyNode;")
        };
        return new(projectId, "Arboris", "FunctionDecl", "DependencyNode", "Type4", "Namespace4", null, location, null);
    }

    public static AddNode GenerateAddNodeWithTypeNode(Guid projectId)
    {
        Location location = new("TypeNode.h", (uint)Random.Shared.Next(), 0, 5, 0)
        {
            DisplayName = new("TypeNode"),
            SourceCode = new("Type5 TypeNode;")
        };
        return new(projectId, "Arboris", "FunctionDecl", "TypeNode", "Type5", "Namespace5", null, location, null);
    }
}
