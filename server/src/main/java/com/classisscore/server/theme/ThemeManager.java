package com.classisscore.server.theme;

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
public class ThemeManager {

    private final List<ThemeManifest> themes = new ArrayList<>();
    private final AtomicLong idGenerator = new AtomicLong(1);
    private final ObjectMapper objectMapper = new ObjectMapper();

    private String getThemeDir() {
        return System.getProperty("user.home") + "/.classisscore/themes/";
    }

    @PostConstruct
    public void init() {
        scanThemes();
    }

    public void scanThemes() {
        themes.clear();
        Path dirPath = Paths.get(getThemeDir());
        if (!Files.exists(dirPath)) {
            try {
                Files.createDirectories(dirPath);
            } catch (IOException e) {
                return;
            }
        }

        File dir = dirPath.toFile();
        File[] files = dir.listFiles((d, name) -> name.endsWith(".cistheme"));
        if (files != null) {
            for (File file : files) {
                ThemeManifest manifest = getThemeManifest(file);
                if (manifest != null) {
                    themes.add(manifest);
                }
            }
        }
    }

    public ThemeManifest getThemeManifest(File file) {
        try (JarFile jarFile = new JarFile(file)) {
            ZipEntry entry = jarFile.getEntry("theme.json");
            if (entry == null) {
                ThemeManifest manifest = ThemeManifest.builder()
                        .id(idGenerator.getAndIncrement())
                        .name(file.getName().replace(".cistheme", ""))
                        .version("1.0.0")
                        .enabled(false)
                        .filePath(file.getAbsolutePath())
                        .build();
                return manifest;
            }

            String content = new String(jarFile.getInputStream(entry).readAllBytes());
            ThemeManifest manifest = objectMapper.readValue(content, ThemeManifest.class);
            manifest.setId(idGenerator.getAndIncrement());
            manifest.setFilePath(file.getAbsolutePath());
            if (manifest.getEnabled() == null) {
                manifest.setEnabled(false);
            }
            return manifest;
        } catch (IOException e) {
            ThemeManifest manifest = ThemeManifest.builder()
                    .id(idGenerator.getAndIncrement())
                    .name(file.getName().replace(".cistheme", ""))
                    .version("1.0.0")
                    .enabled(false)
                    .filePath(file.getAbsolutePath())
                    .build();
            return manifest;
        }
    }

    public List<ThemeManifest> listThemes() {
        return new ArrayList<>(themes);
    }

    public String getThemeCss(Long id) {
        for (ThemeManifest manifest : themes) {
            if (manifest.getId().equals(id)) {
                try (JarFile jarFile = new JarFile(manifest.getFilePath())) {
                    ZipEntry entry = jarFile.getEntry("theme.css");
                    if (entry != null) {
                        return new String(jarFile.getInputStream(entry).readAllBytes());
                    }
                } catch (IOException e) {
                    return null;
                }
            }
        }
        return null;
    }

    public boolean toggleTheme(Long id, boolean enabled) {
        for (ThemeManifest manifest : themes) {
            if (manifest.getId().equals(id)) {
                manifest.setEnabled(enabled);
                return true;
            }
        }
        return false;
    }

    public boolean deleteTheme(Long id) {
        for (ThemeManifest manifest : themes) {
            if (manifest.getId().equals(id)) {
                File file = new File(manifest.getFilePath());
                boolean deleted = file.delete();
                if (deleted) {
                    themes.remove(manifest);
                }
                return deleted;
            }
        }
        return false;
    }
}
