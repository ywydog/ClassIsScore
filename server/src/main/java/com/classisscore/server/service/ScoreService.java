package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.dto.ScoreRequest;
import com.classisscore.server.entity.ScoreRecord;
import com.classisscore.server.entity.Student;
import com.classisscore.server.mapper.ScoreRecordMapper;
import com.classisscore.server.websocket.ScoreWebSocketHandler;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Duration;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Service
public class ScoreService extends ServiceImpl<ScoreRecordMapper, ScoreRecord> {

    @Autowired
    private StudentService studentService;

    @Autowired
    private ScoreWebSocketHandler webSocketHandler;

    @Autowired
    private AdminService adminService;

    @Autowired
    private SettingsService settingsService;

    private static final long QUICK_REVERT_WINDOW_MINUTES = 3;

    public List<ScoreRecord> listScoreRecords(Long studentId, String category, String startTime, String endTime) {
        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        if (studentId != null) {
            wrapper.eq(ScoreRecord::getStudentId, studentId);
        }
        if (category != null && !category.isEmpty()) {
            wrapper.eq(ScoreRecord::getCategory, category);
        }
        wrapper.orderByDesc(ScoreRecord::getCreatedAt);
        return this.list(wrapper);
    }

    @Transactional
    public ScoreRecord addScore(ScoreRequest request) {
        ScoreRecord record = ScoreRecord.builder()
                .studentId(request.getStudentId())
                .scoreChange(request.getScoreChange())
                .reason(request.getReason())
                .category(request.getCategory())
                .operatorId(request.getOperatorId())
                .canQuickRevert(true)
                .reverted(false)
                .createdAt(LocalDateTime.now())
                .build();
        this.save(record);

        studentService.updateTotalScore(request.getStudentId(), request.getScoreChange());

        Student student = studentService.getById(request.getStudentId());
        if (student != null) {
            Map<String, Object> message = new HashMap<>();
            message.put("type", "SCORE_UPDATE");
            message.put("studentId", student.getId());
            message.put("scoreChange", request.getScoreChange());
            message.put("totalScore", student.getTotalScore());
            message.put("reason", request.getReason());
            webSocketHandler.broadcast(message);
        }

        return record;
    }

    @Transactional
    public List<ScoreRecord> batchAddScore(ScoreRequest request) {
        List<ScoreRecord> records = new ArrayList<>();
        if (request.getStudentIds() != null) {
            for (Long studentId : request.getStudentIds()) {
                ScoreRequest singleRequest = ScoreRequest.builder()
                        .studentId(studentId)
                        .scoreChange(request.getScoreChange())
                        .reason(request.getReason())
                        .category(request.getCategory())
                        .operatorId(request.getOperatorId())
                        .canQuickRevert(request.getCanQuickRevert())
                        .build();
                records.add(addScore(singleRequest));
            }
        }
        return records;
    }

    @Transactional
    public ScoreRecord revertScore(Long id, String adminPassword) {
        ScoreRecord record = this.getById(id);
        if (record == null) {
            return null;
        }
        if (record.getReverted()) {
            return record;
        }

        // 检查是否在3分钟快速撤销窗口内
        boolean quickRevert = canQuickRevert(id);

        if (!quickRevert) {
            // 超过3分钟，需要管理员密码验证
            if (adminPassword == null || adminPassword.isEmpty()) {
                throw new RuntimeException("超过3分钟快速撤销窗口，需要管理员密码验证");
            }
            // 验证管理员密码
            String storedPassword = settingsService.getSetting("admin_password");
            if (storedPassword == null) {
                storedPassword = "admin123";
            }
            if (!storedPassword.equals(adminPassword)) {
                throw new RuntimeException("管理员密码错误");
            }
        }

        record.setReverted(true);
        this.updateById(record);

        studentService.updateTotalScore(record.getStudentId(), -record.getScoreChange());

        Student student = studentService.getById(record.getStudentId());
        if (student != null) {
            Map<String, Object> message = new HashMap<>();
            message.put("type", "SCORE_UPDATE");
            message.put("studentId", student.getId());
            message.put("scoreChange", -record.getScoreChange());
            message.put("totalScore", student.getTotalScore());
            message.put("reason", "撤销: " + record.getReason());
            webSocketHandler.broadcast(message);
        }

        return record;
    }

    public boolean canQuickRevert(Long recordId) {
        ScoreRecord record = this.getById(recordId);
        if (record == null || record.getReverted()) {
            return false;
        }
        if (record.getCreatedAt() == null) {
            return false;
        }
        Duration duration = Duration.between(record.getCreatedAt(), LocalDateTime.now());
        return duration.toMinutes() < QUICK_REVERT_WINDOW_MINUTES;
    }

    public List<ScoreRecord> getStudentScoreHistory(Long studentId) {
        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(ScoreRecord::getStudentId, studentId);
        wrapper.orderByDesc(ScoreRecord::getCreatedAt);
        return this.list(wrapper);
    }

    public List<ScoreRecord> getRecentScores(int limit) {
        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(ScoreRecord::getReverted, false);
        wrapper.orderByDesc(ScoreRecord::getCreatedAt);
        wrapper.last("LIMIT " + Math.min(limit, 200));
        return this.list(wrapper);
    }

    public List<Map<String, Object>> getScoreStats(String semesterStartDate) {
        LocalDateTime now = LocalDateTime.now();
        LocalDateTime todayStart = now.toLocalDate().atStartOfDay();
        LocalDateTime weekStart = now.minusDays(now.getDayOfWeek().getValue() - 1).toLocalDate().atStartOfDay();
        LocalDateTime monthStart = now.withDayOfMonth(1).toLocalDate().atStartOfDay();
        LocalDateTime semesterStart = null;
        if (semesterStartDate != null && !semesterStartDate.isEmpty()) {
            try {
                semesterStart = java.time.LocalDate.parse(semesterStartDate).atStartOfDay();
            } catch (Exception ignored) {}
        }

        List<Student> students = studentService.list();
        List<Map<String, Object>> result = new ArrayList<>();

        for (Student student : students) {
            Map<String, Object> stats = new HashMap<>();
            stats.put("studentId", student.getId());
            stats.put("studentName", student.getName());
            stats.put("totalScore", student.getTotalScore());

            // 日统计
            int[] dayStats = calcPeriodStats(student.getId(), todayStart, now);
            stats.put("dayPlus", dayStats[0]);
            stats.put("dayMinus", dayStats[1]);
            stats.put("dayNet", dayStats[0] + dayStats[1]);

            // 周统计
            int[] weekStats = calcPeriodStats(student.getId(), weekStart, now);
            stats.put("weekPlus", weekStats[0]);
            stats.put("weekMinus", weekStats[1]);
            stats.put("weekNet", weekStats[0] + weekStats[1]);

            // 月统计
            int[] monthStats = calcPeriodStats(student.getId(), monthStart, now);
            stats.put("monthPlus", monthStats[0]);
            stats.put("monthMinus", monthStats[1]);
            stats.put("monthNet", monthStats[0] + monthStats[1]);

            // 学期统计
            if (semesterStart != null) {
                int[] semStats = calcPeriodStats(student.getId(), semesterStart, now);
                stats.put("semesterPlus", semStats[0]);
                stats.put("semesterMinus", semStats[1]);
                stats.put("semesterNet", semStats[0] + semStats[1]);
            }

            result.add(stats);
        }

        return result;
    }

    private int[] calcPeriodStats(Long studentId, LocalDateTime start, LocalDateTime end) {
        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(ScoreRecord::getStudentId, studentId);
        wrapper.eq(ScoreRecord::getReverted, false);
        wrapper.ge(ScoreRecord::getCreatedAt, start);
        wrapper.le(ScoreRecord::getCreatedAt, end);
        List<ScoreRecord> records = this.list(wrapper);

        int plus = 0, minus = 0;
        for (ScoreRecord r : records) {
            if (r.getScoreChange() > 0) {
                plus += r.getScoreChange();
            } else if (r.getScoreChange() < 0) {
                minus += r.getScoreChange();
            }
        }
        return new int[]{plus, minus};
    }
}
