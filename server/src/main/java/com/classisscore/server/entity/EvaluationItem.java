package com.classisscore.server.entity;

import com.baomidou.mybatisplus.annotation.*;
import lombok.Data;
import lombok.Builder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.time.LocalDateTime;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@TableName("evaluation_item")
public class EvaluationItem {

    @TableId(type = IdType.AUTO)
    private Long id;

    private String name;

    private Integer scoreChange;

    private String category;

    @Builder.Default
    private Boolean isQuickAccess = false;

    @TableField(fill = FieldFill.INSERT)
    private LocalDateTime createdAt;
}
