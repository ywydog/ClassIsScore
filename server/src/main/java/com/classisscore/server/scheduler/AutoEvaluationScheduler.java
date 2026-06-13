package com.classisscore.server.scheduler;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.classisscore.server.dto.ScoreRequest;
import com.classisscore.server.entity.AutoEvaluationConfig;
import com.classisscore.server.entity.EvaluationItem;
import com.classisscore.server.entity.Student;
import com.classisscore.server.service.AutoEvaluationConfigService;
import com.classisscore.server.service.EvaluationService;
import com.classisscore.server.service.ScoreService;
import com.classisscore.server.service.StudentService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.time.DayOfWeek;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;
import java.util.ArrayList;
import java.util.List;

@Component
public class AutoEvaluationScheduler {

    private static final Logger log = LoggerFactory.getLogger(AutoEvaluationScheduler.class);

    private static final ZoneId ZONE = ZoneId.of("Asia/Shanghai");
    private static final long TOLERANCE_MINUTES = 2;
    private static final DateTimeFormatter TIME_FORMATTER = DateTimeFormatter.ofPattern("HH:mm");

    @Autowired
    private AutoEvaluationConfigService configService;

    @Autowired
    private StudentService studentService;

    @Autowired
    private EvaluationService evaluationService;

    @Autowired
    private ScoreService scoreService;

    @Scheduled(fixedRate = 60000)
    public void checkAndExecute() {
        LocalDateTime now = LocalDateTime.now(ZONE);
        log.debug("AutoEvaluationScheduler check at {}", now);

        LambdaQueryWrapper<AutoEvaluationConfig> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(AutoEvaluationConfig::getIsEnabled, true);
        wrapper.ne(AutoEvaluationConfig::getTriggerType, "BeforeSettlement");
        List<AutoEvaluationConfig> configs = configService.list(wrapper);

        for (AutoEvaluationConfig config : configs) {
            try {
                if (shouldFire(config, now)) {
                    executeConfig(config);
                }
            } catch (Exception e) {
                log.error("执行自动评价配置 [id={}, name={}] 时出错: {}", config.getId(), config.getName(), e.getMessage(), e);
            }
        }
    }

    public void executeBeforeSettlementConfigs() {
        LambdaQueryWrapper<AutoEvaluationConfig> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(AutoEvaluationConfig::getIsEnabled, true);
        wrapper.eq(AutoEvaluationConfig::getTriggerType, "BeforeSettlement");
        List<AutoEvaluationConfig> configs = configService.list(wrapper);

        for (AutoEvaluationConfig config : configs) {
            try {
                executeConfig(config);
            } catch (Exception e) {
                log.error("执行结算前自动评价配置 [id={}, name={}] 时出错: {}", config.getId(), config.getName(), e.getMessage(), e);
            }
        }
    }

    private boolean shouldFire(AutoEvaluationConfig config, LocalDateTime now) {
        String triggerType = config.getTriggerType();
        String triggerTimeStr = config.getTriggerTime();

        if (triggerTimeStr == null || triggerTimeStr.isEmpty()) {
            return false;
        }

        LocalTime triggerTime;
        try {
            triggerTime = LocalTime.parse(triggerTimeStr, TIME_FORMATTER);
        } catch (Exception e) {
            log.warn("配置 [id={}] 的触发时间格式无效: {}", config.getId(), triggerTimeStr);
            return false;
        }

        LocalDateTime triggerDateTime = now.toLocalDate().atTime(triggerTime);

        // Check if already fired within this trigger period
        if (config.getLastExecutedAt() != null) {
            if (!hasEnteredNewPeriod(config, now)) {
                return false;
            }
        }

        // Check if current time is within tolerance window of the trigger time
        long minutesDiff = ChronoUnit.MINUTES.between(triggerDateTime, now);
        return minutesDiff >= 0 && minutesDiff < TOLERANCE_MINUTES;
    }

    private boolean hasEnteredNewPeriod(AutoEvaluationConfig config, LocalDateTime now) {
        LocalDateTime lastRun = config.getLastExecutedAt();
        String triggerType = config.getTriggerType();

        switch (triggerType) {
            case "Daily":
                return !lastRun.toLocalDate().equals(now.toLocalDate());
            case "Weekly":
                if (!lastRun.toLocalDate().equals(now.toLocalDate())) {
                    return true;
                }
                DayOfWeek configDay = DayOfWeek.of(config.getDayOfWeek());
                return now.getDayOfWeek() == configDay && lastRun.isBefore(now.toLocalDate().atStartOfDay());
            case "Monthly":
                if (!lastRun.toLocalDate().equals(now.toLocalDate())) {
                    return true;
                }
                return now.getDayOfMonth() == config.getDayOfMonth() && lastRun.isBefore(now.toLocalDate().atStartOfDay());
            default:
                return !lastRun.toLocalDate().equals(now.toLocalDate());
        }
    }

    private void executeConfig(AutoEvaluationConfig config) {
        List<Student> targetStudents = resolveTargetStudents(config);
        if (targetStudents.isEmpty()) {
            log.info("自动评价配置 [id={}, name={}] 没有目标学生，跳过执行", config.getId(), config.getName());
            return;
        }

        String category = resolveCategory(config);
        int scoreChange = config.getScoreChange() != null ? config.getScoreChange().intValue() : 0;

        for (Student student : targetStudents) {
            ScoreRequest request = ScoreRequest.builder()
                    .studentId(student.getId())
                    .scoreChange(scoreChange)
                    .reason(config.getReason() != null ? config.getReason() : config.getName())
                    .category(category)
                    .operatorId(null)
                    .canQuickRevert(false)
                    .build();
            scoreService.addScore(request);
        }

        config.setLastExecutedAt(LocalDateTime.now(ZONE));
        configService.updateById(config);

        log.info("自动评价配置 [id={}, name={}] 执行完成，影响 {} 名学生，分值变化: {}",
                config.getId(), config.getName(), targetStudents.size(), scoreChange);
    }

    private List<Student> resolveTargetStudents(AutoEvaluationConfig config) {
        String targetType = config.getTargetType();
        if (targetType == null) {
            targetType = "AllStudents";
        }

        switch (targetType) {
            case "AllStudents":
                return studentService.list();
            case "SpecificGroup":
                if (config.getTargetGroupId() != null) {
                    return studentService.listByGroupId(config.getTargetGroupId());
                }
                return new ArrayList<>();
            case "SpecificStudent":
                if (config.getTargetStudentId() != null) {
                    Student student = studentService.getById(config.getTargetStudentId());
                    if (student != null) {
                        List<Student> list = new ArrayList<>();
                        list.add(student);
                        return list;
                    }
                }
                return new ArrayList<>();
            default:
                return studentService.list();
        }
    }

    private String resolveCategory(AutoEvaluationConfig config) {
        if (config.getEvaluationItemId() != null) {
            try {
                EvaluationItem item = evaluationService.getById(config.getEvaluationItemId());
                if (item != null && item.getCategory() != null) {
                    return item.getCategory();
                }
            } catch (Exception ignored) {
            }
        }
        return "自动评价";
    }
}
