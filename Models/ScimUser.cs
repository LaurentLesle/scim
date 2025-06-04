using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScimServiceProvider.Models
{
    public class ScimUser
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new() { "urn:ietf:params:scim:schemas:core:2.0:User" };
        
        [System.Text.Json.Serialization.JsonPropertyName("externalId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ExternalId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;
        
        // Customer relationship - not serialized in SCIM response
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string CustomerId { get; set; } = string.Empty;
        
        [ForeignKey("CustomerId")]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Customer? Customer { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Name? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? DisplayName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("nickName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? NickName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("profileUrl")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ProfileUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("userType")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? UserType { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("preferredLanguage")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? PreferredLanguage { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("locale")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Locale { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("timezone")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Timezone { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("active")]
        public bool Active { get; set; } = true;
        
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Password { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("emails")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<Email>? Emails { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("phoneNumbers")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<PhoneNumber>? PhoneNumbers { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("ims")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<InstantMessaging>? Ims { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("photos")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<Photo>? Photos { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("addresses")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<Address>? Addresses { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("groups")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<GroupMembership>? Groups { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("entitlements")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<Entitlement>? Entitlements { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("roles")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<Role>? Roles { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("x509Certificates")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<X509Certificate>? X509Certificates { get; set; }
        
        // Enterprise extension
        [System.Text.Json.Serialization.JsonPropertyName("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
        [Newtonsoft.Json.JsonProperty("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
        public EnterpriseUser? EnterpriseUser { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("meta")]
        public ScimMeta Meta { get; set; } = new();
        
        // These should be part of meta, not root level - hide from serialization
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class Name
    {
        [System.Text.Json.Serialization.JsonPropertyName("formatted")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Formatted { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("familyName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? FamilyName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("givenName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? GivenName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("middleName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? MiddleName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("honorificPrefix")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? HonorificPrefix { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("honorificSuffix")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? HonorificSuffix { get; set; }
    }

    public class Email : IEquatable<Email>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(Email? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Email);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class PhoneNumber : IEquatable<PhoneNumber>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(PhoneNumber? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PhoneNumber);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class Address : IEquatable<Address>
    {
        [System.Text.Json.Serialization.JsonPropertyName("formatted")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Formatted { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("streetAddress")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? StreetAddress { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("locality")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Locality { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("region")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Region { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("postalCode")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? PostalCode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("country")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Country { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "work";
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(Address? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Formatted == other.Formatted && 
                   StreetAddress == other.StreetAddress && 
                   Locality == other.Locality && 
                   Region == other.Region && 
                   PostalCode == other.PostalCode && 
                   Country == other.Country && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Address);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Formatted, StreetAddress, Locality, Region, PostalCode, Country, Type, Primary);
        }
    }

    public class GroupMembership : IEquatable<GroupMembership>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("$ref")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Ref { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = "direct";

        public bool Equals(GroupMembership? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Ref == other.Ref &&
                   Display == other.Display && 
                   Type == other.Type;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as GroupMembership);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Ref, Display, Type);
        }
    }

    public class Role : IEquatable<Role>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(Role? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Role);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class EnterpriseUser
    {
        [System.Text.Json.Serialization.JsonPropertyName("employeeNumber")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("employeeNumber")]
        public string? EmployeeNumber { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("department")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("department")]
        public string? Department { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("costCenter")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("costCenter")]
        public string? CostCenter { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("organization")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("organization")]
        public string? Organization { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("division")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("division")]
        public string? Division { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("manager")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("manager")]
        public Manager? Manager { get; set; }
    }

    public class Manager : IEquatable<Manager>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("value")]
        public string? Value { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("$ref")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("$ref")]
        public string? Ref { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        [Newtonsoft.Json.JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        public bool Equals(Manager? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && Ref == other.Ref && DisplayName == other.DisplayName;
        }

        public override bool Equals(object? obj) => Equals(obj as Manager);

        public override int GetHashCode() => HashCode.Combine(Value, Ref, DisplayName);
    }

    public class ScimMeta
    {
        [System.Text.Json.Serialization.JsonPropertyName("resourceType")]
        public string ResourceType { get; set; } = "User";
        
        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Location { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Version { get; set; }
    }

    public class InstantMessaging : IEquatable<InstantMessaging>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(InstantMessaging? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as InstantMessaging);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class Photo : IEquatable<Photo>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(Photo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Photo);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class Entitlement : IEquatable<Entitlement>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(Entitlement? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Entitlement);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }

    public class X509Certificate : IEquatable<X509Certificate>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("display")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("primary")]
        public bool Primary { get; set; } = false;

        public bool Equals(X509Certificate? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value && 
                   Display == other.Display && 
                   Type == other.Type && 
                   Primary == other.Primary;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as X509Certificate);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Display, Type, Primary);
        }
    }
}
