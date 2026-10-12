import AppKit

@MainActor final class MapLauncherPlugin {
    private static var instance: MapLauncherPlugin?
    static func register() {
        if instance == nil { instance = MapLauncherPlugin() }
    }

    private init() {
        let channel = NativeChannels.channel("dotnative.map-launcher")
        channel.handle("installed") { _, reply in
            var maps: [String] = []
            if opens("maps") { maps.append("apple") }
            if opens("comgooglemaps") { maps.append("google") }
            reply.success(.list(maps.map { PluginValue.string($0) }))
        }
        channel.handle("showMarker") { arguments, reply in
            let fields = arguments.fields
            guard let map = fields["map"]?.string,
                case .double(let latitude)? = fields["latitude"],
                case .double(let longitude)? = fields["longitude"],
                let title = fields["title"]?.string, !title.isEmpty
            else {
                reply.failure("invalid_argument", "A map, coordinates and a title are required")
                return
            }
            let scheme = map == "apple" ? "maps" : "comgooglemaps"
            var components = URLComponents()
            components.scheme = scheme
            components.queryItems = [
                URLQueryItem(
                    name: "q",
                    value: map == "apple" ? title : "\(latitude),\(longitude)"
                ),
                URLQueryItem(name: "ll", value: "\(latitude),\(longitude)"),
            ]
            guard let url = components.url, NSWorkspace.shared.open(url) else {
                reply.failure("map_unavailable", "That map is not available")
                return
            }
            reply.success()
        }
    }

    private func opens(_ scheme: String) -> Bool {
        guard let url = URL(string: "\(scheme)://") else { return false }
        return NSWorkspace.shared.urlForApplication(toOpen: url) != nil
    }
}
