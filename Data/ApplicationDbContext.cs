using Microsoft.EntityFrameworkCore;
using panelapp.Models;

namespace panelapp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Panel> Panels => Set<Panel>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<PanelMaterial> PanelMaterials => Set<PanelMaterial>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<SupplierContactPerson> SupplierContactPersons => Set<SupplierContactPerson>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            modelBuilder.Entity<Panel>().ToTable("Panels");
            modelBuilder.Entity<Material>().ToTable("Materials");
            modelBuilder.Entity<PanelMaterial>().ToTable("PanelMaterials");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<SupplierContactPerson>().ToTable("SupplierContactPersons");
            modelBuilder.Entity<ActivityLog>().ToTable("ActivityLog");


            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Panel>()
                .HasIndex(p => p.PanelCode)
                .IsUnique();

            modelBuilder.Entity<Material>()
                .HasIndex(m => new { m.SupplierID, m.MaterialCode })
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerName)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.VatNumber)
                .IsUnique();

            modelBuilder.Entity<Panel>()
                .HasOne(p => p.Customer)
                .WithMany(c => c.Panels)
                .HasForeignKey(p => p.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierContactPerson>()
                .HasOne(x => x.Supplier)
                .WithMany(x => x.ContactPersons)
                .HasForeignKey(x => x.SupplierID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ActivityLogs");

                entity.HasKey(x => x.ActivityLogID);

                entity.Property(x => x.EntityType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.EntityID)
                    .IsRequired(false);

                entity.Property(x => x.ActionType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.UserName)
                    .HasMaxLength(100);

                entity.Property(x => x.UserRole)
                    .HasMaxLength(100);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(x => x.CreatedAt);
                entity.HasIndex(x => new { x.EntityType, x.EntityID });
            });


        }
    }
}