package com.classisscore.server.controller;

import com.classisscore.server.dto.ApiResult;
import com.classisscore.server.entity.EvaluationItem;
import com.classisscore.server.service.EvaluationService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/evaluation/items")
public class EvaluationController {

    @Autowired
    private EvaluationService evaluationService;

    @GetMapping
    public ApiResult<List<EvaluationItem>> list() {
        return ApiResult.success(evaluationService.listItems());
    }

    @PostMapping
    public ApiResult<EvaluationItem> create(@RequestBody EvaluationItem item) {
        return ApiResult.success(evaluationService.createItem(item));
    }

    @PutMapping("/{id}")
    public ApiResult<EvaluationItem> update(@PathVariable Long id, @RequestBody EvaluationItem item) {
        return ApiResult.success(evaluationService.updateItem(id, item));
    }

    @DeleteMapping("/{id}")
    public ApiResult<Void> delete(@PathVariable Long id) {
        evaluationService.deleteItem(id);
        return ApiResult.success();
    }
}
