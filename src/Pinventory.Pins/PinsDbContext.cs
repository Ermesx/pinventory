using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Import;
using Pinventory.Pins.Tagging;
using Pinventory.Pins.Verification;

namespace Pinventory.Pins;

public sealed class PinsDbContext(DbContextOptions<PinsDbContext> options) : DbContext(options)
{
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<PinTag> PinTags => Set<PinTag>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<TaggingJob> TaggingJobs => Set<TaggingJob>();
    public DbSet<VerificationJob> VerificationJobs => Set<VerificationJob>();
    public DbSet<TagCatalog> TagCatalogs => Set<TagCatalog>();
    public DbSet<TagItem> TagItems => Set<TagItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Pin
        b.Entity<Pin>(eb =>
        {
            eb.ToTable("pins");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.GooglePlaceId)
                .HasConversion(id => id.Value, s => new GooglePlaceId(s))
                .IsRequired();
            eb.HasIndex(x => x.GooglePlaceId).IsUnique();

            eb.Property(x => x.Name).IsRequired();
            eb.Property(x => x.ExistsStatus).HasConversion<string>().IsRequired();
            eb.Property(x => x.UpdatedAt).IsRequired();

            eb.ComplexProperty(x => x.Address, cb =>
            {
                cb.Property(p => p.Line).HasColumnName("address");
            });
            eb.ComplexProperty(x => x.Location, cb =>
            {
                cb.Property(p => p.Latitude).HasColumnName("latitude").IsRequired();
                cb.Property(p => p.Longitude).HasColumnName("longitude").IsRequired();
            });

            eb.Property(x => x.Version).IsConcurrencyToken();

            eb.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<PinTag>(eb =>
        {
            eb.ToTable("pin_tags");
            eb.HasKey(x => new { x.PinId, x.Tag });
            eb.Property(x => x.Tag).IsRequired();
            eb.HasOne<Pin>().WithMany(p => p.Tags).HasForeignKey(x => x.PinId).OnDelete(DeleteBehavior.Cascade);
            // Npgsql GIN index for tags (optional, provider-specific)
            eb.HasIndex(x => x.Tag).HasDatabaseName("ix_pin_tags_tag");
        });

        // ImportJob
        b.Entity<ImportJob>(eb =>
        {
            eb.ToTable("import_jobs");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.UserId).IsRequired();
            eb.Property(x => x.State).IsRequired();

            eb.HasIndex(x => new { x.UserId, x.State })
                .HasDatabaseName("ix_import_one_active")
                .HasFilter("state = 'InProgress'")
                .IsUnique();
        });

        // TaggingJob
        b.Entity<TaggingJob>(eb =>
        {
            eb.ToTable("tagging_jobs");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.State).IsRequired();
            eb.Property(x => x.Scope).IsRequired();
            eb.Property(x => x.ModelVersion).IsRequired();
        });

        // VerificationJob
        b.Entity<VerificationJob>(eb =>
        {
            eb.ToTable("verification_jobs");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.State).IsRequired();
            eb.Property(x => x.Scope).IsRequired();
        });

        // TagCatalog + TagItem
        b.Entity<TagCatalog>(eb =>
        {
            eb.ToTable("tag_catalogs");
            eb.HasKey(x => x.Id);
            eb.Property(x => x.OwnerUserId);

            eb.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<TagItem>(eb =>
        {
            eb.ToTable("tag_items");
            eb.HasKey(x => new { x.CatalogId, x.Tag });
            eb.Property(x => x.Tag).IsRequired();
            eb.HasOne<TagCatalog>().WithMany(c => c.Items)
                .HasForeignKey(x => x.CatalogId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}