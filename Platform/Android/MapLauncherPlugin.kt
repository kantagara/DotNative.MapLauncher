package com.dotnative.plugins

import android.app.Activity
import android.content.Intent
import android.net.Uri

class MapLauncherPlugin(private val activity: Activity) {
    init {
        val channel = NativeChannels.channel("dotnative.map-launcher")
        channel.handle("installed") { _, reply ->
            val maps = mutableListOf<String>()
            if (activity.packageManager.getLaunchIntentForPackage("com.google.android.apps.maps") != null)
                maps.add("google")
            reply.success(maps)
        }
        channel.handle("showMarker") { args, reply ->
            val fields = args as? Map<*, *>
            val latitude = fields?.get("latitude") as? Double
            val longitude = fields?.get("longitude") as? Double
            val title = fields?.get("title") as? String
            if (fields?.get("map") != "google" || latitude == null || longitude == null || title.isNullOrBlank()) {
                reply.failure("invalid_argument", "Google Maps marker coordinates and a title are required")
                return@handle
            }
            val label = Uri.encode(title)
            val intent =
                Intent(
                    Intent.ACTION_VIEW,
                    Uri.parse("geo:$latitude,$longitude?q=$latitude,$longitude($label)")
                ).apply { setPackage("com.google.android.apps.maps") }
            try {
                activity.startActivity(intent)
                reply.success()
            } catch (error: Exception) {
                reply.failure("map_unavailable", error.message ?: "Google Maps is not available")
            }
        }
    }
}
