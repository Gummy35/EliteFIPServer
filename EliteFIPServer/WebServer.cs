using EliteFIPServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text.Json;

public class WebServer
{
    private readonly CoreServer core;
    private IHost? host;

    public WebServer(CoreServer core)
    {
        this.core = core;
    }

    public void Start(int port = 5000)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();

        var fileProvider = new PhysicalFileProvider(
            Path.Combine(AppContext.BaseDirectory, "wwwroot")
        );

        // Page par défaut : index.html
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider,
            DefaultFileNames = new[] { "index.html" }
        });

        // Fichiers statiques
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });

        // Endpoint JSON pour ExoView
        app.MapGet("/exo", () =>
        {
            return Results.Json(core.ExoView, new JsonSerializerOptions { WriteIndented = true });
        });

        app.RunAsync(); // ⚡ démarre le serveur en tâche de fond
    }


    public void Stop()
    {
        host?.StopAsync().Wait();
        host?.Dispose();
    }
}
