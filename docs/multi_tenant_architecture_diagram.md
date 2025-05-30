# Multi-Tenant Architecture Diagram

The following architecture diagram illustrates the multi-tenant design of the SCIM Service Provider:

```mermaid
architecture-beta
    group scim_service(cloud)[SCIM Service Provider]
        
        group api_layer(server)[API Layer]
            service scim_endpoints(internet)[SCIM Endpoints] in api_layer
            service controllers(database)[Controllers] in api_layer
            service middleware(disk)[Customer Context Middleware] in api_layer
        
        group service_layer(server)[Service Layer]
            service user_service(database)[User Service] in service_layer
            service group_service(database)[Group Service] in service_layer
            service customer_service(database)[Customer Service] in service_layer
        
        group data_layer(server)[Data Layer]
            service db_context(database)[SCIM DB Context] in data_layer
            service customers(disk)[Customers] in data_layer
            service users(disk)[Users] in data_layer
            service groups(disk)[Groups] in data_layer
    
    group client_tenant1(cloud)[Tenant 1]
        service client1(internet)[Client] in client_tenant1
    
    group client_tenant2(cloud)[Tenant 2]
        service client2(internet)[Client] in client_tenant2
    
    client1:R --> L:middleware
    client2:R --> L:middleware
    
    middleware:R -- L:controllers
    controllers:R -- L:user_service
    controllers:R -- L:group_service
    controllers:R -- L:customer_service
    
    user_service:R -- L:db_context
    group_service:R -- L:db_context
    customer_service:R -- L:db_context
    
    db_context:R -- L:customers
    db_context:R -- L:users
    db_context:R -- L:groups
```

## Key Components

1. **Client Tenants**: Separate clients representing different organizations or tenants that connect to the SCIM Service.

2. **Customer Context Middleware**: Intercepts all requests, extracts tenant information, and establishes tenant context.

3. **Controllers**: Use tenant context to ensure operations are tenant-specific.

4. **Service Layer**: Implements tenant-aware business logic with customerId parameters.

5. **Data Layer**: Enforces data isolation between tenants at the database level.

## Data Flow

1. Clients from different tenants send requests to the SCIM endpoints.
2. Customer Context Middleware identifies the tenant from headers or JWT claims.
3. Controllers retrieve tenant context and pass it to service methods.
4. Services filter data operations by tenant ID.
5. Database queries include tenant filters, ensuring data isolation.

This architecture ensures that each tenant's data remains completely isolated while maintaining a single deployment of the application.
