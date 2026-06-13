package com.classisscore.server.service;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.HashMap;
import java.util.HexFormat;
import java.util.Map;

@Service
public class AdminService {

    @Autowired
    private SettingsService settingsService;

    private static final String KEY_ADMIN_ENABLED = "admin.enabled";
    private static final String KEY_ADMIN_VERIFICATION_METHOD = "admin.verification_method";
    private static final String KEY_ADMIN_PASSWORD_ENABLED = "admin.password_enabled";
    private static final String KEY_ADMIN_USB_ENABLED = "admin.usb_enabled";
    private static final String KEY_ADMIN_FACE_ENABLED = "admin.face_enabled";
    private static final String KEY_ADMIN_PASSWORD_HASH = "admin.password_hash";
    private static final String KEY_ADMIN_USB_DEVICE_ID = "admin.usb_device_id";

    public Map<String, Object> login(String username, String password) {
        String storedUsername = settingsService.getSetting("admin_username");
        String storedPassword = settingsService.getSetting("admin_password");

        if (storedUsername == null) {
            storedUsername = "admin";
        }
        if (storedPassword == null) {
            storedPassword = "admin123";
        }

        // 如果启用了管理员保护，需要用哈希密码验证
        boolean adminEnabled = Boolean.parseBoolean(
                settingsService.getSetting(KEY_ADMIN_ENABLED) != null
                        ? settingsService.getSetting(KEY_ADMIN_ENABLED) : "false");
        boolean passwordEnabled = Boolean.parseBoolean(
                settingsService.getSetting(KEY_ADMIN_PASSWORD_ENABLED) != null
                        ? settingsService.getSetting(KEY_ADMIN_PASSWORD_ENABLED) : "true");

        Map<String, Object> result = new HashMap<>();
        if (adminEnabled && passwordEnabled) {
            String storedHash = settingsService.getSetting(KEY_ADMIN_PASSWORD_HASH);
            String inputHash = sha256(password);
            if (storedUsername.equals(username) && inputHash != null && inputHash.equals(storedHash)) {
                result.put("success", true);
                result.put("token", "admin-token-" + System.currentTimeMillis());
                result.put("username", username);
            } else {
                result.put("success", false);
                result.put("message", "用户名或密码错误");
            }
        } else {
            if (storedUsername.equals(username) && storedPassword.equals(password)) {
                result.put("success", true);
                result.put("token", "admin-token-" + System.currentTimeMillis());
                result.put("username", username);
            } else {
                result.put("success", false);
                result.put("message", "用户名或密码错误");
            }
        }
        return result;
    }

    public boolean changePassword(String oldPassword, String newPassword) {
        boolean adminEnabled = Boolean.parseBoolean(
                getSettingOrDefault(KEY_ADMIN_ENABLED, "false"));
        boolean passwordEnabled = Boolean.parseBoolean(
                getSettingOrDefault(KEY_ADMIN_PASSWORD_ENABLED, "true"));

        if (adminEnabled && passwordEnabled) {
            String storedHash = settingsService.getSetting(KEY_ADMIN_PASSWORD_HASH);
            String oldHash = sha256(oldPassword);
            if (storedHash == null || !storedHash.equals(oldHash)) {
                return false;
            }
            settingsService.updateSetting(KEY_ADMIN_PASSWORD_HASH, sha256(newPassword));
            settingsService.updateSetting("admin_password", newPassword);
            return true;
        }

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

    /**
     * 获取管理员验证设置
     */
    public Map<String, Object> getAdminSettings() {
        Map<String, Object> result = new HashMap<>();
        result.put("isEnabled", Boolean.parseBoolean(getSettingOrDefault(KEY_ADMIN_ENABLED, "false")));
        result.put("verificationMethod", getSettingOrDefault(KEY_ADMIN_VERIFICATION_METHOD, "Password"));
        result.put("isPasswordEnabled", Boolean.parseBoolean(getSettingOrDefault(KEY_ADMIN_PASSWORD_ENABLED, "true")));
        result.put("isUsbEnabled", Boolean.parseBoolean(getSettingOrDefault(KEY_ADMIN_USB_ENABLED, "false")));
        result.put("isFaceEnabled", Boolean.parseBoolean(getSettingOrDefault(KEY_ADMIN_FACE_ENABLED, "false")));
        result.put("usbDeviceId", settingsService.getSetting(KEY_ADMIN_USB_DEVICE_ID));
        result.put("hasPassword", settingsService.getSetting(KEY_ADMIN_PASSWORD_HASH) != null);
        return result;
    }

    /**
     * 更新管理员验证设置
     */
    public void updateAdminSettings(Map<String, Object> settings) {
        if (settings.containsKey("isEnabled")) {
            settingsService.updateSetting(KEY_ADMIN_ENABLED, String.valueOf(settings.get("isEnabled")));
        }
        if (settings.containsKey("verificationMethod")) {
            settingsService.updateSetting(KEY_ADMIN_VERIFICATION_METHOD, String.valueOf(settings.get("verificationMethod")));
        }
        if (settings.containsKey("isPasswordEnabled")) {
            settingsService.updateSetting(KEY_ADMIN_PASSWORD_ENABLED, String.valueOf(settings.get("isPasswordEnabled")));
        }
        if (settings.containsKey("isUsbEnabled")) {
            settingsService.updateSetting(KEY_ADMIN_USB_ENABLED, String.valueOf(settings.get("isUsbEnabled")));
        }
        if (settings.containsKey("isFaceEnabled")) {
            settingsService.updateSetting(KEY_ADMIN_FACE_ENABLED, String.valueOf(settings.get("isFaceEnabled")));
        }
        if (settings.containsKey("usbDeviceId")) {
            String usbDeviceId = (String) settings.get("usbDeviceId");
            if (usbDeviceId != null) {
                settingsService.updateSetting(KEY_ADMIN_USB_DEVICE_ID, usbDeviceId);
            }
        }
    }

    /**
     * 验证管理员身份
     */
    public boolean verifyAdmin(String method, String credential) {
        if (method == null) {
            method = getSettingOrDefault(KEY_ADMIN_VERIFICATION_METHOD, "Password");
        }

        switch (method) {
            case "Password":
                return verifyByPassword(credential);
            case "Usb":
                return verifyByUsb(credential);
            case "Face":
                return verifyByFace();
            default:
                return verifyByPassword(credential);
        }
    }

    /**
     * 设置管理员密码（SHA-256 哈希存储）
     */
    public void setPassword(String password) {
        if (password == null || password.isEmpty()) {
            throw new IllegalArgumentException("密码不能为空");
        }
        String hash = sha256(password);
        settingsService.updateSetting(KEY_ADMIN_PASSWORD_HASH, hash);
        settingsService.updateSetting(KEY_ADMIN_PASSWORD_ENABLED, "true");
    }

    private boolean verifyByPassword(String credential) {
        if (credential == null || credential.isEmpty()) {
            return false;
        }
        String storedHash = settingsService.getSetting(KEY_ADMIN_PASSWORD_HASH);
        if (storedHash == null) {
            // 未设置哈希密码时，使用明文密码验证
            String storedPassword = settingsService.getSetting("admin_password");
            if (storedPassword == null) {
                storedPassword = "admin123";
            }
            return storedPassword.equals(credential);
        }
        String inputHash = sha256(credential);
        return inputHash != null && inputHash.equals(storedHash);
    }

    private boolean verifyByUsb(String credential) {
        if (credential == null || credential.isEmpty()) {
            return false;
        }
        String storedDeviceId = settingsService.getSetting(KEY_ADMIN_USB_DEVICE_ID);
        if (storedDeviceId == null) {
            return false;
        }
        return storedDeviceId.equals(credential);
    }

    private boolean verifyByFace() {
        // 人脸验证占位：实际人脸识别需要原生模块支持
        // 当前始终返回 true，表示功能已预留但未实现
        return true;
    }

    private String sha256(String input) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(input.getBytes(StandardCharsets.UTF_8));
            return HexFormat.of().formatHex(hash);
        } catch (NoSuchAlgorithmException e) {
            return null;
        }
    }

    private String getSettingOrDefault(String key, String defaultValue) {
        String value = settingsService.getSetting(key);
        return value != null ? value : defaultValue;
    }
}
