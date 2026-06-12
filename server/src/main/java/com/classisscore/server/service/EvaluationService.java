package com.classisscore.server.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.classisscore.server.entity.EvaluationItem;
import com.classisscore.server.mapper.EvaluationItemMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;

@Service
public class EvaluationService extends ServiceImpl<EvaluationItemMapper, EvaluationItem> {

    public java.util.List<EvaluationItem> listItems() {
        LambdaQueryWrapper<EvaluationItem> wrapper = new LambdaQueryWrapper<>();
        wrapper.orderByAsc(EvaluationItem::getCategory);
        wrapper.orderByDesc(EvaluationItem::getIsQuickAccess);
        return this.list(wrapper);
    }

    @Transactional
    public EvaluationItem createItem(EvaluationItem item) {
        item.setCreatedAt(LocalDateTime.now());
        if (item.getIsQuickAccess() == null) {
            item.setIsQuickAccess(false);
        }
        this.save(item);
        return item;
    }

    @Transactional
    public EvaluationItem updateItem(Long id, EvaluationItem item) {
        item.setId(id);
        this.updateById(item);
        return this.getById(id);
    }

    @Transactional
    public boolean deleteItem(Long id) {
        return this.removeById(id);
    }
}
