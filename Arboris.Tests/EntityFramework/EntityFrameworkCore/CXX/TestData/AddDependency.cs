using Arboris.EntityFramework.EntityFrameworkCore.CXX;

namespace Arboris.Tests.EntityFramework.CXX.TestData;

public partial class GenerateBuilder
{
    public GenerateBuilder AddDependency1()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Dependency");

        Node node = GenerateDependencyNode();
        NodeDependency nodeDependency = new()
        {
            Node = Nodes[0],
            From = node
        };

        db.Cxx_NodeDependencies.Add(nodeDependency);

        return this;
    }

    public GenerateBuilder AddDependency2()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Dependency");

        db.SaveChanges();
        Node node = GenerateDependencyNode();
        NodeDependency nodeDependency = new()
        {
            NodeId = Nodes[0].Id,
            From = node
        };

        Node rootNode = db.Cxx_Nodes.Find(Nodes[0].Id)!;
        rootNode.Dependencies.Add(nodeDependency);
        db.Cxx_Nodes.Update(Nodes[0]);

        return this;
    }
}
