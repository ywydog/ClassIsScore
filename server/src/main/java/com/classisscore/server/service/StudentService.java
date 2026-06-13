package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.dto.PageResult;
import com.classisscore.server.entity.Student;
import com.classisscore.server.mapper.StudentMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;

@Service
public class StudentService extends ServiceImpl<StudentMapper, Student> {

    public PageResult<Student> listStudents(Long current, Long size, Long groupId) {
        LambdaQueryWrapper<Student> wrapper = new LambdaQueryWrapper<>();
        if (groupId != null) {
            wrapper.eq(Student::getGroupId, groupId);
        }
        wrapper.orderByDesc(Student::getTotalScore);
        IPage<Student> page = this.page(new Page<>(current, size), wrapper);
        return PageResult.<Student>builder()
                .records(page.getRecords())
                .total(page.getTotal())
                .current(page.getCurrent())
                .size(page.getSize())
                .pages(page.getPages())
                .build();
    }

    public Student getStudent(Long id) {
        return this.getById(id);
    }

    @Transactional
    public Student createStudent(Student student) {
        student.setCreatedAt(LocalDateTime.now());
        student.setUpdatedAt(LocalDateTime.now());
        if (student.getTotalScore() == null) {
            student.setTotalScore(0);
        }
        this.save(student);
        return student;
    }

    @Transactional
    public Student updateStudent(Long id, Student student) {
        student.setId(id);
        student.setUpdatedAt(LocalDateTime.now());
        this.updateById(student);
        return this.getById(id);
    }

    @Transactional
    public boolean deleteStudent(Long id) {
        return this.removeById(id);
    }

    @Transactional
    public List<Student> batchCreate(List<Student> students) {
        for (Student student : students) {
            student.setCreatedAt(LocalDateTime.now());
            student.setUpdatedAt(LocalDateTime.now());
            if (student.getTotalScore() == null) {
                student.setTotalScore(0);
            }
        }
        this.saveBatch(students);
        return students;
    }

    public List<Student> listByGroupId(Long groupId) {
        LambdaQueryWrapper<Student> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(Student::getGroupId, groupId);
        wrapper.orderByDesc(Student::getTotalScore);
        return this.list(wrapper);
    }

    @Transactional
    public void updateTotalScore(Long studentId, Integer scoreChange) {
        Student student = this.getById(studentId);
        if (student != null) {
            student.setTotalScore(student.getTotalScore() + scoreChange);
            student.setUpdatedAt(LocalDateTime.now());
            this.updateById(student);
        }
    }

    @Transactional
    public void resetAllScores() {
        List<Student> students = this.list();
        for (Student student : students) {
            student.setTotalScore(0);
            student.setUpdatedAt(LocalDateTime.now());
        }
        this.updateBatchById(students);
    }
}
