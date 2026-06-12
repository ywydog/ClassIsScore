package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.theme.ThemeManifest;
import com.classisscore.server.service.ThemeService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/themes")
public class ThemeController {

    @Autowired
    private ThemeService themeService;

    @GetMapping
    public ApiResult<List<ThemeManifest>> list() {
        return ApiResult.success(themeService.listThemes());
    }

    @GetMapping(value = "/{id}/css", produces = MediaType.TEXT_CSS_VALUE)
    public ApiResult<String> getCss(@PathVariable Long id) {
        String css = themeService.getThemeCss(id);
        if (css == null) {
            return ApiResult.error("主题CSS不存在");
        }
        return ApiResult.success(css);
    }

    @PostMapping("/upload")
    public ApiResult<ThemeManifest> upload(@RequestParam("file") MultipartFile file) {
        try {
            return ApiResult.success(themeService.uploadTheme(file));
        } catch (IOException e) {
            return ApiResult.error("上传主题失败: " + e.getMessage());
        } catch (IllegalArgumentException e) {
            return ApiResult.error(e.getMessage());
        }
    }

    @PutMapping("/{id}/toggle")
    public ApiResult<Void> toggle(@PathVariable Long id, @RequestBody Map<String, Boolean> body) {
        boolean enabled = body.getOrDefault("enabled", true);
        boolean success = themeService.toggleTheme(id, enabled);
        if (!success) {
            return ApiResult.error("操作失败");
        }
        return ApiResult.success();
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        boolean success = themeService.deleteTheme(id);
        if (!success) {
            return ApiResult.error("删除主题失败");
        }
        return ApiResult.success();
    }
}
