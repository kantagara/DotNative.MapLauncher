using System.Diagnostics;
using System.Globalization;
using DotNative;
using DotNative.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNative.MapLauncher;

// ─────────────────────────────────────────────────────────────
// Public API
// ─────────────────────────────────────────────────────────────

public enum MapApp
{
    Apple,
    Google,
}

public readonly record struct InstalledMap(MapApp App, string Name);

public interface IMapLauncher
{
    Task<IReadOnlyList<InstalledMap>> InstalledMapsAsync(
        CancellationToken cancellationToken = default);

    Task ShowMarkerAsync(
        MapApp app,
        double latitude,
        double longitude,
        string title,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────────────────────
// Dependency Injection
// ─────────────────────────────────────────────────────────────

public static class MapLauncherServices
{
    public static IServiceCollection AddMapLauncher(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IMapLauncher, MapLauncherService>();
        return services;
    }
}

// ─────────────────────────────────────────────────────────────
// Implementation
// ─────────────────────────────────────────────────────────────

internal sealed class MapLauncherService(
    IPlatformChannels channels,
    PresentationTarget target
) : IMapLauncher
{
    private const string ChannelName = "dotnative.map-launcher";

    private bool IsDesktop =>
        target.Platform is NativePlatform.Windows or NativePlatform.Linux;

    private Task<object?> InvokeAsync(
        string method,
        Dictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        return channels.Get(ChannelName)
            .InvokeAsync(method, arguments, cancellationToken);
    }

    public async Task<IReadOnlyList<InstalledMap>> InstalledMapsAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsDesktop)
            return [new(MapApp.Google, "Google Maps")];

        var value = await InvokeAsync("installed", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (value is not IEnumerable<object?> items)
            throw new InvalidDataException("Invalid installed map list.");

        var maps = new List<InstalledMap>();
        foreach (var item in items)
        {
            if (item is not string name)
                continue;

            maps.Add(
                name switch
                {
                    "apple" => new InstalledMap(MapApp.Apple, "Apple Maps"),
                    "google" => new InstalledMap(MapApp.Google, "Google Maps"),
                    _ => throw new InvalidDataException("Unknown map."),
                }
            );
        }

        return maps;
    }

    public Task ShowMarkerAsync(
        MapApp app,
        double latitude,
        double longitude,
        string title,
        CancellationToken cancellationToken = default)
    {
        Validate(latitude, longitude, title);

        if (IsDesktop)
        {
            if (app != MapApp.Google)
                throw new InvalidOperationException(
                    "Apple Maps is not available on this platform.");

            OpenBrowser(MarkerUrl(latitude, longitude));
            return Task.CompletedTask;
        }

        return InvokeAsync(
            "showMarker",
            new()
            {
                ["map"] = app == MapApp.Apple ? "apple" : "google",
                ["latitude"] = latitude,
                ["longitude"] = longitude,
                ["title"] = title,
            },
            cancellationToken);
    }

    internal static void Validate(double latitude, double longitude, string title)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(latitude));

        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (title.Length > 200)
            throw new ArgumentOutOfRangeException(nameof(title));
    }

    internal static string MarkerUrl(double latitude, double longitude)
    {
        var query = string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1}",
            latitude,
            longitude
        );
        return "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query);
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
