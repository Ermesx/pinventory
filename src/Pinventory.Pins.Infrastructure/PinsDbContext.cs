using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure.ReadModels;
using Pinventory.Pins.Infrastructure.Sagas;

namespace Pinventory.Pins.Infrastructure;

public sealed class PinsDbContext(DbContextOptions<PinsDbContext> options) : DbContext(options)
{
    public DbSet<TagCatalog> TagCatalogs => Set<TagCatalog>();
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<Import> Imports => Set<Import>();
    public DbSet<ImportSummary> ImportSummaries => Set<ImportSummary>();
    public DbSet<ImportProcess> ImportProcesses => Set<ImportProcess>();

    /*public DbSet<TaggingJob> TaggingJobs => Set<TaggingJob>();
    public DbSet<VerificationJob> VerificationJobs => Set<VerificationJob>();*/

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("pins");

        // Pin
        builder.Entity<Pin>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OwnerId).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(500);

            entity.Property(x => x.PlaceId)
                .HasConversion(id => id.Id, id => new GooglePlaceId(id))
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(x => x.PlaceId).IsUnique();

            entity.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(x => x.StatusUpdatedAt).IsRequired();
            entity.Property(x => x.AddedAt).IsRequired();

            entity.ComplexProperty(x => x.Address, cb =>
            {
                cb.Property(p => p.Line).HasColumnName("Address").IsRequired().HasMaxLength(1000);
                cb.Property(p => p.CountryCode).HasColumnName("CountryCode").HasConversion<string>().IsRequired().HasMaxLength(2);
            });
            entity.ComplexProperty(x => x.Location, cb =>
            {
                cb.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired();
                cb.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired();
            });

            entity.Property(x => x.Version)
                .HasDefaultValue(0)
                .IsRowVersion();

            entity.OwnsMany(x => x.Tags, b =>
            {
                b.ToTable("PinTags");
                b.WithOwner().HasForeignKey("PinId");
                b.Property(t => t.Value).IsRequired().HasMaxLength(100);
                b.HasKey("PinId", "Value");
                b.HasIndex("Value");
            });

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // TagCatalog + TagItem
        builder.Entity<TagCatalog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OwnerId).HasMaxLength(100);

            entity.Property(x => x.Version)
                .ValueGeneratedNever();

            entity.OwnsMany(x => x.Tags, e =>
            {
                e.ToTable("CatalogTags");
                e.WithOwner().HasForeignKey("CatalogId");
                e.Property(i => i.Value).IsRequired().HasMaxLength(100);
                e.HasKey("CatalogId", "Value");
                e.HasIndex("Value");
            });

            entity.Property(x => x.Version)
                .HasDefaultValue(0)
                .IsRowVersion();

            entity.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Import
        builder.Entity<Import>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ArchiveJobId).HasMaxLength(200);
            entity.Property(x => x.State).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(x => x.StartedAt);
            entity.Property(x => x.CompletedAt);

            entity.ComplexProperty(x => x.Period, cb =>
            {
                cb.Property(p => p.Start).HasColumnName("PeriodStart").IsRequired();
                cb.Property(p => p.End).HasColumnName("PeriodEnd").IsRequired();
            });

            entity.Property(x => x.Version)
                .HasDefaultValue(0)
                .IsRowVersion();

            entity.OwnsMany(b => b.StarredPlaces, starredPlace =>
            {
                starredPlace.ToTable("ImportStarredPlaces");
                starredPlace.WithOwner().HasForeignKey("ImportId");
                starredPlace.Property(p => p.Id).ValueGeneratedNever();
                starredPlace.HasKey(p => p.Id);

                starredPlace.Property(p => p.Name).HasMaxLength(500);
                starredPlace.Property(p => p.GoogleMapsUrl).IsRequired().HasMaxLength(2048);
                starredPlace.Property(p => p.Address).HasMaxLength(1000);
                starredPlace.Property(p => p.CountryCode).HasConversion<string>().HasMaxLength(2);
                starredPlace.Property(p => p.Latitude);
                starredPlace.Property(p => p.Longitude);
                starredPlace.Property(p => p.AddedDate).IsRequired();
                starredPlace.Property(p => p.Comment).HasMaxLength(2000);
                starredPlace.Property(p => p.State).HasConversion<string>().IsRequired().HasMaxLength(20);
                starredPlace.Property(p => p.IsProcessed).IsRequired();

                starredPlace.HasIndex("ImportId");
                starredPlace.HasIndex("State");
            });

            entity.Navigation(p => p.StarredPlaces).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasIndex(x => new { x.UserId, x.State })
                .HasFilter("\"State\" = 'InProgress'")
                .IsUnique();
        });

        // ImportSummary (View)
        builder.Entity<ImportSummary>(entity =>
        {
            entity.ToView("ImportSummaries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.State).HasConversion<string>();
        });

        builder.Entity<ImportProcess>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.BatchesToProceed).IsRequired();
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