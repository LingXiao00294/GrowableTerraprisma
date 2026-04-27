using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Global
{
    public class GrowableTerraprismaGlobalProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == ProjectileID.EmpressBlade;
        }

        public override void PostAI(Projectile projectile)
        {
            if (projectile.localAI[2] != 1f)
                return;

            var gtPlayer = Main.player[projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();
            if (gtPlayer.gtprismaMinionActive)
            {
                projectile.timeLeft = 2;
            }
            else
            {
                // 原版 empressBlade 可能已将 timeLeft 设为 2，直接杀死以覆盖
                projectile.Kill();
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.localAI[2] != 1f)
                return;
            if (target.life > 0)
                return;
            if (target.friendly)
                return;

            int owner = projectile.owner;
            if (owner < 0 || owner >= Main.maxPlayers)
                return;

            var gtPlayer = Main.player[owner].GetModPlayer<GrowableTerraprismaPlayer>();
            gtPlayer.minionKillCounter++;
        }
    }
}
