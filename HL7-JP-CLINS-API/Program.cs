using HL7_JP_CLINS_API.Services;
using HL7_JP_CLINS_Tranforms.Transformers;
using HL7_JP_CLINS_Tranforms.Interfaces;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure JSON options for Newtonsoft.Json (FHIR compatibility)
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.Formatting = Formatting.Indented;
    });

// Configure CORS policy for healthcare applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("HL7Policy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Type", "X-Correlation-ID");
    });
});

// Register HL7-JP-CLINS Transformers
builder.Services.AddScoped<EReferralTransformer>();
builder.Services.AddScoped<EDischargeSummaryTransformer>();
builder.Services.AddScoped<ECheckupTransformer>();

// Register FHIR Serialization Service
builder.Services.AddScoped<IFhirSerializationService, FhirSerializationService>();

// Configure Swagger/OpenAPI with detailed documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HL7-JP-CLINS API",
        Version = "v1.0.0",
        Description = "API for converting hospital data to HL7 FHIR R4 format according to JP-CLINS v1.11.0 specification",
        Contact = new OpenApiContact
        {
            Name = "HL7-JP-CLINS Development Team",
            Url = new Uri("https://jpfhir.jp/fhir/clins/igv1/index.html")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments for API documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add security definitions for future authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add operation examples (requires Swashbuckle.AspNetCore.Annotations)
    // options.EnableAnnotations();
});

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add health checks
builder.Services.AddHealthChecks();

// Configure HTTP client factory (for external service integration if needed)
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable CORS
app.UseCors("HL7Policy");

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HL7-JP-CLINS API v1");
        options.RoutePrefix = string.Empty; // Set Swagger UI at apps root
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DisplayRequestDuration();
    });

    // Detailed error pages in development
    app.UseDeveloperExceptionPage();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Security headers
app.UseHttpsRedirection();

// Request/Response logging middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request {Method} {Path} started with correlation ID {CorrelationId}",
        context.Request.Method, context.Request.Path, correlationId);

    await next();

    logger.LogInformation("Request {Method} {Path} completed with status {StatusCode} (correlation ID: {CorrelationId})",
        context.Request.Method, context.Request.Path, context.Response.StatusCode, correlationId);
});

// Authentication and Authorization (placeholder for future implementation)
// app.UseAuthentication();
// app.UseAuthorization();

// Map health checks
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// Application startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("HL7-JP-CLINS-API starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("JP-CLINS Version: v1.11.0");
logger.LogInformation("FHIR Version: R4");

app.Run();
