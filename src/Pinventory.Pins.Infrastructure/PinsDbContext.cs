using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Domain.Tags;

namespace Pinventory.Pins.Infrastructure;

public sealed class PinsDbContext(DbContextOptions<PinsDbContext> options) : DbContext(options)
{
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<Import> Imports => Set<Import>();
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
            entity.Property(x => x.AddedAt).IsRequired();

            entity.ComplexProperty(x => x.Address, cb =>
            {
                cb.Property(p => p.Line).HasColumnName("Address").IsRequired();
                cb.Property(p => p.CountryCode).HasColumnName("CountryCode").HasConversion<string>().IsRequired();
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
                b.Property(t => t.Value).IsRequired();
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
                e.Property(i => i.Value).IsRequired();
                e.HasKey("CatalogId", "Value");
                e.HasIndex("Value");
            });

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Import
        builder.Entity<Import>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.ArchiveJobId);
            entity.Property(x => x.State).HasConversion<string>().IsRequired();
            entity.Property(x => x.StartedAt);
            entity.Property(x => x.CompletedAt);
            entity.Property(x => x.Processed).IsRequired();
            entity.Property(x => x.Created).IsRequired();
            entity.Property(x => x.Updated).IsRequired();
            entity.Property(x => x.Failed).IsRequired();
            entity.Property(x => x.Conflicts).IsRequired();
            entity.Property(x => x.Total).IsRequired();

            entity.ComplexProperty(x => x.Period, cb =>
            {
                cb.Property(p => p.Start).HasColumnName("PeriodStart").IsRequired();
                cb.Property(p => p.End).HasColumnName("PeriodEnd").IsRequired();
            });

            entity.Property(x => x.Version).IsConcurrencyToken()
                .HasDefaultValue(0)
                .ValueGeneratedOnAddOrUpdate();

            entity.OwnsMany<Batch>("_batches", batch =>
            {
                batch.ToTable("ImportBatches");
                batch.WithOwner().HasForeignKey("ImportId");
                batch.HasKey("Id");

                batch.OwnsMany(b => b.StarredPlaces, starredPlace =>
                {
                    starredPlace.ToTable("ImportStarredPlaces");
                    starredPlace.WithOwner().HasForeignKey("BatchId");
                    starredPlace.HasKey("Id");

                    starredPlace.Property(p => p.Name);
                    starredPlace.Property(p => p.GoogleMapsUrl).IsRequired();
                    starredPlace.Property(p => p.Address);
                    starredPlace.Property(p => p.CountryCode).HasConversion<string>();
                    starredPlace.Property(p => p.Latitude);
                    starredPlace.Property(p => p.Longitude);
                    starredPlace.Property(p => p.AddedDate).IsRequired();
                    starredPlace.Property(p => p.Comment);
                    starredPlace.Property(p => p.State).HasConversion<string>().IsRequired();
                    starredPlace.Property(p => p.IsProcessed).IsRequired();

                    starredPlace.HasIndex("BatchId");
                    starredPlace.HasIndex("State");
                });

                batch.Navigation(b => b.StarredPlaces).UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            entity.Navigation("_batches").UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasIndex(x => new { x.UserId, x.State })
                .HasFilter("\"State\" = 'InProgress'")
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