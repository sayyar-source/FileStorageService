using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<FileEntry> Files { get; set; }
    public DbSet<SharedAccess> SharedAccesses { get; set; }
    public DbSet<FileVersion> FileVersions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<FileEntry>()
            .HasOne(f => f.Owner)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileEntry>()
            .HasOne(f => f.ParentFolder)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SharedAccess>()
            .HasOne(sa => sa.FileEntry)
            .WithMany(f => f.SharedAccesses)
            .HasForeignKey(sa => sa.FileEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SharedAccess>()
            .HasOne(sa => sa.User)
            .WithMany(u => u.SharedAccesses)
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FileVersion>()
            .HasOne(fv => fv.FileEntry)
            .WithMany(f => f.Versions)
            .HasForeignKey(fv => fv.FileEntryId)
            .OnDelete(DeleteBehavior.Cascade);
   
    }

}
