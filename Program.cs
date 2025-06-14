using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Services;
using ScimServiceProvider.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Configure the default output formatters to use SCIM content type
    options.FormatterMappings.SetMediaTypeMappingForFormat("scim", "application/scim+json");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    // Exclude null values from JSON responses to comply with SCIM 2.0 standards
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    // Add custom boolean converter to handle string to boolean conversion
    options.JsonSerializerOptions.Converters.Add(new ScimServiceProvider.Converters.BooleanJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new ScimServiceProvider.Converters.NullableBooleanJsonConverter());
});

// Configure Entity Framework
builder.Services.AddDbContext<ScimDbContext>(options =>
    options.UseInMemoryDatabase("ScimDatabase")); // Using InMemory for demo, replace with SQL Server for production

// Register SCIM services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add HTTP request logging for development
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.MediaTypeOptions.AddText("application/scim+json");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SCIM Service Provider API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTP request logging in all environments for SCIM debugging
app.UseHttpLogging();

// Add static files support for admin UI
app.UseStaticFiles();

// Add request logging middleware to capture all requests
app.UseRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Add customer context middleware
app.UseCustomerContext();

app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScimDbContext>();
    context.Database.EnsureCreated();
    
    // Add test customer for SCIM validation (needed in all environments)
    if (!context.Customers.Any())
    {
        var testCustomer = new ScimServiceProvider.Models.Customer
        {
            Id = "test-customer-1",
            Name = "Test Customer",
            TenantId = "tenant1",
            IsActive = true,
            Created = DateTime.UtcNow
        };
        context.Customers.Add(testCustomer);
        await context.SaveChangesAsync();
    }
}

app.Run();

// Make Program class public for integration tests
namespace ScimServiceProvider 
{
    public partial class Program { }
}
