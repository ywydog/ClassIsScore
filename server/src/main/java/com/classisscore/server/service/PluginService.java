package com.classisscore.server.service;

import com.classisscore.server.plugin.PluginManager;
import com.classisscore.server.plugin.PluginManifest;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

@Service
public class PluginService {

    @Autowired
    private PluginManager pluginManager;

    public List<PluginManifest> listPlugins() {
        return pluginManager.listPlugins();
    }

    public PluginManifest uploadPlugin(MultipartFile file) throws IOException {
        String pluginDir = System.getProperty("user.home") + "/.classisscore/plugins/";
        Path dirPath = Paths.get(pluginDir);
        if (!Files.exists(dirPath)) {
            Files.createDirectories(dirPath);
        }

        String filename = file.getOriginalFilename();
        if (filename == null || !filename.endsWith(".cispg")) {
            throw new IllegalArgumentException("插件文件必须以 .cispg 结尾");
        }

        Path filePath = dirPath.resolve(filename);
        file.transferTo(filePath.toFile());

        pluginManager.scanPlugins();
        return pluginManager.getPluginManifest(filePath.toFile());
    }

    public boolean togglePlugin(Long id, boolean enabled) {
        return pluginManager.togglePlugin(id, enabled);
    }

    public boolean deletePlugin(Long id) {
        return pluginManager.deletePlugin(id);
    }
}
