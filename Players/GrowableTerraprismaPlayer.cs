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

        // 由 buff 存在性计算，复刻原版 empressBlade 模式
        public bool gtprismaMinionActive =>
            Player.HasBuff(ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>());

        public int UniqueBossesDefeated => defeatedBossTypes.Count;
        public int UniqueMiniBossesDefeated => defeatedMiniBossTypes.Count;
        public float GrowthPoints => minionKillCounter + (UniqueBossesDefeated + UniqueMiniBossesDefeated) * 25;
        public float DamageMultiplier => 1f + MathF.Log(1 + GrowthPoints * 0.004f) * 0.5f;

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
        }

        // 清理因 gtp 弹幕（同为 type 946）而残留的原版 EmpressBlade buff
        public override void PostUpdate()
        {
            if (Player.HasBuff(BuffID.EmpressBlade))
            {
                bool hasUnmarked = false;
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.owner == Player.whoAmI
                        && proj.type == ProjectileID.EmpressBlade
                        && proj.localAI[2] != 1f
                        && proj.active
                        && proj.minion)
                    {
                        hasUnmarked = true;
                        break;
                    }
                }
                if (!hasUnmarked)
                {
                    Player.ClearBuff(BuffID.EmpressBlade);
                }
            }
        }

        // 手动统计 gtp 弹幕，避免 ownedProjectileCounts[946] 混入原版弹幕
        public override void PostUpdateBuffs()
        {
            int buffType = ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>();
            if (!Player.HasBuff(buffType))
                return;

            bool hasGtpProj = false;
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.owner == Player.whoAmI
                    && proj.type == ProjectileID.EmpressBlade
                    && proj.localAI[2] == 1f
                    && proj.active)
                {
                    hasGtpProj = true;
                    break;
                }
            }

            if (hasGtpProj)
            {
                Player.buffTime[Player.FindBuffIndex(buffType)] = 18000;
            }
            else
            {
                Player.DelBuff(Player.FindBuffIndex(buffType));
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
