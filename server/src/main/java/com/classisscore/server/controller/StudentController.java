package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.dto.PageResult;
import com.classisscore.server.entity.Student;
import com.classisscore.server.service.StudentService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/students")
public class StudentController {

    @Autowired
    private StudentService studentService;

    @GetMapping
    public ApiResult<PageResult<Student>> list(
            @RequestParam(defaultValue = "1") Long current,
            @RequestParam(defaultValue = "10") Long size,
            @RequestParam(required = false) Long groupId) {
        return ApiResult.success(studentService.listStudents(current, size, groupId));
    }

    @GetMapping("/{id}")
    public ApiResult<Student> get(@PathVariable Long id) {
        Student student = studentService.getStudent(id);
        if (student == null) {
            return ApiResult.error("学生不存在");
        }
        return ApiResult.success(student);
    }

    @PostMapping
    public ApiResult<Student> create(@RequestBody Student student) {
        return ApiResult.success(studentService.createStudent(student));
    }

    @PutMapping("/{id}")
    public ApiResult<Student> update(@PathVariable Long id, @RequestBody Student student) {
        return ApiResult.success(studentService.updateStudent(id, student));
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        studentService.deleteStudent(id);
        return ApiResult.success();
    }

    @PostMapping("/batch")
    public ApiResult<List<Student>> batchCreate(@RequestBody List<Student> students) {
        return ApiResult.success(studentService.batchCreate(students));
    }
}
