using Microsoft.EntityFrameworkCore;
using MyWorkbench.Modules.Invoices.Domain;

namespace MyWorkbench.Modules.Invoices.Persistence;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoiceTemplate> InvoiceTemplates => Set<InvoiceTemplate>();
    public DbSet<InvoiceTemplateVersion> InvoiceTemplateVersions => Set<InvoiceTemplateVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoicePrefix).HasMaxLength(32);
            entity.Property(x => x.DefaultCurrencyCode).HasMaxLength(8);
            entity.Property(x => x.IssuerName).HasMaxLength(200);
            entity.Property(x => x.DefaultTerms).HasMaxLength(4000);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoiceNumber).HasMaxLength(64);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.Property(x => x.CurrencyCode).HasMaxLength(8);
            entity.HasMany(x => x.LineItems).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<InvoiceTemplate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasMany(x => x.Versions)
                .WithOne(x => x.Template)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ActiveVersion)
                .WithMany()
                .HasForeignKey(x => x.ActiveVersionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceTemplateVersion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceStorageKey).HasMaxLength(512);
            entity.Property(x => x.LayoutJson).HasMaxLength(20000);
            entity.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
        });
    }
}
