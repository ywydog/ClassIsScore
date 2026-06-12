package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.dto.ScoreRequest;
import com.classisscore.server.entity.ScoreRecord;
import com.classisscore.server.service.ScoreService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

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
    public ApiResult<ScoreRecord> revert(@PathVariable Long id) {
        ScoreRecord record = scoreService.revertScore(id);
        if (record == null) {
            return ApiResult.error("积分记录不存在");
        }
        return ApiResult.success(record);
    }

    @GetMapping("/student/{studentId}")
    public ApiResult<List<ScoreRecord>> studentHistory(@PathVariable Long studentId) {
        return ApiResult.success(scoreService.getStudentScoreHistory(studentId));
    }
}
