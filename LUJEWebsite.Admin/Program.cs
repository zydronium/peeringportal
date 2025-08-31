var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

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

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
