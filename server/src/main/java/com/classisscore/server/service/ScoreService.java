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
                .canQuickRevert(request.getCanQuickRevert() != null ? request.getCanQuickRevert() : true)
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
    public ScoreRecord revertScore(Long id) {
        ScoreRecord record = this.getById(id);
        if (record == null) {
            return null;
        }
        if (record.getReverted()) {
            return record;
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

    public List<ScoreRecord> getStudentScoreHistory(Long studentId) {
        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(ScoreRecord::getStudentId, studentId);
        wrapper.orderByDesc(ScoreRecord::getCreatedAt);
        return this.list(wrapper);
    }
}
