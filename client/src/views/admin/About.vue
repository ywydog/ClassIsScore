<template>
  <div class="about-page">
    <h2 id="about-title" class="about-page__header">关于</h2>

    <div class="about-page__content">
      <el-card class="about-page__card">
        <div class="about-page__app-info">
          <el-icon :size="72" color="var(--cis-primary)" aria-hidden="true"><Trophy /></el-icon>
          <h3 class="about-page__app-name"><span translate="no">ClassIsScore</span></h3>
          <p class="about-page__version">版本 1.0.0-web</p>
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
          <el-descriptions-item label="前端框架">Vue 3.5 + TypeScript</el-descriptions-item>
          <el-descriptions-item label="UI 组件">Element Plus</el-descriptions-item>
          <el-descriptions-item label="状态管理">Pinia</el-descriptions-item>
          <el-descriptions-item label="桌面端">Electron 33</el-descriptions-item>
          <el-descriptions-item label="后端框架">Spring Boot 3.3</el-descriptions-item>
          <el-descriptions-item label="数据库">H2 Embedded</el-descriptions-item>
          <el-descriptions-item label="ORM">MyBatis-Plus</el-descriptions-item>
        </el-descriptions>
      </el-card>

      <el-card class="about-page__card">
        <template #header>
          <span>开源信息</span>
        </template>
        <el-descriptions :column="1" border>
          <el-descriptions-item label="开发者">ywydog</el-descriptions-item>
          <el-descriptions-item label="仓库">
            <el-link type="primary" href="https://github.com/ywydog/ClassIsScore" target="_blank">
              github.com/ywydog/ClassIsScore
            </el-link>
          </el-descriptions-item>
          <el-descriptions-item label="许可证">MIT License</el-descriptions-item>
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
  margin: 0 0 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--cis-border-color-light);
  font-family: var(--cis-font-family-display);
  font-size: 22px;
  color: var(--cis-text-primary);
  padding-left: 12px;
  border-left: 3px solid var(--cis-primary);
  background: linear-gradient(135deg, var(--cis-primary), var(--cis-primary-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  scroll-margin-top: 80px;
}

.about-page__content {
  max-width: 600px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.about-page__card {
  background: var(--cis-card-bg);
  border-radius: var(--cis-radius-lg);
  box-shadow: var(--cis-shadow-card);
  transition: box-shadow var(--cis-transition-fast);
}

.about-page__card:focus-visible {
  outline: 2px solid var(--cis-primary);
  outline-offset: 2px;
}

.about-page__card:hover {
  box-shadow: var(--cis-shadow-card-hover);
}

.about-page__app-info {
  text-align: center;
  padding: 24px 0;
}

.about-page__app-name {
  margin: 12px 0 4px;
  font-family: var(--cis-font-family-display);
  font-size: 24px;
  background: var(--cis-gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
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

@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
</style>
