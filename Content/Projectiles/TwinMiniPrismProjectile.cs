using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GrowableTerraprisma.Content.Projectiles
{
    public class TwinMiniPrismProjectile : ModProjectile
    {
        private const float MaxSpeed = 16f;
        private const float Inertia = 15f;

        public override void SetStaticDefaults()
        {
            Main.instance.LoadProjectile(ProjectileID.EmpressBlade);
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.penetrate = 3;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.5f;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            int idx = (int)Projectile.ai[0];
            NPC target = Main.npc.IndexInRange(idx) ? Main.npc[idx] : null;
            if (target == null || !target.active || target.life <= 0 || !target.CanBeChasedBy(Projectile))
            {
                target = FindTarget(800f);
                if (target != null)
                    Projectile.ai[0] = target.whoAmI;
                else
                    return;
            }

            float speed = (MaxSpeed + Projectile.velocity.Length()) / 2f;
            Vector2 direction = Projectile.Center.DirectionTo(target.Center);
            Projectile.velocity = (Projectile.velocity * (Inertia - 1) + direction * speed) / Inertia;
        }

        private NPC FindTarget(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile))
                {
                    float dist = Projectile.Distance(npc.Center);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = npc;
                    }
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color color = Color.White * Projectile.Opacity;
            float rot = Projectile.rotation - MathHelper.PiOver2;
            Main.EntitySpriteDraw(tex, pos, null, color, rot, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
