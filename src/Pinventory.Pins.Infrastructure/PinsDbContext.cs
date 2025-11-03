using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Import;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Domain.Tags;

namespace Pinventory.Pins.Infrastructure;

public sealed class PinsDbContext(DbContextOptions<PinsDbContext> options) : DbContext(options)
{
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<TagCatalog> TagCatalogs => Set<TagCatalog>();

    /*public DbSet<TaggingJob> TaggingJobs => Set<TaggingJob>();
    public DbSet<VerificationJob> VerificationJobs => Set<VerificationJob>();*/

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("pins");

        // Pin
        builder.Entity<Pin>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerId).IsRequired();

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
                .HasDefaultValue(0)
                .ValueGeneratedOnAddOrUpdate();

            entity.OwnsMany(x => x.Tags, b =>
            {
                b.ToTable("PinTags");
                b.WithOwner().HasForeignKey("PinId");
                b.Property(t => t.Value).HasColumnName("Value").IsRequired();
                b.HasKey("PinId", "Value");
                b.HasIndex("Value");
            });

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // TagCatalog + TagItem
        builder.Entity<TagCatalog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerId);

            entity.Property(x => x.Version).IsConcurrencyToken()
                .HasDefaultValue(0)
                .ValueGeneratedOnAddOrUpdate();

            entity.OwnsMany(x => x.Tags, e =>
            {
                e.ToTable("CatalogTags");
                e.WithOwner().HasForeignKey("CatalogId");
                e.Property(i => i.Value).HasColumnName("Value").IsRequired();
                e.HasKey("CatalogId", "Value");
                e.HasIndex("Value");
            });

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ImportJob
        builder.Entity<ImportJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.ArchiveJobId).IsRequired(false);
            entity.Property(x => x.State).HasConversion<string>().IsRequired();
            entity.Property(x => x.StartedAt).IsRequired(false);
            entity.Property(x => x.CompletedAt).IsRequired(false);
            entity.Property(x => x.Processed).IsRequired();
            entity.Property(x => x.Created).IsRequired();
            entity.Property(x => x.Updated).IsRequired();
            entity.Property(x => x.Failed).IsRequired();
            entity.Property(x => x.Conflicts).IsRequired();
            entity.Property(x => x.Total).IsRequired();

            entity.Property(x => x.Version).IsConcurrencyToken()
                .HasDefaultValue(0)
                .ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(x => new { x.UserId, x.State })
                .HasFilter("[State] = 'InProgress'")
                .IsUnique();
        });

        // // TaggingJob
        // builder.Entity<TaggingJob>(entity =>
        // {
        //     entity.HasKey(x => x.Id);
        //     entity.Property(x => x.State).IsRequired();
        // });
        //
        // // VerificationJob
        // builder.Entity<VerificationJob>(entity =>
        // {
        //     entity.HasKey(x => x.Id);
        //     entity.Property(x => x.State).IsRequired();
        //     entity.Property(x => x.Scope).IsRequired();
        // });
    }
}