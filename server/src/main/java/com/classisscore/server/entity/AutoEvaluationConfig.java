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
@TableName("auto_evaluation_config")
public class AutoEvaluationConfig {

    @TableId(type = IdType.AUTO)
    private Long id;

    private String name;

    private String triggerType;

    private String triggerTime;

    private Integer dayOfWeek;

    private Integer dayOfMonth;

    private Long evaluationItemId;

    private Double scoreChange;

    private String reason;

    private String targetType;

    private Long targetGroupId;

    private Long targetStudentId;

    @Builder.Default
    private Boolean isEnabled = false;

    private LocalDateTime lastExecutedAt;

    @TableField(fill = FieldFill.INSERT)
    private LocalDateTime createdAt;
}
