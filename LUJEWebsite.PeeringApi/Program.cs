var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Required for Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AutoPeer API",
        Version = "v0.1",
        Description = "REST API for AutoPeer peering session management"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoPeer API v1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger
});

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();    // API controller routes

app.Run();
