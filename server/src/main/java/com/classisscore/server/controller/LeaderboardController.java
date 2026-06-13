package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
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
    public ApiResult<List<Map<String, Object>>> groupLeaderboardByPath(@PathVariable Long groupId) {
        return ApiResult.success(leaderboardService.getLeaderboardWithRank(groupId));
    }

    @GetMapping("/personal")
    public ApiResult<List<Map<String, Object>>> personalLeaderboard(
            @RequestParam(required = false) String startTime,
            @RequestParam(required = false) String endTime) {
        return ApiResult.success(leaderboardService.getPersonalLeaderboard(startTime, endTime));
    }

    @GetMapping("/group")
    public ApiResult<List<Map<String, Object>>> groupLeaderboard(
            @RequestParam(required = false) String startTime,
            @RequestParam(required = false) String endTime) {
        return ApiResult.success(leaderboardService.getGroupLeaderboard(startTime, endTime));
    }
}
