using Arboris.EntityFramework.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.Repositories.TestData;

public partial class GenerateBuilder
{
    public readonly List<Project> Projects = [];

    public GenerateBuilder GenerateProject1()
    {
        Project project = new()
        {
            Name = "Project1"
        };
        Projects.Add(project);
        db.Projects.Add(project);

        return this;
    }

    public GenerateBuilder GenerateProject2()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before Project2");

        Project project = new()
        {
            Name = "Project2"
        };
        Projects.Add(project);
        db.Projects.Add(project);

        return this;
    }
}
