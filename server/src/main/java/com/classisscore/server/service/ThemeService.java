package com.classisscore.server.service;

import com.classisscore.server.theme.ThemeManager;
import com.classisscore.server.theme.ThemeManifest;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

@Service
public class ThemeService {

    @Autowired
    private ThemeManager themeManager;

    public List<ThemeManifest> listThemes() {
        return themeManager.listThemes();
    }

    public String getThemeCss(Long id) {
        return themeManager.getThemeCss(id);
    }

    public ThemeManifest uploadTheme(MultipartFile file) throws IOException {
        String themeDir = System.getProperty("user.home") + "/.classisscore/themes/";
        Path dirPath = Paths.get(themeDir);
        if (!Files.exists(dirPath)) {
            Files.createDirectories(dirPath);
        }

        String filename = file.getOriginalFilename();
        if (filename == null || !filename.endsWith(".cistheme")) {
            throw new IllegalArgumentException("主题文件必须以 .cistheme 结尾");
        }

        Path filePath = dirPath.resolve(filename);
        file.transferTo(filePath.toFile());

        themeManager.scanThemes();
        return themeManager.getThemeManifest(filePath.toFile());
    }

    public boolean toggleTheme(Long id, boolean enabled) {
        return themeManager.toggleTheme(id, enabled);
    }

    public boolean deleteTheme(Long id) {
        return themeManager.deleteTheme(id);
    }
}
