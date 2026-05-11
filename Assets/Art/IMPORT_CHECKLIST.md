# 素材整合导入清单 (IMPORT_CHECKLIST)

本清单由 Claude Code 在 2026-05-11 根据 `D:\gameData\游戏素材` 任务包整合生成。
当前项目走的是 **运行时程序化构建**（见 `Assets/Scripts/GameRuntimeBootstrap.cs`），
所有 `BackdropL/R`、`Ground`、`Platform`、`Player`、`InkCritter`、`AttackCircleFX`、
血条 `Slider` 都是脚本在 `Awake` 时由 `Assets/Art/Blockout/WhiteSquare64.png` 生成的方块。
因此第一版整合不修改玩法逻辑，只把"白方块的 Sprite 来源"换成新美术资源即可。

---

## 1. 背景

- 路径：`Assets/Art/Backgrounds/Ink/InkBackground.png`
- 来源：`世界与关卡/背景/水墨背景.png`
- 用途：替换 `GameRuntimeBootstrap.BuildBackdrop` 生成的 `BackdropL / BackdropR / BackdropC` 的 Sprite
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Pixels Per Unit: 100（保持与现有逻辑一致）
  - Wrap Mode: `Clamp`
  - Filter Mode: `Bilinear`
  - Compression: `None` 或 `High Quality`

---

## 2. 主角（Sword sprites，77 帧）

- 路径：`Assets/Art/Characters/Fighter/Frames/`
- 来源：`主角/Stick Figure Character Sprites 2D.zip` 解压后的 `Sword sprites` 文件夹
- 文件名前缀分组（已在本地校对）：

| 动作（推荐 Animator State 名） | 文件名前缀 | 帧数 |
|---|---|---|
| Idle  | `sword_Idle_`        | 8 |
| Run   | `sword_run_`         | 8 |
| Walk  | `sword_walk_`        | 8 |
| Jump  | `sword_jump_`        | 5 |
| Air   | `sword_air_attack_`  | 3 |
| Combo / Attack | `sword_combo_` | 11 |
| Dash  | `sword_dash_`        | 6 |
| Slide | `sword_slide_`       | 8 |
| Climb | `sword_climb_`       | 4 |
| Hit   | `sword_hit_`         | 4 |
| Death | `sword_death_`       | 10 |
| WallSlide | `sword_wallslide00` | 2 |

- 推荐导入设置（全选这 77 张 PNG）：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Pixels Per Unit: 100
  - Filter Mode: `Point (no filter)` 或 `Bilinear`（看美术意图，剪影风建议 Bilinear）
  - Compression: `None`
- Animator/Animation 由人工在编辑器中创建（详见"人工步骤"）。

---

## 3. 小怪

- 路径：
  - `Assets/Art/Enemies/Spritesheet9.png`（**第一版默认使用**）
  - `Assets/Art/Enemies/Spritesheet1.png`（备用）
- 来源：`怪物/Spritesheet9.png`、`怪物/Spritesheet1.png`
- 用途：替换 `GameRuntimeBootstrap` 中 `SimpleEnemy / InkCritter` 的 `SpriteRenderer.sprite`
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Multiple`
  - Pixels Per Unit: 100
  - 用 Sprite Editor 按 Grid By Cell Size 或自动切片切出小怪逐帧
- 单帧风格简化版可临时只取一帧给方块替换。

---

## 4. Boss

- 路径：`Assets/Art/Boss/Madness.png`
- 来源：`怪物/Madness.png`
- 用途：替换 `BossDemonKing / BossInkTree` 的 `SpriteRenderer.sprite`
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`（如果是 sheet 则切 `Multiple`）

---

## 5. 墨爆范围圈

- 路径：`Assets/Art/VFX/AttackCircle/Frames/teleportCircle.png`
- 来源：`墨爆/方法2/teleportCircle.png`
- 用途：`PlayerCombat.cs` 内 `AttackCircleFX` 程序化生成时引用的圆形纹理
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Multiple`
  - 在 Sprite Editor 中按动画帧切（teleportCircle 一般为水平条带 sheet）

---

## 6. 受击闪 (HitSpark)

- 路径：
  - `Assets/Art/VFX/HitSpark/Frames/HitSpark_01.png`
  - `Assets/Art/VFX/HitSpark/Frames/HitSpark_02.png`
  - `Assets/Art/VFX/HitSpark/Frames/HitSpark_03.png`
- 来源：`受击特效/Hit Effect 01.rar` 解压后的 3 张 PNG（已重命名）
- 用途：被 `Hurtbox / SimpleEnemy / BossDemonKing` 命中时播放
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Filter Mode: `Bilinear`
  - Alpha is Transparency: 勾选

---

## 7. UI - 面板与按钮 (Kenney 9-slice)

- 路径：`Assets/Art/UI/Panels/`
  - `Border/`           : 32 个边框九宫格 PNG
  - `Panel/`            : 32 个填充面板 PNG
  - `TransparentBorder/`: 32 个透明边框
  - `TransparentCenter/`: 32 个透明中心
- 来源：`UI/面板底图 _ 按钮九宫格.zip` 解压后的 `PNG/Default`
- 第一版推荐优先使用：
  - `Assets/Art/UI/Panels/Panel/panel-000.png`（基础面板底图）
  - `Assets/Art/UI/Panels/Border/panel-border-000.png`（基础边框）
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Mesh Type: `Full Rect`
  - 在 Sprite Editor 设置 `Border (L/T/R/B)`，按视觉判断 4~12 像素
  - 在 UI 中用 `Image.Type = Sliced`

---

## 8. UI - 装饰条

- 路径：
  - `Assets/Art/UI/Decorations/Divider/`     : 6 个普通分隔条
  - `Assets/Art/UI/Decorations/DividerFade/` : 6 个渐变分隔条
- 用途：用于关卡过场提示 `SectionContinuePrompt`、胜利面板 `VictoryPanel` 的视觉点缀

---

## 9. UI - 血条 (HUD)

- 路径：`Assets/Art/UI/HUD/`
  - `HealthBar_Composite.png`        : 完整成图（最快替换）
  - `HealthBar_BG.png`               : 背板
  - `HealthBar_Red.png` / `HealthBar_Yellow.png` : 血条填充色
  - `HealthBar_FrameRing.png`        : 头像环
  - `HealthBar_RingProfileUp.png` / `RingProfileBottom.png` / `RingProfileBottom2.png`
- 来源：`UI/血条.rar` 解压
- 第一版最小替换：把脚本里 `CreateHealthSlider` 创建的纯色 `Image.color` 换成
  `HealthBar_Red.png` 作为 Slider 的 `Fill` Sprite，
  `HealthBar_BG.png` 作为 Slider 的 `Background` Sprite。
- 推荐导入设置：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Alpha is Transparency: 勾选

---

## 10. 拾取物（bonus，已本地存在）

- 路径：`Assets/Art/Pickups/`
  - `Pickup_BrushWhite.png`（白毫短笔）
  - `Pickup_BrushTeal.png` （翠色树心笔）
  - `Pickup_RobePlain.png` （素绢短衫）
  - `Pickup_TrioSet.png`   （三件套合集）
- 用途：替换 `Pickup.cs` 显示的方块 Sprite。

---

## 11. 待补下载（本地不存在）

下列在素材包文档中提到，但 **本地仓库内未提供** 的资源已在清单中标记：

- 字体：第一版未在 `游戏素材` 中提供，建议在团结引擎 `Project Settings` 或 TMP 中临时使用内置字体；
  后续可放到 `Assets/Art/Fonts/`。
- 技能图标：未在 `游戏素材/UI/` 找到，目录 `Assets/Art/UI/SkillIcons/` 已预留。
- 拾取物动效贴图：仅静态 PNG 可用，动效需要在编辑器中手动制作或后续补素材。
- 墨爆 方法1 的 Asset Store VFX：仅文档说明，本地无资源。
- 主角的 `Pistol sprites` / `Extras` / `hit effect`：已随 Stick Figure zip 解压到
  `D:\gameData\游戏素材\主角\StickFigure_Extracted\...`，但 **未拷入项目**，
  仅 `Sword sprites` 入项目（按默认策略）。

---

## 12. 人工步骤（在团结引擎编辑器中完成）

1. 在 Project 窗口选中本清单提到的所有图片，按上面"推荐导入设置"配置 `Inspector → Apply`。
2. `teleportCircle.png` 与 `Spritesheet9.png` 进入 `Sprite Editor` 切片为 `Multiple`。
3. 在 `Assets/Art/Characters/Fighter/Animations/` 下：
   - 选中同一前缀的所有 PNG（如 `sword_Idle_*` 的 8 张），拖到 Hierarchy 创建 `.anim`，
     重命名为 `Fighter_Idle.anim`，按动作集依次做：
     `Fighter_Idle / Fighter_Run / Fighter_Jump / Fighter_AirAttack / Fighter_Combo / Fighter_Hit / Fighter_Death`。
   - 在 `Controllers/` 新建 `FighterController.controller`，把上述动画拖入，连接默认 Transition。
4. 在 `Assets/Art/VFX/AttackCircle/Animations/` 下创建 `AttackCircle_Play.anim`。
5. 在 `Assets/Art/VFX/HitSpark/Animations/` 下用 3 张 PNG 创建 `HitSpark_Play.anim`，
   FrameRate 建议 12~16。
6. 替换 `SpriteRenderer.sprite`（无需改脚本逻辑）：
   - 方式 A（最小改动）：让 `GameRuntimeBootstrap.cs` 读取 Resources / 公开字段，把对应 Sprite 拖入 Inspector。
   - 方式 B（直接改脚本）：将 `WhiteSquare64.png` 的 `Resources.Load` 换成新 Sprite 引用。
   - **本次自动整合不动玩法代码，因此该步留给人工决策。**
7. 检查每个新 SpriteRenderer 的 `Sorting Layer / Order in Layer` 与原方块一致，
   防止穿插（背景在最底层，平台在中间，角色与特效在最上层）。
8. 检查 `Collider2D` 尺寸：新 Sprite 尺寸与白方块不同，可能需要重新配置 `BoxCollider2D.size`，
   尤其是 `Player / SimpleEnemy / InkCritter / BossDemonKing`。
9. UI Image 全部改为 `Image.Type = Sliced`，并在 Sprite Editor 设置 `Border`。

---

## 13. 资源在项目中的最终位置（一览）

```
Assets/Art/
  Backgrounds/Ink/InkBackground.png
  Characters/Fighter/Frames/sword_*.png        (77 frames)
  Characters/Fighter/Animations/                (空，待人工创建 .anim)
  Characters/Fighter/Controllers/               (空，待人工创建 .controller)
  Enemies/Spritesheet1.png
  Enemies/Spritesheet9.png
  Boss/Madness.png
  Platforms/                                    (空，第一版复用 WhiteSquare64)
  VFX/AttackCircle/Frames/teleportCircle.png
  VFX/AttackCircle/Animations/                  (空)
  VFX/AttackCircle/Prefabs/                     (空)
  VFX/HitSpark/Frames/HitSpark_0[1-3].png
  VFX/HitSpark/Animations/                      (空)
  VFX/HitSpark/Prefabs/                         (空)
  UI/Panels/Border/panel-border-*.png           (32)
  UI/Panels/Panel/panel-*.png                   (32)
  UI/Panels/TransparentBorder/*.png             (32)
  UI/Panels/TransparentCenter/*.png             (32)
  UI/HUD/HealthBar_*.png                        (8 张)
  UI/Decorations/Divider/*.png                  (6)
  UI/Decorations/DividerFade/*.png              (6)
  UI/Buttons/                                   (空，按钮可复用 Border + Panel 九宫格组合)
  UI/SkillIcons/                                (待补下载)
  Pickups/Pickup_*.png                          (4)
  Fonts/                                        (待补下载)
  Blockout/WhiteSquare64.png                    (保留，作为程序化兜底)
```

---

## 14. 第一版整合"完成判定"

按 `游戏素材/总说明.md` 的可展示版标准：

- [ ] 场景背景不再是灰块 → 替换 `BackdropL/R/C` Sprite 为 `InkBackground.png`
- [ ] 玩家不再是方块 → 给 `Player` 加载 `Fighter_Idle` 动画（最少 Idle + Run）
- [ ] 小怪不再是方块 → 给 `SimpleEnemy / InkCritter` 设置 `Spritesheet9` 切片中的一帧
- [ ] 墨爆范围圈能显示 → 给 `AttackCircleFX` 用 `teleportCircle` 单帧或动画
- [ ] 受击闪能显示 → `Hurtbox` 命中时实例化 `HitSpark_Play.anim`
- [ ] 血条不再是纯色 → `CreateHealthSlider` 输出的 Slider 设置 `HealthBar_BG / Red` Sprite
