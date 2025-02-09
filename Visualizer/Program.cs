using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Model;
using Visualizer;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
var rmq = await RabbitMqManager.Initialize();
rmq.QueueName = "visualizer";

builder.Services.AddSingleton<RabbitMqManager>(rmq);

await builder.Build().RunAsync();


