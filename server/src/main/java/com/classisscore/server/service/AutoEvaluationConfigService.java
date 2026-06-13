package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.entity.AutoEvaluationConfig;
import com.classisscore.server.mapper.AutoEvaluationConfigMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;

@Service
public class AutoEvaluationConfigService extends ServiceImpl<AutoEvaluationConfigMapper, AutoEvaluationConfig> {

    public java.util.List<AutoEvaluationConfig> listConfigs() {
        LambdaQueryWrapper<AutoEvaluationConfig> wrapper = new LambdaQueryWrapper<>();
        wrapper.orderByDesc(AutoEvaluationConfig::getCreatedAt);
        return this.list(wrapper);
    }

    @Transactional
    public AutoEvaluationConfig createConfig(AutoEvaluationConfig config) {
        config.setCreatedAt(LocalDateTime.now());
        if (config.getIsEnabled() == null) {
            config.setIsEnabled(false);
        }
        this.save(config);
        return config;
    }

    @Transactional
    public AutoEvaluationConfig updateConfig(Long id, AutoEvaluationConfig config) {
        config.setId(id);
        this.updateById(config);
        return this.getById(id);
    }

    @Transactional
    public boolean deleteConfig(Long id) {
        return this.removeById(id);
    }

    @Transactional
    public AutoEvaluationConfig toggleConfig(Long id) {
        AutoEvaluationConfig config = this.getById(id);
        if (config != null) {
            config.setIsEnabled(!config.getIsEnabled());
            this.updateById(config);
        }
        return config;
    }
}
