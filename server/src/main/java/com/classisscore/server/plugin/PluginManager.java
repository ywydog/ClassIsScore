package com.classisscore.server.plugin;

import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.annotation.PostConstruct;
import org.springframework.stereotype.Component;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.atomic.AtomicLong;
import java.util.jar.JarFile;
import java.util.zip.ZipEntry;

@Component
public class PluginManager {

    private final List<PluginManifest> plugins = new ArrayList<>();
    private final AtomicLong idGenerator = new AtomicLong(1);
    private final ObjectMapper objectMapper = new ObjectMapper();

    private String getPluginDir() {
        return System.getProperty("user.home") + "/.classisscore/plugins/";
    }

    @PostConstruct
    public void init() {
        scanPlugins();
    }

    public void scanPlugins() {
        plugins.clear();
        Path dirPath = Paths.get(getPluginDir());
        if (!Files.exists(dirPath)) {
            try {
                Files.createDirectories(dirPath);
            } catch (IOException e) {
                return;
            }
        }

        File dir = dirPath.toFile();
        File[] files = dir.listFiles((d, name) -> name.endsWith(".cispg"));
        if (files != null) {
            for (File file : files) {
                PluginManifest manifest = getPluginManifest(file);
                if (manifest != null) {
                    plugins.add(manifest);
                }
            }
        }
    }

    public PluginManifest getPluginManifest(File file) {
        try (JarFile jarFile = new JarFile(file)) {
            ZipEntry entry = jarFile.getEntry("plugin.json");
            if (entry == null) {
                PluginManifest manifest = PluginManifest.builder()
                        .id(idGenerator.getAndIncrement())
                        .name(file.getName().replace(".cispg", ""))
                        .version("1.0.0")
                        .enabled(false)
                        .filePath(file.getAbsolutePath())
                        .build();
                return manifest;
            }

            String content = new String(jarFile.getInputStream(entry).readAllBytes());
            PluginManifest manifest = objectMapper.readValue(content, PluginManifest.class);
            manifest.setId(idGenerator.getAndIncrement());
            manifest.setFilePath(file.getAbsolutePath());
            if (manifest.getEnabled() == null) {
                manifest.setEnabled(false);
            }
            return manifest;
        } catch (IOException e) {
            PluginManifest manifest = PluginManifest.builder()
                    .id(idGenerator.getAndIncrement())
                    .name(file.getName().replace(".cispg", ""))
                    .version("1.0.0")
                    .enabled(false)
                    .filePath(file.getAbsolutePath())
                    .build();
            return manifest;
        }
    }

    public List<PluginManifest> listPlugins() {
        return new ArrayList<>(plugins);
    }

    public boolean togglePlugin(Long id, boolean enabled) {
        for (PluginManifest manifest : plugins) {
            if (manifest.getId().equals(id)) {
                manifest.setEnabled(enabled);
                return true;
            }
        }
        return false;
    }

    public boolean deletePlugin(Long id) {
        for (PluginManifest manifest : plugins) {
            if (manifest.getId().equals(id)) {
                File file = new File(manifest.getFilePath());
                boolean deleted = file.delete();
                if (deleted) {
                    plugins.remove(manifest);
                }
                return deleted;
            }
        }
        return false;
    }
}
