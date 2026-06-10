using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Fault> Faults { get; set; }
        public DbSet<FaultHistory> FaultHistories { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<RequestStockItem> RequestStockItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationReceipt> NotificationReceipts { get; set; }
        public DbSet<ServiceForm> ServiceForms { get; set; }
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<InstitutionAddress> InstitutionAddresses { get; set; }

        // Auth entities
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        
        // WorkItem entities
        public DbSet<WorkItem> WorkItems { get; set; }
        public DbSet<WorkItemTag> WorkItemTags { get; set; }
        public DbSet<WorkItemComment> WorkItemComments { get; set; }
        public DbSet<WorkItemAttachment> WorkItemAttachments { get; set; }
        public DbSet<WorkItemChecklistItem> WorkItemChecklistItems { get; set; }
        public DbSet<WorkItemHistory> WorkItemHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Email unique index
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.EntityName, a.EntityId, a.CreatedAt })
                    .HasDatabaseName("IX_AuditLogs_Entity_CreatedAt");

                entity.HasIndex(a => new { a.EntityName, a.EntityId, a.Version })
                    .HasDatabaseName("IX_AuditLogs_Entity_Version");

                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                // Token lookup için unique index
                entity.HasIndex(r => r.Token)
                    .IsUnique()
                    .HasDatabaseName("IX_RefreshTokens_Token");
                
                // User bazlı token lookup için index
                entity.HasIndex(r => new { r.UserId, r.IsRevoked, r.IsUsed, r.ExpiresAt })
                    .HasDatabaseName("IX_RefreshTokens_UserId_Active");
                
                // User ilişkisi - User silindiğinde tokenlar cascade silinsin
                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // StockItem - ItemCode unique index kaldırıldı

            // Device - SerialNumber unique index
            modelBuilder.Entity<Device>()
                .HasIndex(d => d.SerialNumber)
                .IsUnique();

            // Request artık Device'a bağlı değil (manuel cihaz bilgisi)

            // Fault relationships
            modelBuilder.Entity<Fault>()
                .HasOne(f => f.AssignedToUser)
                .WithMany()
                .HasForeignKey(f => f.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Fault>()
                .HasOne(f => f.SpareDevice)
                .WithMany()
                .HasForeignKey(f => f.SpareDeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Fault>()
                .HasMany(f => f.History)
                .WithOne(h => h.Fault)
                .HasForeignKey(h => h.FaultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FaultHistory>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shipment relationships
            modelBuilder.Entity<Shipment>()
                .HasMany(s => s.Items)
                .WithOne(i => i.Shipment)
                .HasForeignKey(i => i.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShipmentItem relationships
            modelBuilder.Entity<ShipmentItem>()
                .HasOne(i => i.Device)
                .WithMany()
                .HasForeignKey(i => i.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShipmentItem>()
                .HasOne(i => i.StockItem)
                .WithMany()
                .HasForeignKey(i => i.StockItemId)
                .OnDelete(DeleteBehavior.SetNull);

            // StockMovement relationships
            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.StockItem)
                .WithMany(s => s.Movements)
                .HasForeignKey(m => m.StockItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // RequestStockItem relationships
            modelBuilder.Entity<RequestStockItem>()
                .HasOne(rsi => rsi.Request)
                .WithMany(r => r.RequestStockItems)
                .HasForeignKey(rsi => rsi.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestStockItem>()
                .HasOne(rsi => rsi.StockItem)
                .WithMany()
                .HasForeignKey(rsi => rsi.StockItemId)
                .OnDelete(DeleteBehavior.Restrict); // Stok kalemi silinmesin

            // Seed Data - Removed in favor of DbSeeder
            // modelBuilder.Entity<User>().HasData(...) removed to avoid migration conflicts with DateTime.UtcNow


            // ============ WORKITEM CONFIGURATIONS ============

            // WorkItem - Enum'ları int olarak sakla
            modelBuilder.Entity<WorkItem>(entity =>
            {
                entity.Property(w => w.Kind)
                    .HasConversion<int>();

                entity.Property(w => w.Stage)
                    .HasConversion<int>();

                entity.Property(w => w.Priority)
                    .HasConversion<int>();

                // RowVersion for optimistic concurrency (MySQL uyumlu)
                entity.Property(w => w.RowVersion)
                    .IsRowVersion();

                // Indexes for performance
                entity.HasIndex(w => new { w.Stage, w.Priority, w.UpdatedAt })
                    .HasDatabaseName("IX_WorkItems_Stage_Priority_UpdatedAt");

                entity.HasIndex(w => new { w.Kind, w.Stage })
                    .HasDatabaseName("IX_WorkItems_Kind_Stage");

                // LegacyRequestId unique index (idempotent migration için)
                // MySQL'de HasFilter desteklenmez, nullable unique index yeterli
                entity.HasIndex(w => w.LegacyRequestId)
                    .IsUnique()
                    .HasDatabaseName("IX_WorkItems_LegacyRequestId");

                // Relationships
                entity.HasOne(w => w.Device)
                    .WithMany()
                    .HasForeignKey(w => w.DeviceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(w => w.SourceFault)
                    .WithMany()
                    .HasForeignKey(w => w.SourceFaultId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(w => w.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(w => w.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(w => w.Tags)
                    .WithOne(t => t.WorkItem)
                    .HasForeignKey(t => t.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(w => w.Comments)
                    .WithOne(c => c.WorkItem)
                    .HasForeignKey(c => c.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(w => w.Attachments)
                    .WithOne(a => a.WorkItem)
                    .HasForeignKey(a => a.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(w => w.ChecklistItems)
                    .WithOne(c => c.WorkItem)
                    .HasForeignKey(c => c.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(w => w.History)
                    .WithOne(h => h.WorkItem)
                    .HasForeignKey(h => h.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WorkItemTag - Index for tag lookups
            modelBuilder.Entity<WorkItemTag>(entity =>
            {
                entity.HasIndex(t => new { t.WorkItemId, t.TagName })
                    .IsUnique()
                    .HasDatabaseName("IX_WorkItemTags_WorkItemId_TagName");
            });

            // WorkItemComment - Index for listing
            modelBuilder.Entity<WorkItemComment>(entity =>
            {
                entity.HasIndex(c => new { c.WorkItemId, c.CreatedAt })
                    .HasDatabaseName("IX_WorkItemComments_WorkItemId_CreatedAt");

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WorkItemAttachment
            modelBuilder.Entity<WorkItemAttachment>(entity =>
            {
                entity.HasOne(a => a.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // WorkItemChecklistItem
            modelBuilder.Entity<WorkItemChecklistItem>(entity =>
            {
                entity.HasIndex(c => new { c.WorkItemId, c.Order })
                    .HasDatabaseName("IX_WorkItemChecklistItems_WorkItemId_Order");

                entity.HasOne(c => c.CompletedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CompletedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // WorkItemHistory - Index for timeline
            modelBuilder.Entity<WorkItemHistory>(entity =>
            {
                entity.HasIndex(h => new { h.WorkItemId, h.CreatedAt })
                    .HasDatabaseName("IX_WorkItemHistory_WorkItemId_CreatedAt");

                entity.HasOne(h => h.User)
                    .WithMany()
                    .HasForeignKey(h => h.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Performance Indexes for CreatedByUserId (Role-based filtering optimization)
            modelBuilder.Entity<Fault>()
                .HasIndex(f => f.CreatedByUserId)
                .HasDatabaseName("IX_Faults_CreatedByUserId");

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.CreatedByUserId)
                .HasDatabaseName("IX_Requests_CreatedByUserId");

            modelBuilder.Entity<ServiceForm>()
                .HasIndex(s => s.CreatedByUserId)
                .HasDatabaseName("IX_ServiceForms_CreatedByUserId");
        }
    }
}




