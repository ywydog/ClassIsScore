package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.entity.StudentGroup;
import com.classisscore.server.mapper.StudentGroupMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;

@Service
public class GroupService extends ServiceImpl<StudentGroupMapper, StudentGroup> {

    public java.util.List<StudentGroup> listGroups() {
        LambdaQueryWrapper<StudentGroup> wrapper = new LambdaQueryWrapper<>();
        wrapper.orderByAsc(StudentGroup::getCreatedAt);
        return this.list(wrapper);
    }

    @Transactional
    public StudentGroup createGroup(StudentGroup group) {
        group.setCreatedAt(LocalDateTime.now());
        this.save(group);
        return group;
    }

    @Transactional
    public StudentGroup updateGroup(Long id, StudentGroup group) {
        group.setId(id);
        this.updateById(group);
        return this.getById(id);
    }

    @Transactional
    public boolean deleteGroup(Long id) {
        return this.removeById(id);
    }
}
