# Migration Guide: Single-Tenant to Multi-Tenant SCIM Service Provider

This document provides guidance on migrating an existing single-tenant SCIM Service Provider deployment to the new multi-tenant architecture.

## Migration Overview

The migration from a single-tenant to a multi-tenant architecture involves the following high-level steps:

1. Database schema migration
2. Data migration
3. Configuration updates
4. Client application updates

## Prerequisites

- Backup your existing database
- Schedule downtime for the migration
- Test the migration process in a development environment first

## Step 1: Database Schema Migration

### Run Database Migration

The Entity Framework Core migration will:
1. Add the new `Customers` table
2. Add `CustomerId` foreign key to `Users` and `Groups` tables
3. Update relationships to enforce tenant boundaries

```bash
# From your project root
dotnet ef database update
```

If you need to run the migration manually, the SQL script will be similar to:

```sql
-- Create Customers table
CREATE TABLE [Customers] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);

-- Add CustomerId to Users table
ALTER TABLE [Users] ADD [CustomerId] nvarchar(450) NULL;

-- Add CustomerId to Groups table
ALTER TABLE [Groups] ADD [CustomerId] nvarchar(450) NULL;

-- Create indexes
CREATE UNIQUE INDEX [IX_Customers_TenantId] ON [Customers] ([TenantId]);
CREATE INDEX [IX_Users_CustomerId] ON [Users] ([CustomerId]);
CREATE INDEX [IX_Groups_CustomerId] ON [Groups] ([CustomerId]);
```

## Step 2: Data Migration

### Create a Default Customer

First, create a default customer to associate with existing data:

```sql
-- Insert default customer
INSERT INTO [Customers] ([Id], [Name], [TenantId], [IsActive], [CreatedAt])
VALUES ('default-customer-id', 'Default Customer', 'default', 1, GETUTCDATE());
```

### Migrate Existing Data

Update existing users and groups to associate them with the default customer:

```sql
-- Associate existing users with default customer
UPDATE [Users]
SET [CustomerId] = 'default-customer-id'
WHERE [CustomerId] IS NULL;

-- Associate existing groups with default customer
UPDATE [Groups]
SET [CustomerId] = 'default-customer-id'
WHERE [CustomerId] IS NULL;
```

### Make CustomerId Required

After migrating all data, update the schema to make CustomerId required:

```sql
-- Make CustomerId required for Users
ALTER TABLE [Users]
ALTER COLUMN [CustomerId] nvarchar(450) NOT NULL;

-- Make CustomerId required for Groups
ALTER TABLE [Groups]
ALTER COLUMN [CustomerId] nvarchar(450) NOT NULL;

-- Add foreign key constraints
ALTER TABLE [Users] 
ADD CONSTRAINT [FK_Users_Customers_CustomerId] 
FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]);

ALTER TABLE [Groups] 
ADD CONSTRAINT [FK_Groups_Customers_CustomerId] 
FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]);
```

## Step 3: Configuration Updates

### Update Application Settings

Modify your `appsettings.json` to include tenant configuration:

```json
{
  "TenantConfiguration": {
    "UseJwtClaims": true,
    "JwtClaimName": "tenant_id",
    "UseHeader": true,
    "HeaderName": "X-Tenant-ID",
    "RequireTenant": false  // Initially set to false for backward compatibility
  }
}
```

Setting `RequireTenant` to `false` initially allows existing clients to continue working without tenant context during the transition period.

## Step 4: Client Application Updates

### Phase 1: Backward Compatibility Mode

In the initial phase, configure the middleware to provide a default tenant when none is specified:

```csharp
// In CustomerContextMiddleware.cs
if (string.IsNullOrEmpty(tenantId))
{
    // Use default customer for backward compatibility
    context.Items["CustomerId"] = "default-customer-id";
    return await _next(context);
}
```

This allows existing clients to continue working without changes.

### Phase 2: Update Client Applications

Update client applications to include tenant context in requests, either by:

1. Including the `X-Tenant-ID` header in all requests
2. Updating JWT tokens to include the tenant claim

### Phase 3: Enforce Tenant Context

After all clients have been updated:

1. Update configuration to require tenant context:

```json
{
  "TenantConfiguration": {
    "RequireTenant": true
  }
}
```

2. Remove the default tenant fallback from middleware

## Verification Steps

After migration, verify the following:

1. All existing users and groups are associated with the default customer
2. New users and groups require a valid tenant context
3. Tenant isolation is enforced (tenant A cannot access tenant B's data)
4. Client applications can successfully authenticate and specify tenant context

## Troubleshooting

### Common Issues

**Issue: Missing Tenant ID in Requests**
- Symptom: 400 Bad Request with "Missing tenant identifier"
- Solution: Add tenant header or JWT claim

**Issue: Foreign Key Constraint Failures**
- Symptom: Database errors during data migration
- Solution: Ensure all users and groups are associated with a valid customer ID

**Issue: Authentication Failures**
- Symptom: 401 Unauthorized errors after migration
- Solution: Verify JWT configuration includes tenant claims

## Rollback Plan

If critical issues are encountered, roll back using the following steps:

1. Restore the database from backup
2. Revert to the pre-migration application version
3. Update DNS/load balancers to point to the reverted application

## Conclusion

This migration enables your SCIM Service Provider to operate in a multi-tenant environment with proper data isolation between tenants. After completing the migration, each tenant's data remains strictly separated, allowing you to serve multiple customers from a single deployment.
