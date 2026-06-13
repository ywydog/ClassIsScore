package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.dto.ScoreRequest;
import com.classisscore.server.dto.RevertRequest;
import com.classisscore.server.entity.ScoreRecord;
import com.classisscore.server.service.ScoreService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/scores")
public class ScoreController {

    @Autowired
    private ScoreService scoreService;

    @GetMapping
    public ApiResult<List<ScoreRecord>> list(
            @RequestParam(required = false) Long studentId,
            @RequestParam(required = false) String category,
            @RequestParam(required = false) String startTime,
            @RequestParam(required = false) String endTime) {
        return ApiResult.success(scoreService.listScoreRecords(studentId, category, startTime, endTime));
    }

    @PostMapping
    public ApiResult<ScoreRecord> addScore(@RequestBody ScoreRequest request) {
        return ApiResult.success(scoreService.addScore(request));
    }

    @PostMapping("/batch")
    public ApiResult<List<ScoreRecord>> batchAddScore(@RequestBody ScoreRequest request) {
        return ApiResult.success(scoreService.batchAddScore(request));
    }

    @PostMapping("/{id}/revert")
    public ApiResult<ScoreRecord> revert(@PathVariable Long id, @RequestBody(required = false) RevertRequest request) {
        String adminPassword = request != null ? request.getAdminPassword() : null;
        try {
            ScoreRecord record = scoreService.revertScore(id, adminPassword);
            if (record == null) {
                return ApiResult.error("积分记录不存在");
            }
            return ApiResult.success(record);
        } catch (RuntimeException e) {
            return ApiResult.error(e.getMessage());
        }
    }

    @GetMapping("/{id}/can-revert")
    public ApiResult<Map<String, Object>> canRevert(@PathVariable Long id) {
        boolean canQuickRevert = scoreService.canQuickRevert(id);
        Map<String, Object> result = Map.of(
            "canQuickRevert", canQuickRevert,
            "needsAdminVerification", !canQuickRevert
        );
        return ApiResult.success(result);
    }

    @GetMapping("/student/{studentId}")
    public ApiResult<List<ScoreRecord>> studentHistory(@PathVariable Long studentId) {
        return ApiResult.success(scoreService.getStudentScoreHistory(studentId));
    }
}
