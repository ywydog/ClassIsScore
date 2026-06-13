package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.service.AdminService;
import com.classisscore.server.service.StudentService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/admin")
public class AdminController {

    @Autowired
    private AdminService adminService;

    @Autowired
    private StudentService studentService;

    @PostMapping("/login")
    public ApiResult<Map<String, Object>> login(@RequestBody Map<String, String> body) {
        String username = body.get("username");
        String password = body.get("password");
        if (username == null || password == null) {
            return ApiResult.error("用户名和密码不能为空");
        }
        Map<String, Object> result = adminService.login(username, password);
        if (Boolean.TRUE.equals(result.get("success"))) {
            return ApiResult.success(result);
        }
        return ApiResult.error(result.get("message").toString());
    }

    @PostMapping("/change-password")
    public ApiResult<Void> changePassword(@RequestBody Map<String, String> body) {
        String oldPassword = body.get("oldPassword");
        String newPassword = body.get("newPassword");
        if (oldPassword == null || newPassword == null) {
            return ApiResult.error("旧密码和新密码不能为空");
        }
        boolean success = adminService.changePassword(oldPassword, newPassword);
        if (!success) {
            return ApiResult.error("旧密码错误");
        }
        return ApiResult.success();
    }

    @GetMapping("/info")
    public ApiResult<Map<String, Object>> info() {
        return ApiResult.success(adminService.getAdminInfo());
    }

    @PostMapping("/reset")
    public ApiResult<Void> reset() {
        studentService.resetAllScores();
        return ApiResult.success();
    }

    /**
     * 获取管理员验证设置
     */
    @GetMapping("/settings")
    public ApiResult<Map<String, Object>> getAdminSettings() {
        return ApiResult.success(adminService.getAdminSettings());
    }

    /**
     * 更新管理员验证设置
     */
    @PutMapping("/settings")
    public ApiResult<Void> updateAdminSettings(@RequestBody Map<String, Object> settings) {
        adminService.updateAdminSettings(settings);
        return ApiResult.success();
    }

    /**
     * 验证管理员身份
     */
    @PostMapping("/verify")
    public ApiResult<Boolean> verifyAdmin(@RequestBody Map<String, String> body) {
        String method = body.get("method");
        String credential = body.get("credential");
        if (credential == null || credential.isEmpty()) {
            return ApiResult.error("验证凭据不能为空");
        }
        boolean result = adminService.verifyAdmin(method, credential);
        if (result) {
            return ApiResult.success(true);
        }
        return ApiResult.error("验证失败");
    }

    /**
     * 设置管理员密码
     */
    @PostMapping("/set-password")
    public ApiResult<Void> setPassword(@RequestBody Map<String, String> body) {
        String password = body.get("password");
        if (password == null || password.isEmpty()) {
            return ApiResult.error("密码不能为空");
        }
        try {
            adminService.setPassword(password);
            return ApiResult.success();
        } catch (IllegalArgumentException e) {
            return ApiResult.error(e.getMessage());
        }
    }
}
