# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

可成长泰拉棱镜（Growable Terraprisma）的 tModLoader mod。详细设计见 `DESIGN.md`。

## 工作约定

- **每次代码修改必须同步更新 `DESIGN.md`**：增加/修改功能时更新对应章节，修复 bug 时补充实现细节。保持设计文档始终反映实际代码状态。

## 构建与加载

- **不使用命令行构建**。项目文件已链接到 tModLoader 的 `ModSources` 目录，无需移动。
- 在 WSL 中编辑 `.cs` 文件后，在游戏内 tModLoader 菜单中点击 **"Build + Reload"** 即可构建并加载 mod。
- 所有 `.csproj`、`build.txt`、资源文件均已在正确位置，tModLoader 会自动解析。

## 参考仓库

| 仓库 | 路径 | 用途 |
|------|------|------|
| 泰拉瑞亚反编译源码 | `~/projects/terraria/` | 原版 AI/渲染/物品实现 |
| tModLoader | `~/projects/Mods/terraria_mods/tModLoader/` | tModLoader API、ExampleMod 参考 |
| 灾厄 (Calamity) | `~/projects/Mods/terraria_mods/CalamityModPublic/` | 灾厄 Boss NPC type、兼容参考 |

## tModLoader wiki

[tModLoader wiki](https://github.com/tModLoader/tModLoader/wiki)

## 原版泰拉棱镜关键信息

- **内部名**: EmpressBlade（`ItemID.cs` 中 item 5005，`ProjectileID.cs` 中 proj 946）
- **弹幕 AI**: `aiStyle 156`（`AI_156_BatOfLight`），在 `Terraria/Projectile.cs` 中
- **渲染**: `EmpressBladeDrawer` 结构体（`Terraria.Graphics/EmpressBladeDrawer.cs`），使用 `GameShaders.Misc["EmpressBlade"]` 顶点条带着色器
- **颜色**: `Projectile.GetFairyQueenWeaponsColor()` — 彩虹色调循环
- **空闲位置**: 环形排列，逻辑在 `AI_156_GetIdlePosition`（Projectile.cs:48991），`type == 946` 分支
- **SetDefaults**: Projectile.cs:8638 行附近的 `type == 946` 段

## tModLoader 关键模式

### 类继承
- `ModItem` — 自定义物品（`Item.shoot` 赋值弹幕 type）
- `ModProjectile` — 自定义弹幕（重写 `AI()` 控制行为）
- `ModPlayer` — 玩家级数据（`SaveData`/`LoadData` 持久化，`AddStartingItems` 初始物品，`ResetEffects` 每帧重置）
- `ModSystem` — 世界/系统级数据（`SaveWorldData`/`LoadWorldData`）
- `GlobalProjectile` / `GlobalNPC` — 全局钩子，无需修改原版类型即可挂载行为

### 初始物品

使用 `ModPlayer.AddStartingItems(bool mediumCoreDeath)` 返回 `IEnumerable<Item>`。
参考: `tModLoader/ExampleMod/Common/Players/ExampleInventoryPlayer.cs`

### 数据持久化

`SaveData(TagCompound tag)` / `LoadData(TagCompound tag)` 用于 ModPlayer 保存玩家数据。
`TagCompound` 支持 `GetInt`、`GetBool`、`Get<List<T>>` 等方法。需在 `Initialize()` 中设默认值。

### 跨 Mod 兼容

```csharp
// 检测 mod 是否加载
ModLoader.TryGetMod("CalamityMod", out Mod calamity);

// 查找其他 mod 的类型
calamity.Find<ModNPC>("Providence")?.Type;

// 注册表/集成
Mod.Call("GrowableTerraprisma", "RegisterBehavior", instance);
```

## mod 文件结构（规划）

```
GrowableTerraprisma/
├── build.txt                    # displayName, author, version
├── GrowableTerraprisma.cs       # Mod 入口
├── Content/Items/               # ModItem 物品
├── Projectiles/                 # ModProjectile 弹幕
├── Players/                     # ModPlayer 玩家数据
├── Systems/                     # ModSystem 注册表
├── Behaviors/                   # IUprismaBehavior 行为
├── Global/                      # GlobalProjectile / GlobalNPC
├── Recipes/                     # 合成配方
└── Localization/                # 本地化 hjson
```

## 命名约定

- `gtprisma` — 可成长泰拉棱镜（Growable Terraprisma），初始物品
- `uprisma` — 究极泰拉棱镜（Ultra Terraprisma），合成品
- `vprisma` — 原版泰拉棱镜（Vanilla Terraprisma）

## 实施状态

| 阶段 | 描述 | 状态 |
|------|------|------|
| 1 | gtprisma + Buff + ModPlayer | ✅ 已完成 |
| 2 | 击杀追踪 (GlobalProjectile + GlobalNPC) | ✅ 已完成 |
| 3 | 数值成长 + tooltip | ✅ 已包含在阶段1 |
| 4 | uprisma + 合成 + 自定义弹幕 | ⬜ 待实现 |
| 5 | 行为接口 + 原版 Boss 行为 | ⬜ 待实现 |
| 6 | 灾厄兼容 | ⬜ 待实现 |
| 7 | 视觉润色 + 本地化 + 配置 | ⬜ 待实现 |

### 已创建文件

- `Content/Items/GrowableTerraprismaItem.cs` — gtprisma 物品，base damage 15，成长系数 0.004
- `Content/Items/GrowableTerraprismaItem.png` — 物品贴图（原版 Terraprisma 贴图，从 wiki webp 转换）
- `Content/Buffs/GrowableTerraprismaBuff.cs` — gtprisma 召唤栏 buff
- `Content/Buffs/GrowableTerraprismaBuff.png` — Buff 图标（原版 Terraprisma buff 贴图，从 wiki webp 转换）
- `Players/GrowableTerraprismaPlayer.cs` — ModPlayer，击杀计数/Boss击败集合/初始物品/buff生命周期
- `Global/GrowableTerraprismaGlobalProjectile.cs` — 弹幕生命周期（PostAI）+ 小怪击杀追踪（OnHitNPC），仅处理 localAI[2]==1f
- `Global/GrowableTerraprismaGlobalNPC.cs` — Boss/小Boss 击败追踪（OnKill），通过 playerInteraction 判断参战
- `scripts/convert_webp.py` — webp→png 转换脚本（uv + Pillow）

### 下一步

Phase 4: uprisma + 合成配方 + 自定义弹幕（UltraTerraprismaProjectile）。
