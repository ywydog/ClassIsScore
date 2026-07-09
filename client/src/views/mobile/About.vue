<template>
  <div class="m-about">
    <header class="m-about__head">
      <div class="m-about__mark" aria-hidden="true">C</div>
      <h1 class="cis-display m-about__title">ClassIsScore</h1>
      <p class="m-about__sub">v1.0.0 · 教室积分管理工具</p>
    </header>
    <section class="m-about__list-section">
      <span class="cis-eyebrow m-about__section-label">项目</span>
      <ul class="m-about__list" role="list">
        <li><a class="m-about__link" href="https://github.com/ywydog/ClassIsScore" target="_blank" rel="noopener">仓库 →</a></li>
        <li><a class="m-about__link" href="https://github.com/ywydog/ClassIsScore/issues" target="_blank" rel="noopener">问题反馈 →</a></li>
      </ul>
    </section>
    <section class="m-about__list-section">
      <span class="cis-eyebrow m-about__section-label">致谢</span>
      <ul class="m-about__list" role="list">
        <li class="m-about__row">Vue 3</li>
        <li class="m-about__row">Element Plus</li>
        <li class="m-about__row">Tauri</li>
        <li class="m-about__row">Noto Sans / Serif SC</li>
      </ul>
    </section>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.m-about { display: flex; flex-direction: column; gap: 24px; }
.m-about__head { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 24px 0; }
.m-about__mark { width: 56px; height: 56px; display: flex; align-items: center; justify-content: center; background: var(--cis-primary); color: #fff; border-radius: var(--cis-radius-btn); font-family: var(--cis-font-serif); font-weight: 700; font-size: 28px; line-height: 1; }
.m-about__title { font-size: 24px; margin: 4px 0 0; font-weight: 600; }
.m-about__sub { margin: 0; font-size: 12px; color: var(--cis-text-tertiary); }
.m-about__section-label { display: block; margin-bottom: 8px; color: var(--cis-text-tertiary); }
.m-about__list { list-style: none; margin: 0; padding: 0; border: 1px solid var(--cis-border); border-radius: var(--cis-radius-card); overflow: hidden; }
.m-about__row, .m-about__link { display: flex; align-items: center; min-height: 48px; padding: 0 16px; border-bottom: 1px solid var(--cis-border-light); background: var(--cis-surface-1); color: var(--cis-text-primary); font-size: 15px; text-decoration: none; }
.m-about__list li:last-child .m-about__row, .m-about__list li:last-child .m-about__link { border-bottom: none; }
.m-about__link:active { background: var(--cis-primary-tint); color: var(--cis-primary); }
</style>
