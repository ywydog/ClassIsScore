package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.dto.SettlementRequest;
import com.classisscore.server.entity.SettlementRecord;
import com.classisscore.server.entity.Student;
import com.classisscore.server.mapper.SettlementRecordMapper;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Service
public class SettlementService extends ServiceImpl<SettlementRecordMapper, SettlementRecord> {

    @Autowired
    private StudentService studentService;

    @Autowired
    private ScoreService scoreService;

    private final ObjectMapper objectMapper = new ObjectMapper();

    public java.util.List<SettlementRecord> listSettlements() {
        LambdaQueryWrapper<SettlementRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.orderByDesc(SettlementRecord::getCreatedAt);
        return this.list(wrapper);
    }

    @Transactional
    public SettlementRecord createSettlement(SettlementRequest request) {
        List<Student> students = studentService.list();
        List<Map<String, Object>> snapshot = students.stream().map(student -> {
            Map<String, Object> item = new HashMap<>();
            item.put("studentId", student.getId());
            item.put("name", student.getName());
            item.put("studentNumber", student.getStudentNumber());
            item.put("totalScore", student.getTotalScore());
            item.put("groupId", student.getGroupId());
            return item;
        }).collect(Collectors.toList());

        String snapshotJson;
        try {
            snapshotJson = objectMapper.writeValueAsString(snapshot);
        } catch (Exception e) {
            snapshotJson = "[]";
        }

        SettlementRecord record = SettlementRecord.builder()
                .name(request.getName())
                .period(request.getPeriod())
                .snapshotData(snapshotJson)
                .status(0)
                .createdAt(LocalDateTime.now())
                .build();
        this.save(record);
        return record;
    }

    @Transactional
    public SettlementRecord completeSettlement(Long id) {
        SettlementRecord record = this.getById(id);
        if (record == null) {
            return null;
        }
        record.setStatus(1);
        this.updateById(record);
        return record;
    }

    @Transactional
    public boolean revertSettlement(Long id) {
        SettlementRecord record = this.getById(id);
        if (record == null) {
            return false;
        }

        try {
            List<Map<String, Object>> snapshot = objectMapper.readValue(
                    record.getSnapshotData(),
                    objectMapper.getTypeFactory().constructCollectionType(List.class, Map.class)
            );

            for (Map<String, Object> item : snapshot) {
                Long studentId = Long.valueOf(item.get("studentId").toString());
                Integer totalScore = Integer.valueOf(item.get("totalScore").toString());
                Student student = studentService.getById(studentId);
                if (student != null) {
                    student.setTotalScore(totalScore);
                    student.setUpdatedAt(LocalDateTime.now());
                    studentService.updateById(student);
                }
            }
        } catch (Exception e) {
            return false;
        }

        return this.removeById(id);
    }
}
