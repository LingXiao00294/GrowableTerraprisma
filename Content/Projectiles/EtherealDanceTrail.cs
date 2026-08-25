using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GrowableTerraprisma.Content.Projectiles
{
    /// <summary>
    /// 以太之舞残影 — 以太之舞行为生成的短寿命静止残影，造成接触伤害后消失。
    /// </summary>
    public class EtherealDanceTrail : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.instance.LoadProjectile(ProjectileID.EmpressBlade);
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.penetrate = 1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 36;
            Projectile.scale = 0.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // 逐渐淡出
            float progress = Projectile.timeLeft / 36f;
            Projectile.Opacity = progress * 0.6f;
            Projectile.scale = 0.6f * progress;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float rot = Projectile.rotation - MathHelper.PiOver2;

            float timeFactor = Main.GlobalTimeWrappedHourly % 3f / 3f;
            Color glowColor = Projectile.GetFairyQueenWeaponsColor(0.3f, 0f, timeFactor);
            glowColor *= Projectile.Opacity;

            Main.EntitySpriteDraw(tex, pos, null, glowColor, rot, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}