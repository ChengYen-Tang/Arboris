using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.EntityFrameworkCore;

public class ArborisDbContext(DbContextOptions<ArborisDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Node> Cxx_Nodes { get; set; }
    public DbSet<DefineLocation> Cxx_DefineLocations { get; set; }
    public DbSet<ImplementationLocation> Cxx_ImplementationLocations { get; set; }
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

            entity.HasOne(n => n.DefineLocation)
                .WithOne(c => c.Node)
                .HasForeignKey<DefineLocation>(c => c.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.ImplementationLocation)
                .WithOne(c => c.Node)
                .HasForeignKey<ImplementationLocation>(c => c.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(n => n.Members)
                .WithOne(m => m.Node)
                .HasForeignKey(m => m.NodeId);
            entity.HasMany(n => n.Types)
                .WithOne(t => t.Node)
                .HasForeignKey(t => t.NodeId);
            entity.HasMany(n => n.Dependencies)
                .WithOne(d => d.Node)
                .HasForeignKey(d => d.NodeId);
        });

        builder.Entity<NodeMember>(entity =>
        {
            entity.HasOne(nm => nm.Node)
                .WithMany(n => n.Members)
                .HasForeignKey(nm => nm.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(nm => nm.Member)
                .WithMany()
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.ClientCascade);
        });

        builder.Entity<NodeType>(entity =>
        {
            entity.HasOne(nt => nt.Node)
                .WithMany(n => n.Types)
                .HasForeignKey(nt => nt.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(nt => nt.Type)
                .WithMany()
                .HasForeignKey(c => c.TypeId)
                .OnDelete(DeleteBehavior.ClientCascade);
        });

        builder.Entity<NodeDependency>(entity =>
        {
            entity.HasOne(d => d.Node)
                .WithMany(n => n.Dependencies)
                .HasForeignKey(d => d.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.From)
                .WithMany()
                .HasForeignKey(c => c.FromId)
                .OnDelete(DeleteBehavior.ClientCascade);
        });
    }
}
