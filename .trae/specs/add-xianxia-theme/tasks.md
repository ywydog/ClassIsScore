# Tasks

- [x] Task 1: 创建修仙主题包基础架构
  - [x] 1.1 创建 `client/src/themes/xianxia/` 目录结构
  - [x] 1.2 创建 `terminology.ts` 术语映射表（所有术语键值对）
  - [x] 1.3 在 `types/index.ts` 中新增 ThemeMode 类型、CultivationLevel 类型、境界数据类型
  - [x] 1.4 在 `stores/settings.ts` 中新增 themeMode 状态和切换逻辑
  - [x] 1.5 创建术语映射工具函数 `useTerminology()` composable

- [x] Task 2: 实现64级道友境界体系
  - [x] 2.1 创建 `client/src/themes/xianxia/cultivationLevels.ts`，包含64级境界数据（名称、阈值，直接搬诸天修仙练气境界）
  - [x] 2.2 创建 `client/src/utils/cultivationSystem.ts`，包含境界判定函数、修为计算函数、突破判定函数
  - [x] 2.3 实现修为计算公式：修为 = 学期总积分 × (1 + 仙宠加成%) + 仙宠等级 × 5

- [x] Task 3: 实现仙宠渡劫系统
  - [x] 3.1 在 `petSystem.ts` 中新增渡劫逻辑：5级/8级渡劫触发条件
  - [x] 3.2 实现动态成功率计算：基础率 + 经验加成，上限80%
  - [x] 3.3 实现阶梯式失败惩罚（5档：40%/20%/10%/5%/0%）
  - [x] 3.4 创建 `client/src/components/xianxia/TribulationDialog.vue` 渡劫确认弹窗组件
  - [x] 3.5 实现渡劫动画效果（CSS雷劫特效）

- [x] Task 4: 实现道友突破机制
  - [x] 4.1 实现普通突破（level_id < 41）：确定性突破 + 提示弹窗
  - [x] 4.2 实现渡劫突破（level_id >= 41）：概率判定 + 失败惩罚
  - [x] 4.3 创建 `client/src/components/xianxia/BreakthroughDialog.vue` 突破提示组件
  - [x] 4.4 实现突破动画效果

- [x] Task 5: 修仙主题组件覆盖
  - [x] 5.1 ScoreDisplay.vue 集成修仙模式（仙界展示、强制卡片模式、境界+修为显示）
  - [x] 5.2 术语替换（道友/宗门/仙宠/灵力/悟道/魔障/修为/仙榜等）
  - [x] 5.3 修仙模式下的动态显示模式切换（effectiveDisplayMode）

- [x] Task 6: 古风视觉样式
  - [x] 6.1 创建 `client/src/themes/xianxia/styles.css` 古风样式覆盖（配色、边框、背景）
  - [x] 6.2 实现云纹/仙气背景动画
  - [x] 6.3 实现悟道（加分）灵力上升光效动画
  - [x] 6.4 实现魔障（减分）暗色特效动画
  - [x] 6.5 实现仙榜金色光晕装饰
  - [x] 6.6 实现仙宠等级边框颜色映射

- [x] Task 7: 设置页面主题切换入口
  - [x] 7.1 在 Settings.vue 中新增主题切换下拉选择
  - [x] 7.2 切换后立即生效（动态加载主题样式和组件）
  - [x] 7.3 主题偏好保存到 AppSettings 并同步后端

- [x] Task 8: 主题动态加载机制
  - [x] 8.1 通过 data-theme-mode 属性实现主题样式动态注入/移除
  - [x] 8.2 通过 useTerminology composable 实现术语映射的全局注入
  - [x] 8.3 TypeScript 类型检查通过

# Task Dependencies
- Task 2 depends on Task 1 (需要类型定义和主题架构)
- Task 3 depends on Task 1 (需要类型定义)
- Task 4 depends on Task 2 (需要境界体系数据)
- Task 5 depends on Task 1, Task 2, Task 3 (需要术语映射、境界数据、渡劫组件)
- Task 6 depends on Task 1 (需要主题架构)
- Task 7 depends on Task 1 (需要主题状态管理)
- Task 8 depends on Task 1, Task 5, Task 6 (需要组件和样式就绪)
