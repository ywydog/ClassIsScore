package com.classisscore.server.mapper;

import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.classisscore.server.entity.Student;
import org.apache.ibatis.annotations.Mapper;

@Mapper
public interface StudentMapper extends BaseMapper<Student> {
}
