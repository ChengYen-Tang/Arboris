﻿using Arboris.EntityFramework.EntityFrameworkCore.CXX;

namespace Arboris.Tests.EntityFramework.CXX.TestData;

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
        ImplementationLocation cppLocation = new()
        {
            FilePath = "RootNode1.cpp",
            StartLine = 2,
            EndLine = 2,
            Node = node
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(hLocation);
        Locations.Add(cppLocation);
        db.Cxx_DefineLocations.Add(hLocation);
        db.Cxx_ImplementationLocations.Add(cppLocation);

        return this;
    }

    public GenerateBuilder GenerateRootNode2()
    {
        if (Projects.Count < 2)
            throw new InvalidOperationException("Project2 must be generated before RootNode2");
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before RootNode2");

        DefineLocation hLocation = new()
        {
            FilePath = "RootNode2.h",
            StartLine = 1,
            EndLine = 1
        };
        Node node = new()
        {
            CursorKindSpelling = "Class",
            Spelling = "RootNode2",
            Project = Projects[1],
            DefineLocation = hLocation
        };
        ImplementationLocation cppLocation = new()
        {
            FilePath = "RootNode2.cpp",
            StartLine = 2,
            EndLine = 2,
            Node = node
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(hLocation);
        Locations.Add(cppLocation);
        db.Cxx_DefineLocations.Add(hLocation);
        db.Cxx_ImplementationLocations.Add(cppLocation);

        return this;
    }

    private Node GenerateMemberNode()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before MemberNode");

        ImplementationLocation cppLocation = new()
        {
            FilePath = "MemberNode.cpp",
            StartLine = 1,
            EndLine = 1
        };
        Node node = new()
        {
            CursorKindSpelling = "Function",
            Spelling = "MemberNode",
            ProjectId = Projects[0].Id,
            ImplementationLocation = cppLocation
        };
        DefineLocation hLocation = new()
        {
            FilePath = "MemberNode.h",
            StartLine = 1,
            EndLine = 1,
            Node = node
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(hLocation);
        Locations.Add(cppLocation);
        db.Cxx_DefineLocations.Add(hLocation);
        db.Cxx_ImplementationLocations.Add(cppLocation);

        return node;
    }

    private Node GenerateTypeNode()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before TypeNode");

        ImplementationLocation cppLocation = new()
        {
            FilePath = "TypeNode.cpp",
            StartLine = 1,
            EndLine = 1
        };
        Node node = new()
        {
            CursorKindSpelling = "Class",
            Spelling = "TypeNode",
            ProjectId = Projects[0].Id,
            ImplementationLocation = cppLocation
        };
        DefineLocation hLocation = new()
        {
            FilePath = "TypeNode.h",
            StartLine = 1,
            EndLine = 1,
            Node = node
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(hLocation);
        Locations.Add(cppLocation);
        db.Cxx_DefineLocations.Add(hLocation);
        db.Cxx_ImplementationLocations.Add(cppLocation);

        return node;
    }

    private Node GenerateDependencyNode()
    {
        if (Nodes.Count < 1)
            throw new InvalidOperationException("RootNode1 must be generated before DependencyNode");

        DefineLocation dLocation = new()
        {
            FilePath = "DependencyNode1.hpp",
            StartLine = 1,
            EndLine = 1
        };
        Node node = new()
        {
            CursorKindSpelling = "Class",
            Spelling = "DependencyNode",
            Project = Projects[0],
            DefineLocation = dLocation
        };

        Nodes.Add(node);
        db.Cxx_Nodes.Add(node);
        Locations.Add(dLocation);
        db.Cxx_DefineLocations.Add(dLocation);

        return node;
    }
}
