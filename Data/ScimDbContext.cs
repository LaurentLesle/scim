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
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.TenantId).IsRequired();
                
                // One customer to many users
                entity.HasMany(c => c.Users)
                      .WithOne(u => u.Customer)
                      .HasForeignKey(u => u.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // One customer to many groups
                entity.HasMany(c => c.Groups)
                      .WithOne(g => g.Customer)
                      .HasForeignKey(g => g.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure ScimUser entity
            modelBuilder.Entity<ScimUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).IsRequired();
                entity.Property(e => e.CustomerId).IsRequired();
                
                // Convert complex objects to JSON for storage
                entity.Property(e => e.Schemas)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<string>>(v) ?? new List<string>());
                        
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

                entity.Property(e => e.Roles)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Role>>(v) ?? new List<Role>());

                entity.Property(e => e.EnterpriseUser)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<EnterpriseUser>(v));

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
                entity.Property(e => e.CustomerId).IsRequired();

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
