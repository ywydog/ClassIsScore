package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.service.SettingsService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/settings")
public class SettingsController {

    @Autowired
    private SettingsService settingsService;

    @GetMapping
    public ApiResult<Map<String, String>> getAll() {
        return ApiResult.success(settingsService.getAllSettings());
    }

    @PutMapping
    public ApiResult<Void> updateAll(@RequestBody Map<String, String> settings) {
        settingsService.updateSettings(settings);
        return ApiResult.success();
    }

    @GetMapping("/{key}")
    public ApiResult<String> get(@PathVariable String key) {
        String value = settingsService.getSetting(key);
        if (value == null) {
            return ApiResult.error("设置项不存在");
        }
        return ApiResult.success(value);
    }

    @PutMapping("/{key}")
    public ApiResult<Void> update(@PathVariable String key, @RequestBody Map<String, String> body) {
        String value = body.get("value");
        settingsService.updateSetting(key, value);
        return ApiResult.success();
    }
}
