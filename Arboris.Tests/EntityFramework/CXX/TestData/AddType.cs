using Arboris.EntityFramework.EntityFrameworkCore.CXX;

namespace Arboris.Tests.EntityFramework.CXX.TestData;

public partial class GenerateBuilder
{
    public GenerateBuilder AddType1()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Type");

        Node node = GenerateTypeNode();
        NodeType nodeType = new()
        {
            Node = Nodes[0],
            Type = node
        };

        db.Cxx_NodeTypes.Add(nodeType);

        return this;
    }

    public GenerateBuilder AddType2()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Type");

        db.SaveChanges();
        Node node = GenerateTypeNode();
        NodeType nodeType = new()
        {
            NodeId = Nodes[0].Id,
            Type = node
        };

        Node rootNode = db.Cxx_Nodes.Find(Nodes[0].Id)!;
        rootNode.Types.Add(nodeType);
        db.Cxx_Nodes.Update(Nodes[0]);

        return this;
    }
}
