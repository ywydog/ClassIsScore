package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.plugin.PluginManifest;
import com.classisscore.server.service.PluginService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/plugins")
public class PluginController {

    @Autowired
    private PluginService pluginService;

    @GetMapping
    public ApiResult<List<PluginManifest>> list() {
        return ApiResult.success(pluginService.listPlugins());
    }

    @PostMapping("/upload")
    public ApiResult<PluginManifest> upload(@RequestParam("file") MultipartFile file) {
        try {
            return ApiResult.success(pluginService.uploadPlugin(file));
        } catch (IOException e) {
            return ApiResult.error("上传插件失败: " + e.getMessage());
        } catch (IllegalArgumentException e) {
            return ApiResult.error(e.getMessage());
        }
    }

    @PutMapping("/{id}/toggle")
    public ApiResult<Void> toggle(@PathVariable Long id, @RequestBody Map<String, Boolean> body) {
        boolean enabled = body.getOrDefault("enabled", true);
        boolean success = pluginService.togglePlugin(id, enabled);
        if (!success) {
            return ApiResult.error("操作失败");
        }
        return ApiResult.success();
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        boolean success = pluginService.deletePlugin(id);
        if (!success) {
            return ApiResult.error("删除插件失败");
        }
        return ApiResult.success();
    }
}
