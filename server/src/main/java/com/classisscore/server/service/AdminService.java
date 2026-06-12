package com.classisscore.server.service;

import com.classisscore.server.entity.AdminSettings;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.Map;

@Service
public class AdminService {

    @Autowired
    private SettingsService settingsService;

    public Map<String, Object> login(String username, String password) {
        String storedUsername = settingsService.getSetting("admin_username");
        String storedPassword = settingsService.getSetting("admin_password");

        if (storedUsername == null) {
            storedUsername = "admin";
        }
        if (storedPassword == null) {
            storedPassword = "admin123";
        }

        Map<String, Object> result = new HashMap<>();
        if (storedUsername.equals(username) && storedPassword.equals(password)) {
            result.put("success", true);
            result.put("token", "admin-token-" + System.currentTimeMillis());
            result.put("username", username);
        } else {
            result.put("success", false);
            result.put("message", "用户名或密码错误");
        }
        return result;
    }

    public boolean changePassword(String oldPassword, String newPassword) {
        String storedPassword = settingsService.getSetting("admin_password");
        if (storedPassword == null) {
            storedPassword = "admin123";
        }
        if (!storedPassword.equals(oldPassword)) {
            return false;
        }
        settingsService.updateSetting("admin_password", newPassword);
        return true;
    }

    public Map<String, Object> getAdminInfo() {
        String username = settingsService.getSetting("admin_username");
        String siteName = settingsService.getSetting("site_name");
        Map<String, Object> info = new HashMap<>();
        info.put("username", username != null ? username : "admin");
        info.put("siteName", siteName != null ? siteName : "ClassIsScore");
        return info;
    }
}
