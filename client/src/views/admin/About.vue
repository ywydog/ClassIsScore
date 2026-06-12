<template>
  <div class="about-page">
    <div class="about-page__header">
      <h2>关于</h2>
    </div>

    <div class="about-page__content">
      <el-card class="about-page__card">
        <div class="about-page__app-info">
          <el-icon :size="64" color="var(--cis-primary)"><Trophy /></el-icon>
          <h3 class="about-page__app-name">ClassIsScore</h3>
          <p class="about-page__version">版本 1.0.0</p>
          <p class="about-page__description">
            一款面向课堂场景的积分管理工具，支持学生管理、积分记录、排行榜、宠物系统等功能。
          </p>
        </div>
      </el-card>

      <el-card class="about-page__card">
        <template #header>
          <span>技术栈</span>
        </template>
        <el-descriptions :column="1" border>
          <el-descriptions-item label="前端">Vue 3 + Element Plus + Pinia</el-descriptions-item>
          <el-descriptions-item label="后端">Spring Boot</el-descriptions-item>
          <el-descriptions-item label="桌面端">Electron</el-descriptions-item>
        </el-descriptions>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Trophy } from '@element-plus/icons-vue'
</script>

<style scoped>
.about-page__header {
  margin-bottom: 20px;
}

.about-page__header h2 {
  margin: 0;
  font-size: 20px;
  color: var(--cis-text-primary);
}

.about-page__content {
  max-width: 600px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.about-page__app-info {
  text-align: center;
  padding: 20px 0;
}

.about-page__logo {
  width: 80px;
  height: 80px;
  margin-bottom: 12px;
}

.about-page__app-name {
  margin: 0 0 4px;
  font-size: 22px;
  color: var(--cis-text-primary);
}

.about-page__version {
  margin: 0 0 12px;
  font-size: 14px;
  color: var(--cis-text-secondary);
}

.about-page__description {
  margin: 0;
  font-size: 14px;
  color: var(--cis-text-secondary);
  line-height: 1.6;
}
</style>
