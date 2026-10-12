# DotNative.MapLauncher

Opens a marker in a map application that is already installed.

```csharp
builder.Services.AddMapLauncher();
var maps = await services.MapLauncher.InstalledMapsAsync();
await services.MapLauncher.ShowMarkerAsync(MapApp.Google, 44.7866, 20.4489, "Beograd");
```

| Platform | Apple Maps | Google Maps |
| --- | --- | --- |
| iOS | `MKMapItem` | `comgooglemaps://` |
| Android | unavailable | `geo:` intent, package `com.google.android.apps.maps` |
| macOS | `maps:` | `comgooglemaps:` |
| Windows, Linux | unavailable | browser URL |

On iOS, `Info.plist` needs `comgooglemaps` in `LSApplicationQueriesSchemes`.
Without that key, `canOpenURL` reports Google Maps as missing.

## Service access

Import `DotNative.MapLauncher` and register `AddMapLauncher()` before building
the provider. `services.MapLauncher` is the C# 14 property on `net10.0`.
`services.MapLauncher()` is the method on `net9.0`.
