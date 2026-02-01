using SieConverterApi.Services;
using System.Text;

// Register code pages encoding provider for CP437/PC8 support (SIE files)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register SIE converter services
builder.Services.AddScoped<ISieParserService, SieParserService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<ITempFileService, TempFileService>();

// Configure CORS for frontend - allow specific origins
var corsOrigins = builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? 
    new[] { "http://localhost:8080", "http://127.0.0.1:8080" };

// Add Render default origins if not already present
var defaultOrigins = new List<string>(corsOrigins);
if (!defaultOrigins.Contains("https://sie-converter.onrender.com"))
    defaultOrigins.Add("https://sie-converter.onrender.com");
if (!defaultOrigins.Contains("https://sie-converter-api.onrender.com"))
    defaultOrigins.Add("https://sie-converter-api.onrender.com");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(defaultOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure request size limits for security
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

var app = builder.Build();

// CORS must be first to handle preflight requests
app.UseCors("AllowFrontend");

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
