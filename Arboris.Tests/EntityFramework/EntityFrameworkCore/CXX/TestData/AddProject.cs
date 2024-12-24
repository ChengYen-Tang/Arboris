namespace Arboris.Tests.EntityFramework.CXX.TestData;

public partial class GenerateBuilder
{
    public readonly List<Project> Projects = [];

    public GenerateBuilder GenerateProject1()
    {
        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            SolutionName = projectId.ToString()
        };
        Projects.Add(project);
        db.Projects.Add(project);

        return this;
    }

    public GenerateBuilder GenerateProject2()
    {
        if (Projects.Count < 1)
            throw new InvalidOperationException("Project1 must be generated before Project2");

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            SolutionName = projectId.ToString()
        };
        Projects.Add(project);
        db.Projects.Add(project);

        return this;
    }
}
