using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
});
builder.Services.AddAntiforgery();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.Use(async (ctx, next) =>
{
    await next();

    if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
    {
        //Re-execute the request so the user gets the error page
        string originalPath = ctx.Request.Path.Value;
        ctx.Items["originalPath"] = originalPath;
        ctx.Request.Path = "/error404";
        await next();
    }
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.Run();
