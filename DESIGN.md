# 可成长泰拉棱镜（Growable Terraprisma）—— 设计文档

## 概述

一个 tModLoader mod，添加可成长的泰拉棱镜召唤武器。复用原版泰拉棱镜的美术资源、渲染和 AI。
设计上兼容灾厄（Calamity）Mod 及其 Boss。

### 术语

| 术语       | 含义                                                          |
|----------- |-------------------------------------------------------------- |
| vprisma    | 原版泰拉棱镜（原版内部名 EmpressBlade，item 5005，proj 946）     |
| gtprisma   | 可成长泰拉棱镜（Growable Terraprisma）—— 初始物品，原版 AI，纯数值成长 |
| uprisma    | 究极泰拉棱镜（Ultra Terraprisma）—— 合成物品（vprisma + gtprisma），数值成长 + Boss 锁定行为 |

---

## 1. 物品架构

### 1.1  可成长泰拉棱镜（gtprisma）

- **类名**: `GrowableTerraprismaItem : ModItem`
- **获取方式**: 初始物品。角色创建时通过 `ModPlayer.AddStartingItems()` 放入背包，与铜工具一同发放。
- **基础属性**: 初始伤害约为原版泰拉棱镜的 60%，随成长逐步提升，上限可超过原版。
- **弹幕**: 直接生成**原版泰拉棱镜弹幕**（`ProjectileID.EmpressBlade`，type 946），不定义自定义弹幕类。
  ```csharp
  Item.shoot = ProjectileID.EmpressBlade;
  ```
- **AI**: 完全原版 — `aiStyle 156`（BatOfLight），`EmpressBladeDrawer` 顶点条带渲染，`GetFairyQueenWeaponsColor` 彩虹色调。
- **成长**: 纯数值成长 — 伤害随召唤物击杀数和 Boss 击败数提高。无阶段，无新行为。
- **成长数据**: 存储在 `GrowableTerraprismaPlayer`（ModPlayer）上。掉落、交易、死亡不丢失。
- **合成解锁**: 玩家击败光之女皇 AND gtprisma 成长点数达到阈值后，解锁 uprisma 合成配方。

### 1.2  究极泰拉棱镜（uprisma）

- **类名**: `UltraTerraprismaItem : ModItem`
- **获取方式**: 在秘银/山铜砧处合成。
  - 材料: 1× 原版泰拉棱镜（不消耗 gtprisma）
  - 条件: `NPC.downedEmpressOfLight` AND 玩家成长点数达到阈值
- **基础属性**: 基础伤害约为原版泰拉棱镜的 1.3 倍。
- **弹幕**: 自定义弹幕（`UltraTerraprismaProjectile`），在原版 EmpressBlade AI 基础上叠加可插拔行为层。
- **数值成长**: 继承并继续 gtprisma 的数值成长（击杀计数 + Boss 击败计数转移至 uprisma）。uprisma 自身继续累积。
- **行为成长**: 击败**特定 Boss**解锁对应 AI 行为（见 §3）。行为是原版 AI 的**叠加层**，不替换原版逻辑。

---

## 2. 数值成长系统

### 2.1  成长来源（两物品共用）

| 来源                   | 速率                     | 存储位置            |
|----------------------- |------------------------- |-------------------- |
| 召唤物击杀（任意 NPC）  | +1 每击杀                | Per-player（ModPlayer） |
| Boss 击败               | +25 每个独特 Boss NPC ID | Per-player（ModPlayer） |

### 2.2  数值缩放公式

不设硬上限，使用**对数递减**确保成长不会过快。多维度缩放：基础伤害、暴击率、暴击倍率、攻击速度。

```
growthPoints = minionKillCounter + (uniqueBossesDefeated × 25)

// 基础伤害倍率 — 对数递减，无上限但边际收益逐步降低
damageMultiplier = 1.0 + Math.Log(1 + growthPoints * 0.005) * 0.5

// 暴击率 — 满成长后约 +15%
critChanceBonus = Math.Min(growthPoints / 66.0, 15.0)

// 暴击倍率 — 满成长后约从 2× 升至 2.5×
critDamageMultiplier = 2.0 + Math.Log(1 + growthPoints * 0.002) * 0.3

// 攻击速度 — 满成长后约 +20%
attackSpeedMultiplier = 1.0 + Math.Log(1 + growthPoints * 0.003) * 0.2
```

**各时期参考数值**（以击杀 500 小怪 + 10 Boss ≈ 750 points 为例）：

| 时期 | 大致 growthPoints | 伤害倍率 | 暴击率 | 暴击倍率 | 攻速 |
|----- |-------------------|---------|--------|---------|------|
| 困难模式前 | ~100 | 1.2× | +1.5% | 2.05× | 1.03× |
| 困难模式 | ~300 | 1.5× | +4.5% | 2.12× | 1.08× |
| 月后 | ~600 | 1.7× | +9.1% | 2.18× | 1.13× |
| 灾厄月后 | ~1500 | 1.9× | +15% | 2.3× | 1.17× |
| 灾厄终局 | ~4000 | 2.1× | +15% | 2.45× | 1.2× |

- gtprisma：在较低基础伤害上应用四个乘数。
- uprisma：在较高基础伤害上应用四个乘数，且随击败特定 Boss 解锁新行为。

### 2.3  击杀追踪

`GrowableTerraprismaGlobalProjectile : GlobalProjectile`:
- `OnHitNPC` — 若弹幕属于持有 gtprisma/uprisma 的玩家，且该次命中为致死伤害，增加 `minionKillCounter`。

`GrowableTerraprismaGlobalNPC : GlobalNPC`:
- `OnKill` — 若该 NPC 为 Boss（`npc.boss == true`）且击杀者是持有 gtprisma/uprisma 的玩家，将 `npc.type` 加入 `defeatedBossTypes` 集合。
- **特定小 Boss**：以下非 Boss NPC 同样计入（与灾厄一致的小 Boss 定义）：
  - `NPCID.IceGolem`（冰雪巨人）
  - `NPCID.SandElemental`（沙元素）
  - `NPCID.Dreadnautilus`（恐惧鹦鹉螺）
  - `NPCID.PirateShip`（荷兰飞盗船）
  - `NPCID.Pumpking`（南瓜王）
  - `NPCID.IceQueen`（冰雪女王）
  - `NPCID.MartianSaucerCore`（火星飞碟）
  - 灾厄 mod 中 `npc.boss == false` 但被视为 mini-boss 的 NPC（通过配置列表扩展）。

### 2.4  数值成长视觉反馈

- gtprisma 保持原版外观，仅在 tooltip 显示成长数据。
- uprisma 达到成长阈值时触发短暂剑刃辉光脉冲。
- Tooltip 显示：击杀数、独特 Boss 击败数、当前伤害倍率。

---

## 3. uprisma 行为系统

### 3.1  设计原则

uprisma 始终以**原版 EmpressBlade AI 为基础**运行。行为是**叠加层**，在原版 AI 之后执行，永不替换原版逻辑。
每个行为由击败**特定 Boss**解锁。

### 3.2  行为接口

```csharp
public interface IUprismaBehavior
{
    /// 行为名称（在 tooltip 中显示）。
    string Name { get; }

    /// Tooltip 描述。
    string Description { get; }

    /// 解锁此行为所需的 Boss NPC type ID。-1 表示始终激活。
    int RequiredBossNPCType { get; }

    /// 给定玩家是否已解锁此行为。
    bool IsUnlocked(GrowableTerraprismaPlayer player);

    /// 每帧调用一次（每个活跃的 uprisma 弹幕）。
    /// 返回 false 阻止此行为本帧执行。
    bool CanRun(Projectile proj);

    /// 每帧 AI 逻辑。在原版 BatOfLight AI 之后执行。
    void AI(Projectile proj);

    /// 可选：在原版 EmpressBlade 渲染前后的自定义绘制。
    void OnPreDraw(Projectile proj) { }
    void OnPostDraw(Projectile proj) { }
}
```

### 3.3  行为注册表

`GrowableTerraprismaSystem : ModSystem` 维护静态注册表：

```csharp
public static class UprismaBehaviorRegistry
{
    public static List<IUprismaBehavior> Behaviors = new();

    public static void Register(IUprismaBehavior b) => Behaviors.Add(b);

    public static IEnumerable<IUprismaBehavior> GetUnlocked(GrowableTerraprismaPlayer player)
        => Behaviors.Where(b => b.IsUnlocked(player));
}
```

其他 mod 可通过 `Mod.Call` 注册行为：
```csharp
Mod.Call("GrowableTerraprisma", "RegisterBehavior", new MyCustomBehavior());
```

### 3.4  原版 Boss 对应行为

| 行为名称           | 解锁 Boss            | 效果描述                                        |
|------------------- |--------------------- |------------------------------------------------ |
| 加速之刃（Swift Blades）  | （始终激活）           | 此召唤物的移动速度 +15%。                          |
| 星辰加护（Star Aegis）    | 月亮领主              | 最大召唤栏位 +2。                                  |
| 利刃台风（Razor Typhoon） | 猪龙鱼公爵            | 周期性向目标发射小型台风弹幕。                      |
| 以太之舞（Ethereal Dance）| 光之女皇              | 召唤物留下造成伤害的残影轨迹。                      |
| 龙之怒（Dragon's Fury）   | 双足翼龙              | 目标生命值低于 50% 时 +10% 伤害。                  |

### 3.5  灾厄 Boss 对应行为（条件加载）

灾厄 Boss 约 30 个，仅关键剧情 Boss 解锁行为，其余贡献数值成长。

| 行为名称                      | 解锁 Boss（灾厄）    | 效果描述                                        |
|------------------------------ |--------------------- |------------------------------------------------ |
| 硫火余烬（Brimstone Ember）    | 硫火之灵              | 攻击附加 2 秒硫火灼烧 + 2 秒灵液。                   |
| 星辉脉冲（Astral Pulse）       | 星神游龙              | 每 5 次命中释放一次星辉冲击波。                      |
| 瘟疫传播（Plague Spread）      | 瘟疫使者歌莉娅         | 攻击附加 3 秒灾厄瘟疫。                             |
| 天罚（Providential Judgment）  | 普罗维登斯            | Boss 战中伤害 +15%。                              |
| 神噬（Cosmic Devour）          | 神明吞噬者            | 召唤物击中敌人有 3% 概率复制一个持续 2 秒的额外召唤物。  |
| 龙神之怒（Dragon's Wrath）     | 犽戎                  | 攻击有 5% 概率获得 3 秒 +50% 伤害。                 |
| 机械统御（Mechanical Dominion）| 星宇机甲              | 每存在一个此武器的召唤物，所有召唤物伤害 +3%。          |
| 硫磺湮灭（Brimstone Doom）     | 至尊灾厄              | Boss 存活时召唤物伤害 +25%，但持有者每秒 -2 HP。       |

### 3.6  灾厄兼容策略

- **不强依赖**: 灾厄 Boss 的 NPC type 在运行时通过 `ModLoader.TryGetMod("CalamityMod", out Mod calamity)` 解析。
- **type ID 缓存**: mod 加载时一次性查找并缓存：
  ```csharp
  if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)) {
      _providenceType = calamity.Find<ModNPC>("Providence")?.Type ?? -1;
      // ... 其余 Boss
  }
  ```
- **击败追踪**: 我们的 mod 通过 `GlobalNPC.OnKill` 独立追踪 Boss 击杀 — 该钩子无论 NPC 来自哪个 mod 都会触发。因此灾厄 Boss 被 gtprisma/uprisma 杀死后自动计入，无需特殊集成。
- **行为解锁判定**: `RequiredBossNPCType` 存储 NPC type ID。当灾厄未加载时，对应 type ID 为 -1，`IsUnlocked` 永远返回 false，行为不可用。
- **多体节 Boss**: 虫类 Boss（荒漠灾虫、神明吞噬者等）仅计一次击杀 — 通过只在 `bossArrayNPC` 中记录头部体节来去重。
- **行为数量**: 仅 8 个灾厄关键剧情 Boss 分配行为，其余灾厄 Boss 与所有普通 Boss 一样仅贡献数值成长（+25 成长点数）。

---

## 4. 全局成长路线

```
角色创建
  │
  ├─ gtprisma 放入背包（与铜工具一起）
  │
  ▼
前期 → 困难模式前 → 困难模式 → 月球领主后
  │                                    │
  │  gtprisma 纯数值成长                 │  击败光之女皇 → 获得 vprisma
  │  （所有 Boss 击杀 + 召唤物           │  合成 uprisma（vprisma + gtprisma）
  │    击杀均计入）                      │  gtprisma 保留在背包中
  │                                    ▼
  │                              uprisma 阶段：
  │                              数值继续成长（多维缩放）+
  │                              击败特定 Boss → 解锁对应行为
  │                                    │
  ▼                                    ▼
每个 Boss 击败贡献：              原版 Boss 行为（5个）
  - 数值成长点数                  灾厄关键 Boss 行为（8个）
  - 对应行为解锁（uprisma 阶段）    总计 13 个可解锁行为
```

### 4.1  Boss 击败记录

每个独特 Boss NPC type 计入一次。自动通过 `npc.boss` 检测，无需硬编码列表。
兼容所有 mod 新增的 Boss。

---

## 5. 数据持久化

### 5.1  ModPlayer（`GrowableTerraprismaPlayer`）

```csharp
public class GrowableTerraprismaPlayer : ModPlayer
{
    public int minionKillCounter;
    public HashSet<int> defeatedBossTypes = new();  // NPC type ID 集合
    public HashSet<int> defeatedMiniBossTypes = new();  // 特定小 Boss NPC type

    public int UniqueBossesDefeated => defeatedBossTypes.Count;
    public int UniqueMiniBossesDefeated => defeatedMiniBossTypes.Count;
    public float GrowthPoints => minionKillCounter + (UniqueBossesDefeated + UniqueMiniBossesDefeated) * 25;

    // 多维度缩放 — 对数递减，无硬上限
    public float DamageMultiplier       => 1f + MathF.Log(1 + GrowthPoints * 0.005f) * 0.5f;
    public float CritChanceBonus        => MathF.Min(GrowthPoints / 66f, 15f);
    public float CritDamageMultiplier   => 2f + MathF.Log(1 + GrowthPoints * 0.002f) * 0.3f;
    public float AttackSpeedMultiplier  => 1f + MathF.Log(1 + GrowthPoints * 0.003f) * 0.2f;

    // 生命周期
    public override void Initialize() {
        minionKillCounter = 0;
        defeatedBossTypes.Clear();
        defeatedMiniBossTypes.Clear();
    }

    public override void SaveData(TagCompound tag) {
        tag["kills"] = minionKillCounter;
        tag["bosses"] = defeatedBossTypes.ToList();
        tag["miniBosses"] = defeatedMiniBossTypes.ToList();
    }

    public override void LoadData(TagCompound tag) {
        minionKillCounter = tag.GetInt("kills");
        defeatedBossTypes = tag.Get<List<int>>("bosses")?.ToHashSet() ?? new();
        defeatedMiniBossTypes = tag.Get<List<int>>("miniBosses")?.ToHashSet() ?? new();
    }
}
```

### 5.2  无需保存的数据

- 行为解锁状态：运行时由 `defeatedBossTypes.Contains(behavior.RequiredBossNPCType)` 推导。
- 数值倍率：运行时由计数器计算。
- "阶段": 本设计中不存在阶段概念 — 行为由特定 Boss 解锁，数值由公式连续缩放。

---

## 6. 合成配方

### 6.1  究极泰拉棱镜（uprisma）

- **合成站**: 秘银砧 / 山铜砧
- **材料**: 1× 原版泰拉棱镜
- **消耗方式**: 合成时不消耗 gtprisma，仅消耗原版泰拉棱镜。gtprisma 保留在背包中继续使用或后续升级。
- **条件**: `NPC.downedEmpressOfLight` AND gtprisma 成长点数 ≥ 200（可配置）

---

## 7. 渲染（uprisma）

### 7.1  原版资源复用

uprisma 弹幕完全复用原版资源：
- **纹理**: `TextureAssets.Projectile[ProjectileID.EmpressBlade]`
- **拖尾**: `EmpressBladeDrawer` 结构体（原版顶点条带着色器）
- **颜色**: `Projectile.GetFairyQueenWeaponsColor()` 彩虹色调循环

### 7.2  行为相关视觉效果

每个已解锁行为可选通过 `OnPostDraw` 添加视觉元素：
- 穿透之光：命中时白色闪光
- 利刃台风：额外小型弹幕（使用自定义或原版贴图）
- 以太之舞：低透明度残影副本
- 龙之怒：目标低血量时剑刃变红
- 灾厄行为：对应 Boss 主题色粒子效果

---

## 8. 文件结构

```
GrowableTerraprisma/
├── build.txt
├── GrowableTerraprisma.cs
├── DESIGN.md
├── Localization/
│   └── en-US_Mods.GrowableTerraprisma.hjson
├── Content/
│   └── Items/
│       ├── GrowableTerraprismaItem.cs          # gtprisma — 初始物品，原版弹幕
│       └── UltraTerraprismaItem.cs             # uprisma — 合成品，自定义弹幕
├── Projectiles/
│   └── UltraTerraprismaProjectile.cs           # uprisma 召唤物：原版 AI + 行为层
├── Players/
│   └── GrowableTerraprismaPlayer.cs            # 击杀计数、Boss 追踪、初始背包
├── Systems/
│   └── GrowableTerraprismaSystem.cs            # ModSystem、行为注册表、Mod.Call
├── Behaviors/
│   ├── IUprismaBehavior.cs                     # 行为接口
│   ├── UprismaBehaviorRegistry.cs              # 静态注册表
│   ├── Vanilla/
│   │   ├── SwiftBladesBehavior.cs              # 始终激活：加速
│   │   ├── PiercingLightBehavior.cs            # 月亮领主：穿透
│   │   ├── RazorTyphoonBehavior.cs             # 猪龙鱼公爵：台风弹幕
│   │   ├── EtherealDanceBehavior.cs            # 光之女皇：残影
│   │   └── DragonsFuryBehavior.cs              # 双足翼龙：斩杀
│   ├── Calamity/
│   │   ├── CalamityBehaviors.cs                # 灾厄关键 Boss 行为（条件注册）
│   │   └── CalamityBossTypeCache.cs            # 灾厄 Boss type ID 缓存
├── Global/
│   ├── GrowableTerraprismaGlobalProjectile.cs  # 击杀追踪（OnHitNPC）
│   └── GrowableTerraprismaGlobalNPC.cs         # Boss 击杀追踪（OnKill）
└── Recipes/
    └── GrowableTerraprismaRecipes.cs           # uprisma 合成配方
```

---

## 9. 实施阶段

| 阶段 | 范围                                                      | 涉及文件                                    |
|----- |---------------------------------------------------------- |------------------------------------------- |
| 1    | gtprisma 物品（初始物品，原版弹幕）+ ModPlayer               | GrowableTerraprismaItem.cs, GrowableTerraprismaPlayer.cs |
| 2    | 击杀计数 + Boss 击败追踪（GlobalProjectile + GlobalNPC）    | GrowableTerraprismaGlobalProjectile.cs, GrowableTerraprismaGlobalNPC.cs |
| 3    | 数值成长（伤害缩放）+ tooltip                              | GrowableTerraprismaItem.cs, GrowableTerraprismaPlayer.cs |
| 4    | uprisma 物品 + 合成配方 + 自定义弹幕                        | UltraTerraprismaItem.cs, UltraTerraprismaProjectile.cs, Recipes/ |
| 5    | 行为接口 + 注册表 + 原版 Boss 行为                          | IUprismaBehavior.cs, UprismaBehaviorRegistry.cs, Behaviors/Vanilla/ |
| 6    | 灾厄兼容 + 灾厄关键 Boss 行为（8 个）                       | Behaviors/Calamity/, CalamityBossTypeCache.cs |
| 7    | 视觉润色 + 本地化 + 配置文件 + Mod.Call 集成                | 全部文件                                    |

---

## 10. 已确认决策

| # | 问题 | 决策 |
|---|------|------|
| 1 | **数值上限** | 不设硬上限。使用对数递减 + 多维度（伤害/暴击/攻速）控制倍率增幅，确保灾厄各时期数值合理。 |
| 2 | **gtprisma 消耗** | 合成 uprisma 时**保留** gtprisma，仅消耗原版泰拉棱镜。 |
| 3 | **非 Boss 里程碑** | 计入选定的小 Boss（冰雪巨人、沙元素、恐惧鹦鹉螺等，见 §2.3）。 |
| 4 | **多人模式** | 确保兼容，击杀追踪走持有者自身即可。 |
| 5 | **灾厄行为数量** | 仅 8 个关键剧情 Boss 解锁行为（硫火之灵→至尊灾厄），其余仅贡献数值成长。 |

