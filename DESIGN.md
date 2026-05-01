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
- **基础属性**: 初始伤害 15，随成长逐步提升。
- **弹幕**: 自定义弹幕 `GrowableTerraprismaProjectile : ModProjectile`，完整移植原版 EmpressBlade（type 946）的 AI 和渲染逻辑。
  ```csharp
  Item.shoot = ModContent.ProjectileType<GrowableTerraprismaProjectile>();
  ```
- **AI**: 完整移植原版 `AI_156_BatOfLight` + `AI_156_Think` 状态机（空闲环形排列 → 锁定目标 → 弧线接近 → 冲刺）。不使用 `aiStyle = 156`，而是将原版代码改写为 ModProjectile 自有方法。
- **渲染**: 移植原版 `EmpressBladeDrawer` 顶点条带拖尾 + `DrawProj_EmpressBlade` 精灵绘制（残影 + 辉光），使用 `GameShaders.Misc["EmpressBlade"]` 着色器和 `GetFairyQueenWeaponsColor()` 彩虹色调循环。
- **buff 生命周期**: 采用灾厄 `CelestialAxeMinion` 模式 — 弹幕 AI 中 `player.AddBuff(buffType, 3600)` 续期，buff `Update()` 中检查 `ownedProjectileCounts` 维持 bool，`ResetEffects` 中重置 bool。
- **成长**: 纯 Boss 击败伤害加成 — 击败 Boss 直接增加 `Item.damage`（通过 `ModifyWeaponDamage` 追加到计算基底，兼容重铸和装备加成）。无击杀点数，无阶段。
- **成长数据**: 存储在 `GrowableTerraprismaPlayer`（ModPlayer）上。掉落、交易、死亡不丢失。
- **合成解锁**: 玩家击败光之女皇 AND gtprisma 成长点数达到阈值后，解锁 uprisma 合成配方。

### 1.2  究极泰拉棱镜（uprisma）

- **类名**: `UltraTerraprismaItem : ModItem`
- **获取方式**: 在秘银/山铜砧处合成。
  - 材料: 1× 原版泰拉棱镜（不消耗 gtprisma）
  - 条件: `NPC.downedEmpressOfLight` AND 玩家成长点数达到阈值
- **基础属性**: 基础伤害约为原版泰拉棱镜的 1.3 倍。
- **弹幕**: 自定义弹幕（`UltraTerraprismaProjectile : ModProjectile`）。
  - 原版 AI 复用：`Projectile.aiStyle = 156`，运动/索敌/环形排列全部复用原版 `AI_156_BatOfLight`
  - 行为叠加层在 `PostAI()` 中执行，不替换原版逻辑
  - 参考灾厄 `CelestialAxeMinion` 模式：buff 直接在弹幕 `AI()` 中 `AddBuff(3600)` 维持，比当前 gtprisma 的 `GlobalProjectile.PostAI` 更干净
- **与 gtprisma 对比**：gtprisma 侧重功能性成长（光照、拾取、移速），uprisma 侧重战斗行为成长（台风弹幕、残影、debuff）。两者都是自有 `ModProjectile`，互不依赖
- **数值成长**: 继承并继续 gtprisma 的数值成长（击杀计数 + Boss 击败计数转移至 uprisma）。uprisma 自身继续累积。
- **行为成长**: 击败**特定 Boss**解锁对应 AI 行为（见 §3）。行为是原版 AI 的**叠加层**，不替换原版逻辑。

---

## 2. 数值成长系统

**纯 Boss 基础伤害**。Boss 加成直接叠加到 `Item.damage`（尚未受装备/前缀加成的基础值），面板显示与实际伤害一致。

```
最终伤害 = (15 + BossesBaseBonus) × 装备加成 × 前缀
```

### 2.1  Boss 基础伤害（9 阶段）

后期 Boss 加成远高于前期。末尾两阶段（神明吞噬者+）数值大幅跳升，匹配灾厄终局膨胀。

| 阶段 | 加成 | 原版 Boss | 灾厄 Boss（条件加载） |
|------|------|----------|---------------------|
| 1 | +3 | 史莱姆王、克苏鲁之眼、EoW/BoC | 荒漠灾虫、菌生蟹 |
| 2 | +5 | 蜂王、骷髅王、独眼巨鹿、血肉墙 | 腐巢意志/血肉宿主、史莱姆之神 |
| 3 | +8 | 史莱姆皇后、毁灭者、双子魔眼、机械骷髅王 | 渊海灾虫、硫磺火元素、极地之灵 |
| 4 | +15 | 世纪之花 | 灾厄之影、利维坦/阿娜希塔、白金星舰 |
| 5 | +25 | 石巨人、猪鲨、光之女皇、拜月教徒、月亮领主 | 瘟疫使者歌莉娅、毁灭魔像、星神游龙 |
| 6 | +50 | — | 亵渎守卫、痴愚金龙、亵渎天神 |
| 7 | +100 | — | 风暴编织者、无尽虚空、西格纳斯、噬魂幽花、硫海遗爵 |
| 8 | +200 | — | 神明吞噬者、犽戎 |
| 9 | +400 | — | 星流巨械（4体）、至尊灾厄、始源妖龙 |

**数值预测**（原版 + 灾厄全 Boss，gtprisma）：
- 阶段 1-2（困难模式前）：~11 Boss → +15  +30  +30  = **+75**
- 阶段 3-4（困难模式）：~12 Boss → +56  +75  = **+131**
- 阶段 5（月后）：8 Boss → **+200**
- 阶段 6（亵渎）：3 Boss → **+150**
- 阶段 7（噬魂）：5 Boss → **+500**
- 阶段 8（神明吞噬者+）：2 Boss → **+400**
- 阶段 9（终局）：6 Boss → **+2400**
- 基础 15 + 75 + 131 + 200 + 150 + 500 + 400 + 2400 = **3871**
- uprisma ×1.3 = **5032**（匹配灾厄终局水平）

未纳入列表的普通 Boss 默认 +3（阶段一加成）。uprisma 基础伤害 = gtprisma 基础伤害 × 1.3。

### 2.2  实现

**Boss 击败** — `GrowableTerraprismaGlobalNPC.OnKill`：遍历玩家，持 gtprisma buff + `npc.playerInteraction[i]` + `npc.boss` → 加入 `defeatedBossTypes`。

**伤害计算** — `GrowableTerraprismaItem.ModifyWeaponDamage()`：`damage.Base += BossesBaseBonus`。追加到基底（prefix 和装备加成之前），兼容重铸系统。每秒仅 Boss 击败时触发，无每帧开销。

**面板修正** — `ModifyTooltips`：通过 `player.GetWeaponDamage(Item)` 获取真实有效伤害，替换原版 Damage tooltip 行显示修正后数值。

**buff-弹幕生命周期**（灾厄 `CelestialAxeMinion` 模式）：
- `GrowableTerraprismaBuff.Update()` — `ownedProjectileCounts[type] > 0` → 设 `growableMinionActive = true`，`buffTime = 18000`；否则 `DelBuff`
- `GrowableTerraprismaPlayer.ResetEffects()` — `growableMinionActive = false`
- `GrowableTerraprismaProjectile.AI()` — `player.AddBuff(buffType, 3600)`；`!growableMinionActive` → 弹幕死亡

**被动能力注入点**：
- **持有者能力**（发光、+1 栏位、移速）→ `GrowableTerraprismaBuff.Update()`，在生命周期维护后、`buffTime` 续期后执行
- **召唤物能力**（蜜蜂、强光、棱镜、拾取、穿甲）→ `GrowableTerraprismaProjectile.AI()`，在 `_blacklist.Clear()` 后、`AI_156_Think` 前执行
- **Buff 说明文本** → `GrowableTerraprismaBuff.ModifyBuffText()`，根据 `defeatedBossTypes` 动态追加行

### 2.3  gtprisma 功能性成长

gtprisma 击败特定 Boss 后解锁**被动能力**。区别于 uprisma 的行为叠加层，gtprisma 的能力直接作用于持有者或召唤物自身，无需行为接口。

**所有能力仅在 gtprisma 召唤物存活时生效**。影响持有者的增益（发光、移动速度等）通过 `GrowableTerraprismaBuff.Update()` 施加，类似药水效果的运作方式。

#### 已设计能力

| Boss | 能力 | 阶段 | 说明 |
|------|------|------|------|
| 史莱姆王（King Slime） | **自身发光** | 1 | ✅ 已实现。持有者发出基础光源，通过 `GrowableTerraprismaBuff.Update()` 施加。 |
| 蜂王（Queen Bee） | **工蜂护卫** | 2 | ✅ 已实现。首个召唤物每 2.5 秒补充蜜蜂，伤害 = 当前弹幕伤害 × 0.35，aiStyle 42。 |
| 史莱姆之神（Slime God） | **+1 召唤栏位** | 2 | ✅ 已实现。召唤物存活时 +1 召唤栏位，通过 `GrowableTerraprismaBuff.Update()` 施加。 |
| 血肉墙（Wall of Flesh） | **强光 + 血嗜** | 2 | ✅ 已实现。召唤物发出强光（1.2/0.9/1.8）+ 每 10 次命中回复持有者 1 HP。 |
| 史莱姆皇后（Queen Slime） | **移动 +10%** | 3 | ✅ 已实现。持有者移动速度 +10%，通过 `GrowableTerraprismaBuff.Update()` 施加。 |
| 毁灭者（The Destroyer） | **自动拾取** | 3 | ✅ 已实现。空闲状态每 120 帧搜索 50 格半径内掉落物，召唤物直线冲刺（22px/帧）拾取后返回玩家（18px/帧）释放。拾取金币/银币；HP<90%时拾取心，MP<90%时拾取魔力星，心/魔力星优先级高于攻击。多召唤物并发拾取，搜索帧错开5帧，使用 `ItemFetchLockSystem` 物品锁定+冷却机制防止竞态。Shift+右键切换 FocusOnFetching 优先拾取模式（攻击中也拾取普通物品）。 |
| 双子魔眼（The Twins） | **微型棱镜** | 3 | ✅ 已实现。所有召唤物共享命中计数，累计 20 次攻击向目标发射一枚缩小泰拉棱镜弹幕（伤害 = 原版伤害 × 0.4，穿透 3 次）。弹幕自动追踪最近敌人，存活 3 秒。计数器存储在 `GrowableTerraprismaPlayer.miniPrismHitCounter`。 |
| 机械骷髅王（Skeletron Prime） | **穿甲** | 3 | ✅ 已实现。召唤物攻击无视目标 10 点防御。在 `ModifyHitNPC` 中设置 `modifiers.ArmorPenetration += 10`。 |
| 世纪之花（Plantera） | **生命回复** | 4 | ✅ 已实现。持有者生命回复 +5（约 +2.5 HP/s），通过 `GrowableTerraprismaBuff.Update()` 施加。 |

#### 实现模式

所有能力通过 `defeatedBossTypes.Contains(NPCID)` 判定解锁，写入对应钩子：

- **持有者能力**（发光、移动速度、+1 栏位、生命回复）→ `GrowableTerraprismaBuff.Update()`，仅在召唤物存活时生效
- **召唤物能力**（蜜蜂、强光、棱镜、拾取、穿甲）→ `GrowableTerraprismaProjectile` AI/ModifyHitNPC

#### 未设计能力（后续扩展）

以下 Boss 暂未设计能力：克苏鲁之眼、EoW/BoC、骷髅王、石巨人、猪鲨、光之女皇、拜月教徒、月亮领主。可在后续版本补充。

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
  │  gtprisma 纯 Boss 基础伤害成长       │  击败光之女皇 → 获得 vprisma
  │  （击败 Boss → Item.damage 增加）    │  合成 uprisma（vprisma + gtprisma）
  │                                    │  gtprisma 保留在背包中
  │                                    ▼
  │                              uprisma 阶段：
  │                              Boss 数值继续成长 +
  │                              击败特定 Boss → 解锁对应行为
  │                                    │
  ▼                                    ▼
每个 Boss 击败贡献：              原版 Boss 行为（5个）
  - +2~18 基础伤害                灾厄关键 Boss 行为（8个）
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
    public HashSet<int> defeatedBossTypes = new();  // NPC type ID 集合

    public int BossesBaseBonus => defeatedBossTypes.Sum(t => GetBossBonus(t));

    public override void Initialize() {
        defeatedBossTypes.Clear();
    }

    public override void SaveData(TagCompound tag) {
        tag["bosses"] = defeatedBossTypes.ToList();
    }

    public override void LoadData(TagCompound tag) {
        defeatedBossTypes = tag.Get<List<int>>("bosses")?.ToHashSet() ?? new();
    }
}
```

### 5.2  无需保存的数据

- 行为解锁状态：运行时由 `defeatedBossTypes.Contains(behavior.RequiredBossNPCType)` 推导。
- `BossesBaseBonus`：运行时由 `defeatedBossTypes.Sum(GetBossBonus)` 计算。
- `Item.damage`：不直接修改。额外伤害通过 `ModifyWeaponDamage` 运行时追加到 `damage.Base`，Tooltip 由 `GetWeaponDamage(Item)` 动态生成。

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
- **拖尾**: 精灵残影方案（遍历 `oldPos`/`oldRot` 绘制渐进透明精灵副本，兼容 ModProjectile.PreDraw），gtprisma 则使用原版 `EmpressBladeDrawer` 顶点条带
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
│   ├── en-US_Mods.GrowableTerraprisma.hjson
│   ├── en-US_Mods.GrowableTerraprisma.Configs.hjson
│   ├── zh-Hans_Mods.GrowableTerraprisma.Configs.hjson
├── Common/
│   └── Configs/
│       └── GrowableTerraprismaConfig.cs         # ModConfig — 基础伤害/Boss层级加成/uprisma倍率
├── Content/
│   ├── Items/
│   │   ├── GrowableTerraprismaItem.cs          # gtprisma — 初始物品，自定义弹幕
│   │   └── UltraTerraprismaItem.cs             # uprisma — 合成品，自定义弹幕
│   ├── Projectiles/
│   │   ├── GrowableTerraprismaProjectile.cs    # gtprisma 召唤物：完整移植原版 AI + 渲染
│   │   └── UltraTerraprismaProjectile.cs       # uprisma 召唤物：原版 AI + 行为层
│   └── Buffs/
│       └── GrowableTerraprismaBuff.cs          # 召唤栏 buff（灾厄更新模式）
├── Players/
│   └── GrowableTerraprismaPlayer.cs            # Boss 击败追踪 + 层级判定
├── Systems/
│   ├── GrowableTerraprismaSystem.cs            # ModSystem、行为注册表、Mod.Call
│   └── ItemFetchLockSystem.cs                  # 自动拾取物品锁定+冷却机制（防竞态、世界卸载清理）
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
│   └── GrowableTerraprismaGlobalNPC.cs         # Boss 击杀追踪（OnKill）
└── Recipes/
    └── GrowableTerraprismaRecipes.cs           # uprisma 合成配方
```

---

## 9. 配置系统

基于 tModLoader `ModConfig`，`ServerSide` 范围。在游戏内 `设置 → 模组配置` 中可调。

| 配置项 | 类型 | 默认值 | 范围 | 说明 |
|--------|------|--------|------|------|
| `BaseDamage` | int | 15 | 1–100 | 武器初始基础伤害 |
| `Phase1Bonus` | int | 3 | 0–50 | 史莱姆王、克眼、EoW/BoC、荒漠灾虫、菌生蟹 |
| `Phase2Bonus` | int | 5 | 0–50 | 蜂王、骷髅王、独眼巨鹿、血肉墙、腐巢意志/血肉宿主、史莱姆之神 |
| `Phase3Bonus` | int | 8 | 0–50 | 史莱姆皇后、三王、渊海灾虫、硫磺火元素、极地之灵 |
| `Phase4Bonus` | int | 15 | 0–100 | 世纪之花、灾厄之影、利维坦/阿娜希塔、白金星舰 |
| `Phase5Bonus` | int | 25 | 0–100 | 石巨人、猪鲨、光之女皇、拜月教徒、月总、瘟疫使者、毁灭魔像、星神游龙 |
| `Phase6Bonus` | int | 50 | 0–200 | 亵渎守卫、痴愚金龙、亵渎天神 |
| `Phase7Bonus` | int | 100 | 0–300 | 风暴编织者、无尽虚空、西格纳斯、噬魂幽花、硫海遗爵 |
| `Phase8Bonus` | int | 200 | 0–500 | 神明吞噬者、犽戎 |
| `Phase9Bonus` | int | 400 | 0–1000 | 星流巨械（4体）、至尊灾厄、始源妖龙 |
| `UltraTerraprismaDamageMultiplier` | float | 1.3 | 1.0–3.0 | uprisma 伤害 = gtprisma 伤害 × 此值 |

Boss 层级判定：`GrowableTerraprismaPlayer.GetBossBonus()` 按 `IsPhase1Boss` ~ `IsPhase9Boss` 逐级判定，灾厄 NPC 类型通过 `CalResolve` 懒加载一次后缓存。未匹配的普通 Boss 归入阶段一。配置变更即时生效，无需重载 mod。

---

## 10. 实施阶段

| 阶段 | 范围                                                      | 涉及文件                                    |
|----- |---------------------------------------------------------- |------------------------------------------- |
| 1    | gtprisma 物品（初始物品，原版弹幕）+ ModPlayer               | GrowableTerraprismaItem.cs, GrowableTerraprismaPlayer.cs |
| 2    | 击杀计数 + Boss 击败追踪（GlobalProjectile + GlobalNPC）    | GrowableTerraprismaGlobalProjectile.cs, GrowableTerraprismaGlobalNPC.cs |
| 3    | 数值成长（伤害缩放）+ tooltip                              | GrowableTerraprismaItem.cs, GrowableTerraprismaPlayer.cs |
| 4    | gtprisma 自定义弹幕移植（ModProjectile 完整 AI + 渲染）     | GrowableTerraprismaProjectile.cs, GrowableTerraprismaBuff.cs（重构） |
| 5    | gtprisma 功能性成长（8 Boss 被动能力）                      | GrowableTerraprismaPlayer.cs, GrowableTerraprismaProjectile.cs |
| 6    | uprisma 物品 + 合成配方 + 自定义弹幕                        | UltraTerraprismaItem.cs, UltraTerraprismaProjectile.cs, Recipes/ |
| 7    | 行为接口 + 注册表 + 原版 Boss 行为                          | IUprismaBehavior.cs, UprismaBehaviorRegistry.cs, Behaviors/Vanilla/ |
| 8    | 灾厄兼容 + 灾厄关键 Boss 行为（8 个）                       | Behaviors/Calamity/, CalamityBossTypeCache.cs |
| 9    | 视觉润色 + 本地化 + 配置文件 + Mod.Call 集成                | 全部文件                                    |

---

## 11. 已确认决策

| # | 问题 | 决策 |
|---|------|------|
| 1 | **数值机制** | 纯 Boss 基础伤害加成，直接叠加到 `Item.damage`。无击杀点数、无倍率公式、无迷你 Boss 追踪，保持简单可控。 |
| 2 | **gtprisma 消耗** | 合成 uprisma 时**保留** gtprisma，仅消耗原版泰拉棱镜。 |
| 3 | **重铸** | 支持。`Item.damage` 保持原值，Boss 加成通过 `ModifyWeaponDamage.Base` 追加，不破坏前缀。 |
| 4 | **多人模式** | 确保兼容，击杀追踪走持有者自身即可。 |
| 5 | **灾厄行为数量** | 仅 8 个关键剧情 Boss 解锁行为（硫火之灵→至尊灾厄），其余仅贡献数值成长。 |

