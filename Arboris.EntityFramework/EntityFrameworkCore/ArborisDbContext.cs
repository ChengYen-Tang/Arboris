using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.EntityFrameworkCore;

public class ArborisDbContext(DbContextOptions<ArborisDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Node> Cxx_Nodes { get; set; }
    public DbSet<HeaderLocation> Cxx_HeaderLocations { get; set; }
    public DbSet<CppLocation> Cxx_CppLocations { get; set; }
    public DbSet<HppLocation> Cxx_HppLocations { get; set; }
    public DbSet<NodeMember> Cxx_NodeMembers { get; set; }
    public DbSet<NodeType> Cxx_NodeTypes { get; set; }
    public DbSet<NodeDependency> Cxx_NodeDependencies { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.HasMany(p => p.CxxNodes)
                .WithOne(n => n.Project)
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Node>(entity =>
        {
            entity.HasOne(n => n.Project)
                .WithMany(p => p.CxxNodes)
                .HasForeignKey(n => n.ProjectId);

            entity.HasOne(n => n.HeaderLocation)
                .WithOne(c => c.Node)
                .HasForeignKey<HeaderLocation>(c => c.NodeId)
                .HasPrincipalKey<Node>(n => n.Id)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.CppLocation)
                .WithOne(c => c.Node)
                .HasForeignKey<CppLocation>(c => c.NodeId)
                .HasPrincipalKey<Node>(n => n.Id)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.HppLocation)
                .WithOne(c => c.Node)
                .HasForeignKey<HppLocation>(c => c.NodeId)
                .HasPrincipalKey<Node>(n => n.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(n => n.Members)
                .WithOne(m => m.Node)
                .HasForeignKey(m => m.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(n => n.Types)
                .WithOne(t => t.Node)
                .HasForeignKey(t => t.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(n => n.Dependencies)
                .WithOne(d => d.Node)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<HeaderLocation>(entity =>
        {
            entity.HasOne(l => l.Node)
                .WithOne(n => n.HeaderLocation)
                .HasForeignKey<Node>(n => n.HeaderLocationId)
                .HasPrincipalKey<HeaderLocation>(l => l.Id)
                .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<CppLocation>(entity =>
        {
            entity.HasOne(l => l.Node)
                .WithOne(n => n.CppLocation)
                .HasForeignKey<Node>(c => c.CppLocationId)
                .HasPrincipalKey<CppLocation>(l => l.Id)
                .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<HppLocation>(entity =>
        {
            entity.HasOne(l => l.Node)
                .WithOne(n => n.HppLocation)
                .HasForeignKey<Node>(c => c.HppLocationId)
                .HasPrincipalKey<HppLocation>(l => l.Id)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<NodeMember>(entity =>
        {
            entity.HasOne(nm => nm.Node)
                .WithMany(n => n.Members)
                .HasForeignKey(nm => nm.NodeId);
            entity.HasOne(nm => nm.Member)
                .WithOne()
                .HasForeignKey<NodeMember>(c => c.MemberId);
        });

        builder.Entity<NodeType>(entity =>
        {
            entity.HasOne(nt => nt.Node)
                .WithMany(n => n.Types)
                .HasForeignKey(nt => nt.NodeId);
            entity.HasOne(nt => nt.Type)
                .WithOne()
                .HasForeignKey<NodeType>(c => c.TypeId);
        });

        builder.Entity<NodeDependency>(entity =>
        {
            entity.HasOne(d => d.Node)
                .WithMany(n => n.Dependencies)
                .HasForeignKey(d => d.NodeId);
            entity.HasOne(d => d.From)
                .WithOne()
                .HasForeignKey<NodeDependency>(c => c.FromId);
        });
    }
}
