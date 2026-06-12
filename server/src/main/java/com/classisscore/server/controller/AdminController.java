package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.service.AdminService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/admin")
public class AdminController {

    @Autowired
    private AdminService adminService;

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
}
