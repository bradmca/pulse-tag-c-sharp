using PulseTag.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure antiforgery to not require validation
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = true;
    options.Cookie.Name = "PulseTag.Antiforgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddScoped(sp => 
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5100";
    return new HttpClient 
    { 
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(120)  // 2 minute timeout
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
