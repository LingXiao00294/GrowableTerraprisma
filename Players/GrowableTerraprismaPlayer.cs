using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GrowableTerraprisma.Players
{
    public class GrowableTerraprismaPlayer : ModPlayer
    {
        public int minionKillCounter;
        public HashSet<int> defeatedBossTypes = new();
        public HashSet<int> defeatedMiniBossTypes = new();

        // 复刻原版 empressBlade 模式：buff ↔ 弹幕 双向往返
        public bool gtprismaMinionActive;

        public bool gtprismaMinionActive => Player.HasBuff(ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>());

        public int UniqueBossesDefeated => defeatedBossTypes.Count;
        public int UniqueMiniBossesDefeated => defeatedMiniBossTypes.Count;
        public float GrowthPoints => minionKillCounter + (UniqueBossesDefeated + UniqueMiniBossesDefeated) * 25;
        public float DamageMultiplier => 1f + MathF.Log(1 + GrowthPoints * 0.005f) * 0.5f;

        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            if (!mediumCoreDeath)
            {
                return new[] { new Item(ModContent.ItemType<Content.Items.GrowableTerraprismaItem>()) };
            }
            return Array.Empty<Item>();
        }

        public override void ResetEffects()
        {
            gtprismaMinionActive = false;
        }

        // 复刻原版 UpdateBuffs 中 buffType == 322 分支的模式：
        //   ownedProjectileCounts[946] > 0 → empressBlade = true, buffTime = 18000
        public override void PostUpdateBuffs()
        {
            int buffType = ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>();
            if (!Player.HasBuff(buffType))
                return;

            if (Player.ownedProjectileCounts[ProjectileID.EmpressBlade] > 0)
            {
                gtprismaMinionActive = true;
                Player.buffTime[Player.FindBuffIndex(buffType)] = 18000;
            }
        }

        public override void Initialize()
        {
            minionKillCounter = 0;
            defeatedBossTypes.Clear();
            defeatedMiniBossTypes.Clear();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["kills"] = minionKillCounter;
            tag["bosses"] = defeatedBossTypes.ToList();
            tag["miniBosses"] = defeatedMiniBossTypes.ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            minionKillCounter = tag.GetInt("kills");
            defeatedBossTypes = tag.GetList<int>("bosses")?.ToHashSet() ?? new HashSet<int>();
            defeatedMiniBossTypes = tag.GetList<int>("miniBosses")?.ToHashSet() ?? new HashSet<int>();
        }
    }
}
