using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyBusApp;
using MyBusApp.Configuration;
using MyBusApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// carregar appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// bind manual para ApiSettings
var apiSettings = new ApiSettings();
builder.Configuration.GetSection("Apis").Bind(apiSettings);

// registar ApiSettings como singleton
builder.Services.AddSingleton(apiSettings);
builder.Services.AddSingleton<AppLogger>();

// Carris Metropolitana — serviço primário
builder.Services.AddScoped<IBusService, CarrisMetropolitanaService>();

// Carris Lisboa — fallback quando a Metropolitana não encontrar a linha
builder.Services.AddScoped<IBusService, CarrisLisboaService>();

// Transitland — último fallback
builder.Services.AddScoped<IBusService, TransitlandService>();

builder.Services.AddScoped<BusServiceRouter>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
