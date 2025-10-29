using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DataSenseAPI.Domain.Models;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.AppDb
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
        public DbSet<PricingRecord> PricingRecords => Set<PricingRecord>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApiKey
            builder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("ApiKeys");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.KeyHash);
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.UserMetadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure Conversation
            builder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExternalUserId);
            });

            // Configure ChatMessage
            builder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ConversationId);
                entity.HasOne<Conversation>()
                    .WithMany()
                    .HasForeignKey(e => e.ConversationId);
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure RequestLog
            builder.Entity<RequestLog>(entity =>
            {
                entity.ToTable("RequestLogs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure PricingRecord
            builder.Entity<PricingRecord>(entity =>
            {
                entity.ToTable("PricingRecords");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Date });
            });
        }
    }
}


