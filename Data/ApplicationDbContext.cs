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
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<OfferMaterial> OfferMaterials => Set<OfferMaterial>();


        public DbSet<Cabinet> Cabinets => Set<Cabinet>();

        public DbSet<OfferCabinet> OfferCabinets => Set<OfferCabinet>();
        public DbSet<PanelCabinet> PanelCabinets => Set<PanelCabinet>();

        public DbSet<OfferExtraItem> OfferExtraItems => Set<OfferExtraItem>();
        public DbSet<PanelExtraItem> PanelExtraItems => Set<PanelExtraItem>();

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



            modelBuilder.Entity<Offer>(entity =>
            {
                entity.HasKey(e => e.OfferID);

                entity.HasIndex(e => e.OfferCode)
                    .IsUnique();

                entity.Property(e => e.OfferCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LaborCost)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ProfitAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(2000);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Panel)
                    .WithMany()
                    .HasForeignKey(e => e.PanelID)
                    .OnDelete(DeleteBehavior.Restrict);
            });



            modelBuilder.Entity<OfferMaterial>(entity =>
            {
                entity.HasKey(e => e.OfferMaterialID);

                entity.Property(e => e.Quantity)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.DiscountPercent)
                    .HasColumnType("decimal(5,2)");

                entity.Property(e => e.ManualPriceReason)
                    .HasMaxLength(500);

                entity.HasOne(e => e.Offer)
                    .WithMany(e => e.OfferMaterials)
                    .HasForeignKey(e => e.OfferID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Material)
                    .WithMany()
                    .HasForeignKey(e => e.MaterialID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Cabinet>(entity =>
            {
                entity.HasKey(e => e.CabinetID);

                entity.Property(e => e.CabinetCode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CurrentPrice)
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(e => new { e.SupplierID, e.CabinetCode })
                    .IsUnique();

                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OfferCabinet>(entity =>
            {
                entity.HasKey(e => e.OfferCabinetID);

                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ManualPriceReason).HasMaxLength(500);

                entity.HasOne(e => e.Offer)
                    .WithMany(e => e.OfferCabinets)
                    .HasForeignKey(e => e.OfferID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Cabinet)
                    .WithMany()
                    .HasForeignKey(e => e.CabinetID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<PanelCabinet>(entity =>
            {
                entity.HasKey(e => e.PanelCabinetID);

                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ManualPriceReason).HasMaxLength(500);

                entity.HasOne(e => e.Panel)
                    .WithMany(e => e.PanelCabinets)
                    .HasForeignKey(e => e.PanelID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Cabinet)
                    .WithMany()
                    .HasForeignKey(e => e.CabinetID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<OfferExtraItem>(entity =>
            {
                entity.HasKey(e => e.OfferExtraItemID);

                entity.Property(e => e.ItemCode).HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.Offer)
                    .WithMany(e => e.OfferExtraItems)
                    .HasForeignKey(e => e.OfferID)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<PanelExtraItem>(entity =>
            {
                entity.HasKey(e => e.PanelExtraItemID);

                entity.Property(e => e.ItemCode).HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.Panel)
                    .WithMany(e => e.PanelExtraItems)
                    .HasForeignKey(e => e.PanelID)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Panel>()
                .HasOne(p => p.SourceOffer)
                .WithMany()
                .HasForeignKey(p => p.SourceOfferID)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Panel>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Offer>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Material>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Cabinet>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Customer>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Supplier>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Panel>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Offer>()
                .HasQueryFilter(x => !x.IsDeleted);

        }


    }
}