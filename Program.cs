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

// Transitland — fonte principal para linhas de Carris e outras redes
builder.Services.AddScoped<IBusService, TransitlandService>();

// Carris Metropolitana — fallback quando a Transitland não encontrar a linha
builder.Services.AddScoped<IBusService, CarrisMetropolitanaService>();

// Carris Lisboa — desativado; a API pública não está a funcionar
// builder.Services.AddScoped<IBusService, CarrisLisboaService>();

builder.Services.AddScoped<BusServiceRouter>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
