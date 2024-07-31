using Arboris.EntityFramework.EntityFrameworkCore.CXX;

namespace Arboris.Tests.EntityFramework.CXX.TestData;

public partial class GenerateBuilder
{
    public GenerateBuilder AddMember1()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Member");

        Node node = GenerateMemberNode();
        NodeMember nodeMember = new()
        {
            Node = Nodes[0],
            Member = node
        };

        db.Cxx_NodeMembers.Add(nodeMember);

        return this;
    }

    public GenerateBuilder AddMember2()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before Member");

        db.SaveChanges();
        Node node = GenerateMemberNode();
        NodeMember nodeMember = new()
        {
            NodeId = Nodes[0].Id,
            Member = node
        };

        Node rootNode = db.Cxx_Nodes.Find(Nodes[0].Id)!;
        rootNode.Members.Add(nodeMember);
        db.Cxx_Nodes.Update(rootNode);

        return this;
    }
}
