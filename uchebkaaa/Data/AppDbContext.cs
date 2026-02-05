using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace uchebkaaa.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessoriesSpec> AccessoriesSpecs { get; set; }

    public virtual DbSet<Accessory> Accessories { get; set; }

    public virtual DbSet<AssemblySpec> AssemblySpecs { get; set; }

    public virtual DbSet<ComponentWarehouse> ComponentWarehouses { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipments { get; set; }

    public virtual DbSet<EquipmentFailure> EquipmentFailures { get; set; }

    public virtual DbSet<EquipmentType> EquipmentTypes { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialSpec> MaterialSpecs { get; set; }

    public virtual DbSet<MaterialWarehouse> MaterialWarehouses { get; set; }

    public virtual DbSet<OperationSpec> OperationSpecs { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductionOperation> ProductionOperations { get; set; }

    public virtual DbSet<QualityCheck> QualityChecks { get; set; }

    public virtual DbSet<QualityParameter> QualityParameters { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=db1;Username=postgres;Password=1234");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessoriesSpec>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.AccessoriesId });

            entity.HasIndex(e => e.AccessoriesId, "IX_AccessoriesSpecs_AccessoriesId");

            entity.HasOne(d => d.Accessories).WithMany(p => p.AccessoriesSpecs).HasForeignKey(d => d.AccessoriesId);

            entity.HasOne(d => d.Product).WithMany(p => p.AccessoriesSpecs).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<Accessory>(entity =>
        {
            entity.HasKey(e => e.Article);

            entity.HasIndex(e => e.SupplierId, "IX_Accessories_SupplierId");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Accessories).HasForeignKey(d => d.SupplierId);
        });

        modelBuilder.Entity<AssemblySpec>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ItemId });

            entity.HasIndex(e => e.ItemId, "IX_AssemblySpecs_ItemId");

            entity.HasOne(d => d.Item).WithMany(p => p.AssemblySpecItems).HasForeignKey(d => d.ItemId);

            entity.HasOne(d => d.Product).WithMany(p => p.AssemblySpecProducts).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<ComponentWarehouse>(entity =>
        {
            entity.HasKey(e => new { e.ComponentArticle, e.WarehouseId }).HasName("ComponentWarehouses_pkey");

            entity.Property(e => e.ComponentArticle).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasDefaultValue(0);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.ComponentWarehouses)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("ComponentWarehouses_WarehouseId_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Employees_pkey");

            entity.Property(e => e.Education).HasMaxLength(200);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.Qualification).HasMaxLength(200);

            entity.HasMany(d => d.Operations).WithMany(p => p.Employees)
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeeOperation",
                    r => r.HasOne<ProductionOperation>().WithMany()
                        .HasForeignKey("OperationId")
                        .HasConstraintName("EmployeeOperations_OperationId_fkey"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmployeeId")
                        .HasConstraintName("EmployeeOperations_EmployeeId_fkey"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "OperationId").HasName("EmployeeOperations_pkey");
                        j.ToTable("EmployeeOperations");
                    });
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Mark);

            entity.HasIndex(e => e.EquipmentType, "IX_Equipments_EquipmentType");

            entity.HasOne(d => d.EquipmentTypeNavigation).WithMany(p => p.Equipment).HasForeignKey(d => d.EquipmentType);
        });

        modelBuilder.Entity<EquipmentFailure>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("EquipmentFailures_pkey");

            entity.Property(e => e.EndTime).HasColumnType("timestamp without time zone");
            entity.Property(e => e.EquipmentMark).HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.ReportedBy).HasMaxLength(100);
            entity.Property(e => e.StartTime).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<EquipmentType>(entity =>
        {
            entity.HasKey(e => e.Name);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Article);

            entity.HasIndex(e => e.SupplierId, "IX_Materials_SupplierId");

            entity.Property(e => e.Gost).HasColumnName("GOST");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Materials).HasForeignKey(d => d.SupplierId);
        });

        modelBuilder.Entity<MaterialSpec>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.MaterialId });

            entity.HasIndex(e => e.MaterialId, "IX_MaterialSpecs_MaterialId");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialSpecs).HasForeignKey(d => d.MaterialId);

            entity.HasOne(d => d.Product).WithMany(p => p.MaterialSpecs).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<MaterialWarehouse>(entity =>
        {
            entity.HasKey(e => new { e.MaterialArticle, e.WarehouseId }).HasName("MaterialWarehouses_pkey");

            entity.Property(e => e.MaterialArticle).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasDefaultValue(0);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.MaterialWarehouses)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("MaterialWarehouses_WarehouseId_fkey");
        });

        modelBuilder.Entity<OperationSpec>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.Operation, e.Number });

            entity.HasIndex(e => e.EquipmentType, "IX_OperationSpecs_EquipmentType");

            entity.HasOne(d => d.EquipmentTypeNavigation).WithMany(p => p.OperationSpecs).HasForeignKey(d => d.EquipmentType);

            entity.HasOne(d => d.Product).WithMany(p => p.OperationSpecs).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => new { e.Date, e.Number });

            entity.HasIndex(e => e.CustomerId, "IX_Orders_CustomerId");

            entity.HasIndex(e => e.ManagerId, "IX_Orders_ManagerId");

            entity.HasIndex(e => e.ProductId, "IX_Orders_ProductId");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Новый'::character varying");

            entity.HasOne(d => d.Customer).WithMany(p => p.OrderCustomers).HasForeignKey(d => d.CustomerId);

            entity.HasOne(d => d.Manager).WithMany(p => p.OrderManagers)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product).WithMany(p => p.Orders).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OrderStatusHistories_pkey");

            entity.Property(e => e.ChangedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.ChangedBy).HasMaxLength(100);
            entity.Property(e => e.NewStatus).HasMaxLength(50);
            entity.Property(e => e.OldStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Name);
        });

        modelBuilder.Entity<ProductionOperation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProductionOperations_pkey");

            entity.HasIndex(e => e.Name, "ProductionOperations_Name_key").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QualityCheck>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QualityChecks_pkey");

            entity.HasIndex(e => new { e.OrderNumber, e.ParameterId }, "QualityChecks_OrderNumber_ParameterId_key").IsUnique();

            entity.Property(e => e.CheckDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CheckedBy).HasMaxLength(100);

            entity.HasOne(d => d.Parameter).WithMany(p => p.QualityChecks)
                .HasForeignKey(d => d.ParameterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("QualityChecks_ParameterId_fkey");
        });

        modelBuilder.Entity<QualityParameter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QualityParameters_pkey");

            entity.HasIndex(e => e.Name, "QualityParameters_Name_key").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Name);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Login);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Warehouses_pkey");

            entity.HasIndex(e => e.Name, "Warehouses_Name_key").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
