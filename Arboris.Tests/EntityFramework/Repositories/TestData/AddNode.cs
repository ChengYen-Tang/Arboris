using Arboris.Models.Analyze.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

namespace Arboris.Tests.EntityFramework.Repositories.TestData;

public partial class GenerateBuilder
{
    public readonly List<Node> Nodes = [];
    public readonly List<Location> Locations = [];

    public GenerateBuilder GenerateRootNode1()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before RootNode1");
        AddNode addNode = Generator.GenetateAddNodeWithDefineLocation(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateRootNode2()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project2 must be generated before RootNode2");
        AddNode addNode = Generator.GenetateAddNodeWithImplementationLocation(Projects[1].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.AddRange(node.Value.ImplementationsLocation!);

        return this;
    }

    public GenerateBuilder GenerateMemberNode()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before MemberNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before MemberNode");
        AddNode addNode = Generator.GenerateAddNodeWithMemberNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateMemberNode2()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project1 must be generated before MemberNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before MemberNode");
        AddNode addNode = Generator.GenerateAddNodeWithMemberNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateDependencyNode()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before DependencyNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before DependencyNode");
        AddNode addNode = Generator.GenerateAddNodeWithDependencyNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateDependencyNode2()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project1 must be generated before DependencyNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before DependencyNode");
        AddNode addNode = Generator.GenerateAddNodeWithDependencyNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateTypeNode()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before TypeNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before TypeNode");
        AddNode addNode = Generator.GenerateAddNodeWithTypeNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateTypeNode2()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project1 must be generated before TypeNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before TypeNode");
        AddNode addNode = Generator.GenerateAddNodeWithTypeNode(Projects[0].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }

    public GenerateBuilder GenerateTypeNode3()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project1 must be generated before TypeNode");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before TypeNode");
        AddNode addNode = Generator.GenerateAddNodeWithTypeNode(Projects[1].Id);
        Result<Guid> id = repository.AddNodeAsync(addNode).Result;
        Result<Node> node = repository.GetNodeAsync(id.Value).Result;
        Nodes.Add(node.Value);
        Locations.Add(node.Value.DefineLocation!);

        return this;
    }
}
