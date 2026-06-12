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
@TableName("score_record")
public class ScoreRecord {

    @TableId(type = IdType.AUTO)
    private Long id;

    private Long studentId;

    private Integer scoreChange;

    private String reason;

    private String category;

    private Long operatorId;

    @Builder.Default
    private Boolean canQuickRevert = true;

    @Builder.Default
    private Boolean reverted = false;

    @TableField(fill = FieldFill.INSERT)
    private LocalDateTime createdAt;
}
