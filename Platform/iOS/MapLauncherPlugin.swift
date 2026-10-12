import MapKit
import UIKit

@MainActor final class MapLauncherPlugin {
    private static var instance: MapLauncherPlugin?
    static func register() {
        if instance == nil { instance = MapLauncherPlugin() }
    }

    private init() {
        let channel = NativeChannels.channel("dotnative.map-launcher")
        channel.handle("installed") { _, reply in
            var maps = ["apple"]
            if let url = URL(string: "comgooglemaps://"), UIApplication.shared.canOpenURL(url) {
                maps.append("google")
            }
            reply.success(.list(maps.map { PluginValue.string($0) }))
        }
        channel.handle("showMarker") {
            [weak self] arguments, reply in
            self?.show(arguments, reply)
                ?? reply.failure("unavailable", "Map launcher is unavailable")
        }
    }

    private func show(_ arguments: PluginValue, _ reply: PluginReply) {
        let fields = arguments.fields
        guard let map = fields["map"]?.string,
            case .double(let latitude)? = fields["latitude"],
            case .double(let longitude)? = fields["longitude"],
            let title = fields["title"]?.string, !title.isEmpty
        else {
            reply.failure("invalid_argument", "A map, coordinates and a title are required")
            return
        }
        let coordinate = CLLocationCoordinate2D(latitude: latitude, longitude: longitude)
        if map == "apple" {
            let item = MKMapItem(placemark: MKPlacemark(coordinate: coordinate))
            item.name = title
            item.openInMaps()
            reply.success()
            return
        }
        guard map == "google",
            var components = URLComponents(string: "comgooglemaps://")
        else {
            reply.failure("invalid_argument", "Only Apple Maps and Google Maps are supported")
            return
        }
        components.queryItems = [
            URLQueryItem(name: "q", value: "\(latitude),\(longitude)"),
            URLQueryItem(name: "center", value: "\(latitude),\(longitude)"),
        ]
        guard let url = components.url else {
            reply.failure("invalid_argument", "Could not build the Google Maps URL")
            return
        }
        UIApplication.shared.open(url, options: [:]) { opened in
            if opened { reply.success() }
            else { reply.failure("map_unavailable", "Google Maps is not available") }
        }
    }
}
