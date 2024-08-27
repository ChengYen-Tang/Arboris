﻿// <auto-generated />
using System;
using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    [DbContext(typeof(ArborisDbContext))]
    partial class ArborisDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.DefineLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("EndLine")
                        .HasColumnType("bigint");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("SourceCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("StartLine")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("NodeId")
                        .IsUnique();

                    b.HasIndex("FilePath", "StartLine");

                    b.HasIndex("FilePath", "StartLine", "EndLine");

                    b.ToTable("Cxx_DefineLocations");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.ImplementationLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("EndLine")
                        .HasColumnType("bigint");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("SourceCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("StartLine")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("NodeId")
                        .IsUnique();

                    b.HasIndex("FilePath", "StartLine");

                    b.HasIndex("FilePath", "StartLine", "EndLine");

                    b.ToTable("Cxx_ImplementationLocations");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CursorKindSpelling")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CxType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExampleCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LLMDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NameSpace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Spelling")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserDescription")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Cxx_Nodes");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeDependency", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("FromId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "FromId");

                    b.HasIndex("FromId");

                    b.ToTable("Cxx_NodeDependencies");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MemberId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "MemberId");

                    b.HasIndex("MemberId");

                    b.ToTable("Cxx_NodeMembers");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TypeId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "TypeId");

                    b.HasIndex("TypeId");

                    b.ToTable("Cxx_NodeTypes");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.DefineLocation", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithOne("DefineLocation")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.DefineLocation", "NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.ImplementationLocation", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithOne("ImplementationLocation")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.ImplementationLocation", "NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.Project", "Project")
                        .WithMany("CxxNodes")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeDependency", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "From")
                        .WithMany()
                        .HasForeignKey("FromId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Dependencies")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("From");

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Member")
                        .WithMany()
                        .HasForeignKey("MemberId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Members")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Member");

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Types")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Type")
                        .WithMany()
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Node");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.Navigation("DefineLocation");

                    b.Navigation("Dependencies");

                    b.Navigation("ImplementationLocation");

                    b.Navigation("Members");

                    b.Navigation("Types");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.Project", b =>
                {
                    b.Navigation("CxxNodes");
                });
#pragma warning restore 612, 618
        }
    }
}
