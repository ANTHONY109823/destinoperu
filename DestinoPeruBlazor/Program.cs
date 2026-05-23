using DestinoPeruBlazor;
using DestinoPeruBlazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ApiBaseUrl: wwwroot/appsettings.json (local) o appsettings.Production.json (Railway)
var apiUrl = (builder.Configuration["ApiBaseUrl"] ?? "https://destinoperu-production.up.railway.app/").TrimEnd('/') + "/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
