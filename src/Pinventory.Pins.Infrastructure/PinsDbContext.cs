using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Import;
using Pinventory.Pins.Domain.Pins;
using Pinventory.Pins.Domain.Tagging;
using Pinventory.Pins.Domain.Verification;

namespace Pinventory.Pins.Infrastructure;

public sealed class PinsDbContext(DbContextOptions<PinsDbContext> options) : DbContext(options)
{
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<TaggingJob> TaggingJobs => Set<TaggingJob>();
    public DbSet<VerificationJob> VerificationJobs => Set<VerificationJob>();
    public DbSet<TagCatalog> TagCatalogs => Set<TagCatalog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Pin
        builder.Entity<Pin>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PlaceId)
                .HasConversion(id => id.Id, id => new GooglePlaceId(id))
                .IsRequired();
            entity.HasIndex(x => x.PlaceId).IsUnique();

            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.StatusUpdatedAt).IsRequired();

            entity.ComplexProperty(x => x.Address, cb =>
            {
                cb.Property(p => p.Line).HasColumnName("Address");
            });
            entity.ComplexProperty(x => x.Location, cb =>
            {
                cb.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired();
                cb.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired();
            });

            entity.Property(x => x.Version).IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.OwnsMany(x => x.Tags, b =>
            {
                b.ToTable("PinTags");
                b.WithOwner().HasForeignKey("PinId");
                b.Property(t => t.Value).HasColumnName("Tag").IsRequired();
                b.HasKey("PinId", "Tag");
                b.HasIndex("Tag");
            });
        });

        // ImportJob
        builder.Entity<ImportJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.State).IsRequired();

            entity.HasIndex(x => new { x.UserId, x.State })
                .HasFilter("state = 'InProgress'")
                .IsUnique();
        });

        // TaggingJob
        builder.Entity<TaggingJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.State).IsRequired();
        });

        // VerificationJob
        builder.Entity<VerificationJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.State).IsRequired();
            entity.Property(x => x.Scope).IsRequired();
        });

        // TagCatalog + TagItem
        builder.Entity<TagCatalog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerUserId);

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.OwnsMany(x => x.Tags, e =>
            {
                e.ToTable("CatalogTags");
                e.WithOwner().HasForeignKey("CatalogId");
                e.Property(i => i.Value).HasColumnName("Tag").IsRequired();
                e.HasKey("CatalogId", "Tag");
                e.HasIndex("Tag");
            });
        });
    }
}