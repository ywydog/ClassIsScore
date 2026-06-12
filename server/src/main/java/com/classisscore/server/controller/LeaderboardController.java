package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.entity.Student;
import com.classisscore.server.service.LeaderboardService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/leaderboard")
public class LeaderboardController {

    @Autowired
    private LeaderboardService leaderboardService;

    @GetMapping
    public ApiResult<List<Map<String, Object>>> leaderboard(
            @RequestParam(required = false) Long groupId) {
        return ApiResult.success(leaderboardService.getLeaderboardWithRank(groupId));
    }

    @GetMapping("/group/{groupId}")
    public ApiResult<List<Map<String, Object>>> groupLeaderboard(@PathVariable Long groupId) {
        return ApiResult.success(leaderboardService.getLeaderboardWithRank(groupId));
    }
}
