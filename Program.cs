using Tidawnloader.Components;
using Tidawnloader.Services;
using Tidawnloader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dbHost = builder.Configuration["Db:host"];
var dbPort = builder.Configuration["Db:port"];
var dbName = builder.Configuration["Db:name"];
var dbUser = builder.Configuration["Db:user"];
var dbPassword = builder.Configuration["Db:password"];

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Store data protection keys in StoragePath instead of ~/.aspnet/DataProtection-Keys
var dataProtectionBuilder = builder.Services.AddDataProtection();

var storagePath = builder.Configuration["StoragePath"]!;

dataProtectionBuilder.PersistKeysToFileSystem(
    new DirectoryInfo(Path.Combine(storagePath, "DataProtection-Keys"))
);

// HttpClient used by Downloader
builder.Services.AddHttpClient("Default", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddScoped<Request>();
builder.Services.AddScoped<Downloader>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // Run migrations on startup
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
