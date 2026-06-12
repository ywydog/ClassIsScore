package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.dto.SettlementRequest;
import com.classisscore.server.entity.SettlementRecord;
import com.classisscore.server.service.SettlementService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/settlements")
public class SettlementController {

    @Autowired
    private SettlementService settlementService;

    @GetMapping
    public ApiResult<List<SettlementRecord>> list() {
        return ApiResult.success(settlementService.listSettlements());
    }

    @PostMapping
    public ApiResult<SettlementRecord> create(@RequestBody SettlementRequest request) {
        return ApiResult.success(settlementService.createSettlement(request));
    }

    @PostMapping("/{id}/complete")
    public ApiResult<SettlementRecord> complete(@PathVariable Long id) {
        SettlementRecord record = settlementService.completeSettlement(id);
        if (record == null) {
            return ApiResult.error("结算记录不存在");
        }
        return ApiResult.success(record);
    }

    @PostMapping("/{id}/revert")
    public ApiResult<Void> revert(@PathVariable Long id) {
        boolean success = settlementService.revertSettlement(id);
        if (!success) {
            return ApiResult.error("回滚结算失败");
        }
        return ApiResult.success();
    }
}
