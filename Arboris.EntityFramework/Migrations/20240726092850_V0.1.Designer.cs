﻿// <auto-generated />
using System;
using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    [DbContext(typeof(ArborisDbContext))]
    [Migration("20240726092850_V0.1")]
    partial class V01
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.CppLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("EndLine")
                        .HasColumnType("int");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("StartLine")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("CppLocations");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Dependency", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("FromId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "FromId");

                    b.ToTable("Dependency");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.HeaderLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("EndLine")
                        .HasColumnType("int");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("StartLine")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("HeaderLocations");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.HppLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("EndLine")
                        .HasColumnType("int");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("StartLine")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("HppLocations");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CppLocationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CursorKindSpelling")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CxType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("HeaderLocationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("HppLocationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Spelling")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CppLocationId")
                        .IsUnique()
                        .HasFilter("[CppLocationId] IS NOT NULL");

                    b.HasIndex("HeaderLocationId")
                        .IsUnique()
                        .HasFilter("[HeaderLocationId] IS NOT NULL");

                    b.HasIndex("HppLocationId")
                        .IsUnique()
                        .HasFilter("[HppLocationId] IS NOT NULL");

                    b.HasIndex("ProjectId");

                    b.ToTable("CxxNodes");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MemberId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "MemberId");

                    b.ToTable("NodeMember");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", b =>
                {
                    b.Property<Guid>("NodeId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TypeId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("NodeId", "TypeId");

                    b.ToTable("NodeType");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Dependency", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Dependencies")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.CppLocation", "CppLocation")
                        .WithOne("Node")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "CppLocationId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.HeaderLocation", "HeaderLocation")
                        .WithOne("Node")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "HeaderLocationId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.HppLocation", "HppLocation")
                        .WithOne("Node")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "HppLocationId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Dependency", null)
                        .WithOne("From")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Id")
                        .HasPrincipalKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Dependency", "FromId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", null)
                        .WithOne("Member")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Id")
                        .HasPrincipalKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", "MemberId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", null)
                        .WithOne("Type")
                        .HasForeignKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Id")
                        .HasPrincipalKey("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", "TypeId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.Project", "Project")
                        .WithMany("CxxNodes")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CppLocation");

                    b.Navigation("HeaderLocation");

                    b.Navigation("HppLocation");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Members")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", b =>
                {
                    b.HasOne("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", "Node")
                        .WithMany("Types")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.CppLocation", b =>
                {
                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Dependency", b =>
                {
                    b.Navigation("From")
                        .IsRequired();
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.HeaderLocation", b =>
                {
                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.HppLocation", b =>
                {
                    b.Navigation("Node");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.Node", b =>
                {
                    b.Navigation("Dependencies");

                    b.Navigation("Members");

                    b.Navigation("Types");
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeMember", b =>
                {
                    b.Navigation("Member")
                        .IsRequired();
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.CXX.NodeType", b =>
                {
                    b.Navigation("Type")
                        .IsRequired();
                });

            modelBuilder.Entity("Arboris.EntityFramework.EntityFrameworkCore.Project", b =>
                {
                    b.Navigation("CxxNodes");
                });
#pragma warning restore 612, 618
        }
    }
}
