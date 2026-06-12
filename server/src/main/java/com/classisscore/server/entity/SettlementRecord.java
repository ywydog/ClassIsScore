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
@TableName("settlement_record")
public class SettlementRecord {

    @TableId(type = IdType.AUTO)
    private Long id;

    private String name;

    private String period;

    private String snapshotData;

    @Builder.Default
    private Integer status = 0;

    @TableField(fill = FieldFill.INSERT)
    private LocalDateTime createdAt;
}
