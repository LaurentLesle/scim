using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Models;
using Newtonsoft.Json;

namespace ScimServiceProvider.Data
{
    public class ScimDbContext : DbContext
    {
        public ScimDbContext(DbContextOptions<ScimDbContext> options) : base(options)
        {
        }

        public DbSet<ScimUser> Users { get; set; }
        public DbSet<ScimGroup> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ScimUser entity
            modelBuilder.Entity<ScimUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).IsRequired();
                
                // Convert complex objects to JSON for storage
                entity.Property(e => e.Name)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<Name>(v));

                entity.Property(e => e.Emails)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Email>>(v) ?? new List<Email>());

                entity.Property(e => e.PhoneNumbers)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<PhoneNumber>>(v) ?? new List<PhoneNumber>());

                entity.Property(e => e.Addresses)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Address>>(v) ?? new List<Address>());

                entity.Property(e => e.Groups)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<GroupMembership>>(v) ?? new List<GroupMembership>());

                entity.Property(e => e.Meta)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<ScimMeta>(v) ?? new ScimMeta());
            });

            // Configure ScimGroup entity
            modelBuilder.Entity<ScimGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).IsRequired();

                entity.Property(e => e.Members)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<GroupMember>>(v) ?? new List<GroupMember>());

                entity.Property(e => e.Meta)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<ScimMeta>(v) ?? new ScimMeta());
            });
        }
    }
}
