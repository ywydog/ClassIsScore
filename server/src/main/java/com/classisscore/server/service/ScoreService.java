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
}
