using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Content.Projectiles;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors.Vanilla
{
    /// <summary>
    /// 以太之舞 — 击败光之女皇后解锁。召唤物留下造成伤害的残影轨迹。
    /// </summary>
    public class EtherealDanceBehavior : IUprismaBehavior
    {
        private const int TrailInterval = 18; // 每 18 帧生成一次残影

        public string Name => "Mods.GrowableTerraprisma.Behaviors.EtherealDance";
        public string Description => "Mods.GrowableTerraprisma.Behaviors.EtherealDanceDescription";

        public bool IsUnlocked(GrowableTerraprismaPlayer player)
            => player.defeatedBossTypes.Contains(NPCID.HallowBoss);

        public bool CanRun(Projectile proj)
        {
            // 仅攻击状态生成残影
            return proj.ai[0] > 0f;
        }

        public void AI(Projectile proj)
        {
            if (proj.ModProjectile is not UltraTerraprismaProjectile ultra)
                return;

            ultra.EtherealDanceCooldown--;
            if (ultra.EtherealDanceCooldown > 0)
                return;

            ultra.EtherealDanceCooldown = TrailInterval;

            if (proj.owner == Main.myPlayer)
            {
                int trailDmg = (int)(proj.damage * 0.2f);
                var trail = Projectile.NewProjectileDirect(
                    proj.GetSource_FromThis(), proj.Center, Vector2.Zero,
                    ModContent.ProjectileType<EtherealDanceTrail>(), trailDmg, 0f, proj.owner);
                trail.originalDamage = trailDmg;
                trail.DamageType = DamageClass.Summon;
                trail.rotation = proj.rotation;
            }
        }
    }
}
