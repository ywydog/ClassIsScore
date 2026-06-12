package com.classisscore.server.dto;

import lombok.Data;
import lombok.Builder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ScoreRequest {

    private Long studentId;

    private Integer scoreChange;

    private String reason;

    private String category;

    private Long operatorId;

    private Boolean canQuickRevert;

    private List<Long> studentIds;

    private Boolean batch;
}
