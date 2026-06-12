package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.entity.StudentGroup;
import com.classisscore.server.service.GroupService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/groups")
public class GroupController {

    @Autowired
    private GroupService groupService;

    @GetMapping
    public ApiResult<List<StudentGroup>> list() {
        return ApiResult.success(groupService.listGroups());
    }

    @PostMapping
    public ApiResult<StudentGroup> create(@RequestBody StudentGroup group) {
        return ApiResult.success(groupService.createGroup(group));
    }

    @PutMapping("/{id}")
    public ApiResult<StudentGroup> update(@PathVariable Long id, @RequestBody StudentGroup group) {
        return ApiResult.success(groupService.updateGroup(id, group));
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        groupService.deleteGroup(id);
        return ApiResult.success();
    }
}
