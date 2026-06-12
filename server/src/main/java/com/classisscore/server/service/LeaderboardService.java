package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.classisscore.server.entity.Student;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Service
public class LeaderboardService {

    @Autowired
    private StudentService studentService;

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
}
