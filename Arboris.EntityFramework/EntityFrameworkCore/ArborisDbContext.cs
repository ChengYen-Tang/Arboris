using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.EntityFrameworkCore;

public class ArborisDbContext(DbContextOptions<ArborisDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Node> CxxNodes { get; set; }
    public DbSet<HeaderLocation> HeaderLocations { get; set; }
    public DbSet<CppLocation> CppLocations { get; set; }
    public DbSet<HppLocation> HppLocations { get; set; }

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
            entity.HasKey(nm => new { nm.NodeId, nm.MemberId });

            entity.HasOne(nm => nm.Node)
                .WithMany(n => n.Members)
                .HasForeignKey(nm => nm.NodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(nm => nm.Member)
                .WithOne()
                .HasForeignKey<Node>(c => c.Id)
                .HasPrincipalKey<NodeMember>(n => n.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<NodeType>(entity =>
        {
            entity.HasKey(nt => new { nt.NodeId, nt.TypeId });

            entity.HasOne(nt => nt.Node)
                .WithMany(n => n.Types)
                .HasForeignKey(nt => nt.NodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(nt => nt.Type)
                .WithOne()
                .HasForeignKey<Node>(c => c.Id)
                .HasPrincipalKey<NodeType>(n => n.TypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Dependency>(entity =>
        {
            entity.HasKey(d => new { d.NodeId, d.FromId });

            entity.HasOne(d => d.Node)
                .WithMany(n => n.Dependencies)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(d => d.From)
                .WithOne()
                .HasForeignKey<Node>(c => c.Id)
                .HasPrincipalKey<Dependency>(d => d.FromId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
