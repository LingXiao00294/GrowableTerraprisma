using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Common.Balance;
using GrowableTerraprisma.Content.Projectiles;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors.Vanilla
{
    /// <summary>
    /// 利刃台风 — 击败猪龙鱼公爵后解锁。周期性向目标发射小型台风弹幕。
    /// </summary>
    public class RazorTyphoonBehavior : IUprismaBehavior
    {
        public string Name => "Mods.GrowableTerraprisma.Behaviors.RazorTyphoon";
        public string Description => "Mods.GrowableTerraprisma.Behaviors.RazorTyphoonDescription";

        public bool IsUnlocked(GrowableTerraprismaPlayer player)
            => player.defeatedBossTypes.Contains(NPCID.DukeFishron);

        public bool CanRun(Projectile proj)
        {
            // 仅首个召唤物发射台风，且处于攻击状态
            if (proj.ai[0] <= 0f)
                return false;

            // 检查是否为同类型弹幕中 identity 最小的
            int myIdx = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == proj.owner && p.type == proj.type && proj.whoAmI > i)
                    myIdx++;
            }
            return myIdx == 0;
        }

        public void AI(Projectile proj)
        {
            if (proj.ModProjectile is not UltraTerraprismaProjectile ultra)
                return;

            ultra.RazorTyphoonCooldown--;
            if (ultra.RazorTyphoonCooldown > 0)
                return;

            ultra.RazorTyphoonCooldown = GrowthBalance.RazorTyphoonInterval;

            // 寻找攻击目标
            NPC target = null;
            float bestDist = 800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(proj) && npc.Distance(proj.Center) < bestDist)
                {
                    bestDist = npc.Distance(proj.Center);
                    target = npc;
                }
            }

            if (target != null && proj.owner == Main.myPlayer)
            {
                Vector2 vel = proj.Center.DirectionTo(target.Center) * 14f;
                int typhoonDmg = (int)(proj.damage * GrowthBalance.RazorTyphoonDamageScale);
                var typhoon = Projectile.NewProjectileDirect(
                    proj.GetSource_FromThis(), proj.Center, vel,
                    ProjectileID.Typhoon, typhoonDmg, 2f, proj.owner);
                typhoon.originalDamage = typhoonDmg;
                typhoon.DamageType = DamageClass.Summon;
            }
        }
    }
}
