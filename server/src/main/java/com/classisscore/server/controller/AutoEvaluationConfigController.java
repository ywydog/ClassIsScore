package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.entity.AutoEvaluationConfig;
import com.classisscore.server.service.AutoEvaluationConfigService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/auto-evaluation-configs")
public class AutoEvaluationConfigController {

    @Autowired
    private AutoEvaluationConfigService autoEvaluationConfigService;

    @GetMapping
    public ApiResult<List<AutoEvaluationConfig>> list() {
        return ApiResult.success(autoEvaluationConfigService.listConfigs());
    }

    @PostMapping
    public ApiResult<AutoEvaluationConfig> create(@RequestBody AutoEvaluationConfig config) {
        return ApiResult.success(autoEvaluationConfigService.createConfig(config));
    }

    @PutMapping("/{id}")
    public ApiResult<AutoEvaluationConfig> update(@PathVariable Long id, @RequestBody AutoEvaluationConfig config) {
        return ApiResult.success(autoEvaluationConfigService.updateConfig(id, config));
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        autoEvaluationConfigService.deleteConfig(id);
        return ApiResult.success();
    }

    @PutMapping("/{id}/toggle")
    public ApiResult<AutoEvaluationConfig> toggle(@PathVariable Long id) {
        return ApiResult.success(autoEvaluationConfigService.toggleConfig(id));
    }
}
