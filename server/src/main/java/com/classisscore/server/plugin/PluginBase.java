package com.classisscore.server.plugin;

public abstract class PluginBase {

    private PluginManifest manifest;

    public PluginManifest getManifest() {
        return manifest;
    }

    public void setManifest(PluginManifest manifest) {
        this.manifest = manifest;
    }

    public abstract void onEnable();

    public abstract void onDisable();

    public String getName() {
        return manifest != null ? manifest.getName() : "Unknown";
    }
}
