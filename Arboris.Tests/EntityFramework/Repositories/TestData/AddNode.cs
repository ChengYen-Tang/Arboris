using Arboris.EntityFramework.EntityFrameworkCore.CXX;

namespace Arboris.Tests.EntityFramework.Repositories.TestData;

public partial class GenerateBuilder
{
    public readonly List<Node> Nodes = [];
    public readonly List<Location> Locations = [];

    public GenerateBuilder GenerateRootNode1()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before RootNode1");

        DefineLocation hLocation = new()
        {
            FilePath = "RootNode1.h",
            StartLine = 1,
            EndLine = 1
        };
        Node node = new()
        {
            CursorKindSpelling = "Class",
            Spelling = "RootNode1",
            ProjectId = Projects[0].Id,
            DefineLocation = hLocation
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(hLocation);
        db.Cxx_DefineLocations.Add(hLocation);

        return this;
    }
}
