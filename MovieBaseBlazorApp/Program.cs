using MovieBaseBlazorApp.Components;
using MovieBase.Data;
using Microsoft.EntityFrameworkCore;
using MovieBaseBlazorApp.Components.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=movies.db"));

builder.Services.AddScoped<MovieService>();
builder.Services.AddHttpClient<ImdbApi>(client =>
{
    client.BaseAddress = new Uri("https://www.omdbapi.com/"); 
});

builder.Services.AddMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
