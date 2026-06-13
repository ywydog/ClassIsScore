package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.classisscore.server.entity.ScoreRecord;
import com.classisscore.server.entity.Student;
import com.classisscore.server.entity.StudentGroup;
import com.classisscore.server.mapper.ScoreRecordMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Service
public class LeaderboardService {

    @Autowired
    private StudentService studentService;

    @Autowired
    private GroupService groupService;

    @Autowired
    private ScoreRecordMapper scoreRecordMapper;

    public List<Student> getLeaderboard(Long groupId) {
        LambdaQueryWrapper<Student> wrapper = new LambdaQueryWrapper<>();
        if (groupId != null) {
            wrapper.eq(Student::getGroupId, groupId);
        }
        wrapper.orderByDesc(Student::getTotalScore);
        return studentService.list(wrapper);
    }

    public List<Student> getGroupLeaderboard(Long groupId) {
        return getLeaderboard(groupId);
    }

    public List<Map<String, Object>> getLeaderboardWithRank(Long groupId) {
        List<Student> students = getLeaderboard(groupId);
        return students.stream().map(student -> {
            Map<String, Object> item = new HashMap<>();
            item.put("studentId", student.getId());
            item.put("name", student.getName());
            item.put("studentNumber", student.getStudentNumber());
            item.put("groupId", student.getGroupId());
            item.put("totalScore", student.getTotalScore());
            item.put("avatar", student.getAvatar());
            return item;
        }).collect(Collectors.toList());
    }

    /**
     * 获取个人排行榜（支持时间范围过滤）
     */
    public List<Map<String, Object>> getPersonalLeaderboard(String startTime, String endTime) {
        if (startTime == null || startTime.isEmpty() || endTime == null || endTime.isEmpty()) {
            // 无时间范围 - 使用学生表的 totalScore
            List<Student> students = getLeaderboard(null);
            return students.stream().map(student -> {
                Map<String, Object> item = new HashMap<>();
                item.put("name", student.getName());
                item.put("score", student.getTotalScore());
                return item;
            }).collect(Collectors.toList());
        }

        // 有时间范围 - 从 score_record 表计算
        LocalDateTime start = LocalDateTime.parse(startTime);
        LocalDateTime end = LocalDateTime.parse(endTime);

        LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
        wrapper.ge(ScoreRecord::getCreatedAt, start);
        wrapper.le(ScoreRecord::getCreatedAt, end);
        wrapper.eq(ScoreRecord::getReverted, false);
        List<ScoreRecord> records = scoreRecordMapper.selectList(wrapper);

        // 按 studentId 分组求和
        Map<Long, Integer> scoreMap = new HashMap<>();
        for (ScoreRecord record : records) {
            scoreMap.merge(record.getStudentId(), record.getScoreChange(), Integer::sum);
        }

        // 获取学生信息并构建结果
        List<Map<String, Object>> result = scoreMap.entrySet().stream()
                .map(entry -> {
                    Student student = studentService.getById(entry.getKey());
                    if (student != null) {
                        Map<String, Object> item = new HashMap<>();
                        item.put("name", student.getName());
                        item.put("score", entry.getValue());
                        return item;
                    }
                    return null;
                })
                .filter(item -> item != null)
                .sorted((a, b) -> ((Integer) b.get("score")).compareTo((Integer) a.get("score")))
                .collect(Collectors.toList());

        return result;
    }

    /**
     * 获取小组排行榜（支持时间范围过滤）
     */
    public List<Map<String, Object>> getGroupLeaderboard(String startTime, String endTime) {
        List<Student> students = studentService.list();

        // 计算每个学生的积分
        Map<Long, Integer> studentScores = new HashMap<>();

        if (startTime == null || startTime.isEmpty() || endTime == null || endTime.isEmpty()) {
            // 无时间范围 - 使用学生表的 totalScore
            for (Student student : students) {
                studentScores.put(student.getId(), student.getTotalScore());
            }
        } else {
            // 有时间范围 - 从 score_record 表计算
            LocalDateTime start = LocalDateTime.parse(startTime);
            LocalDateTime end = LocalDateTime.parse(endTime);

            LambdaQueryWrapper<ScoreRecord> wrapper = new LambdaQueryWrapper<>();
            wrapper.ge(ScoreRecord::getCreatedAt, start);
            wrapper.le(ScoreRecord::getCreatedAt, end);
            wrapper.eq(ScoreRecord::getReverted, false);
            List<ScoreRecord> records = scoreRecordMapper.selectList(wrapper);

            for (ScoreRecord record : records) {
                studentScores.merge(record.getStudentId(), record.getScoreChange(), Integer::sum);
            }
        }

        // 按 groupId 汇总积分
        Map<Long, Integer> groupScores = new HashMap<>();
        for (Student student : students) {
            if (student.getGroupId() != null) {
                Integer score = studentScores.getOrDefault(student.getId(), 0);
                groupScores.merge(student.getGroupId(), score, Integer::sum);
            }
        }

        // 获取小组信息并构建结果
        List<Map<String, Object>> result = groupScores.entrySet().stream()
                .map(entry -> {
                    StudentGroup group = groupService.getById(entry.getKey());
                    if (group != null) {
                        Map<String, Object> item = new HashMap<>();
                        item.put("name", group.getName());
                        item.put("score", entry.getValue());
                        return item;
                    }
                    return null;
                })
                .filter(item -> item != null)
                .sorted((a, b) -> ((Integer) b.get("score")).compareTo((Integer) a.get("score")))
                .collect(Collectors.toList());

        return result;
    }
}
