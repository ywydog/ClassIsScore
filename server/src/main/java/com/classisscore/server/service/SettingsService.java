package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.entity.AdminSettings;
import com.classisscore.server.mapper.AdminSettingsMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Service
public class SettingsService extends ServiceImpl<AdminSettingsMapper, AdminSettings> {

    public Map<String, String> getAllSettings() {
        List<AdminSettings> settings = this.list();
        return settings.stream()
                .collect(Collectors.toMap(AdminSettings::getSettingKey, AdminSettings::getSettingValue));
    }

    public String getSetting(String key) {
        LambdaQueryWrapper<AdminSettings> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(AdminSettings::getSettingKey, key);
        AdminSettings setting = this.getOne(wrapper);
        return setting != null ? setting.getSettingValue() : null;
    }

    @Transactional
    public void updateSetting(String key, String value) {
        LambdaQueryWrapper<AdminSettings> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(AdminSettings::getSettingKey, key);
        AdminSettings setting = this.getOne(wrapper);
        if (setting != null) {
            setting.setSettingValue(value);
            setting.setUpdatedAt(LocalDateTime.now());
            this.updateById(setting);
        } else {
            setting = AdminSettings.builder()
                    .settingKey(key)
                    .settingValue(value)
                    .updatedAt(LocalDateTime.now())
                    .build();
            this.save(setting);
        }
    }

    @Transactional
    public void updateSettings(Map<String, String> settings) {
        settings.forEach(this::updateSetting);
    }
}
