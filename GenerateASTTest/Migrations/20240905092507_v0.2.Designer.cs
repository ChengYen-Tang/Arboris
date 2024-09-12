﻿// <auto-generated />
using GenerateASTTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GenerateASTTest.Migrations
{
    [DbContext(typeof(LiteContext))]
    [Migration("20240905092507_v0.2")]
    partial class v02
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("GenerateASTTest.CodeInfo", b =>
                {
                    b.Property<string>("CodeName")
                        .HasColumnType("TEXT");

                    b.Property<string>("CxType")
                        .HasColumnType("TEXT");

                    b.Property<string>("NameSpace")
                        .HasColumnType("TEXT");

                    b.Property<string>("Spelling")
                        .HasColumnType("TEXT");

                    b.HasKey("CodeName", "CxType");

                    b.ToTable("CodeInfo");
                });
#pragma warning restore 612, 618
        }
    }
}