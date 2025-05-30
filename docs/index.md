# SCIM Service Provider Multi-Tenant Documentation

## Overview
This documentation covers the multi-tenant architecture implemented in the SCIM Service Provider, which allows the application to serve multiple customers (tenants) within a single instance while maintaining strict data isolation between tenants.

## Table of Contents

### Architecture and Design
1. [Multi-Tenant Architecture](multi_tenant_architecture.md) - Detailed description of the multi-tenant architecture design
2. [Architecture Diagram](multi_tenant_architecture_diagram.md) - Visual representation of the multi-tenant architecture
3. [Data Model](data_model.md) - Entity relationship diagram and data model details

### Guides and How-To's
1. [Usage Guide](multi_tenant_usage_guide.md) - How to configure and use the multi-tenant SCIM Service Provider
2. [Migration Guide](migration_guide.md) - How to migrate from single-tenant to multi-tenant architecture

## Key Features

### Customer/Tenant Management
- Support for multiple customers (tenants) within a single deployment
- Customer-specific SCIM resources (Users and Groups)
- Tenant context extraction from HTTP headers or JWT claims

### Data Isolation
- Complete data isolation between tenants
- Tenant-specific filtering at the service layer
- Customer ID foreign key relationships in the database

### Security
- Tenant context validation and authorization
- Prevention of cross-tenant data access
- Configurable tenant identification methods

## Getting Started

To set up and use the multi-tenant SCIM Service Provider, start with the [Usage Guide](multi_tenant_usage_guide.md).

If you're migrating an existing single-tenant deployment, refer to the [Migration Guide](migration_guide.md).

## Technical Terminology

- **Tenant**: A customer or organization using the SCIM Service Provider
- **Tenant Context**: The current tenant identifier associated with a request
- **Data Isolation**: Ensuring one tenant cannot access another tenant's data
- **Customer ID**: The unique identifier for a customer within the system
