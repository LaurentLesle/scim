using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

            // Ignore value object types to prevent EF Core from treating them as entities
            modelBuilder.Ignore<InstantMessaging>();
            modelBuilder.Ignore<Photo>();

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
                        v => JsonConvert.DeserializeObject<List<string>>(v) ?? new List<string>(),
                        new ValueComparer<List<string>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));
                        
                entity.Property(e => e.Name)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<Name>(v));

                entity.Property(e => e.Emails)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Email>>(v) ?? new List<Email>(),
                        new ValueComparer<List<Email>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.PhoneNumbers)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<PhoneNumber>>(v) ?? new List<PhoneNumber>(),
                        new ValueComparer<List<PhoneNumber>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Addresses)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Address>>(v) ?? new List<Address>(),
                        new ValueComparer<List<Address>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Groups)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<GroupMembership>>(v) ?? new List<GroupMembership>(),
                        new ValueComparer<List<GroupMembership>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Roles)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Role>>(v) ?? new List<Role>(),
                        new ValueComparer<List<Role>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Entitlements)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Entitlement>>(v) ?? new List<Entitlement>(),
                        new ValueComparer<List<Entitlement>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Ims)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<InstantMessaging>>(v) ?? new List<InstantMessaging>(),
                        new ValueComparer<List<InstantMessaging>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Photos)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<Photo>>(v) ?? new List<Photo>(),
                        new ValueComparer<List<Photo>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.X509Certificates)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<X509Certificate>>(v) ?? new List<X509Certificate>(),
                        new ValueComparer<List<X509Certificate>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

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
                        v => JsonConvert.DeserializeObject<List<GroupMember>>(v) ?? new List<GroupMember>(),
                        new ValueComparer<List<GroupMember>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()));

                entity.Property(e => e.Meta)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<ScimMeta>(v) ?? new ScimMeta());
            });
        }
    }
}
