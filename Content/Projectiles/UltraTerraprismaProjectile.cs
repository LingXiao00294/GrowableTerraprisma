using Terraria;
using Terraria.ModLoader;
using GrowableTerraprisma.Behaviors;
using GrowableTerraprisma.Content.Buffs;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Content.Projectiles
{
    /// <summary>
    /// 究极泰拉棱镜召唤物 — 继承 gtprisma 完整 AI_156 状态机，仅叠加 uprisma 行为层。
    /// </summary>
    public class UltraTerraprismaProjectile : GrowableTerraprismaProjectile
    {
        internal int EtherealDanceCooldown;
        internal int RazorTyphoonCooldown;

        public override void SetStaticDefaults() => base.SetStaticDefaults();

        public override void SetDefaults() => base.SetDefaults();

        protected override int MinionBuffType => ModContent.BuffType<UltraTerraprismaBuff>();

        protected override bool ShouldKeepAlive(GrowableTerraprismaPlayer prismaPlayer) =>
            prismaPlayer.ultraMinionActive;

        protected override void OnOwnerDead(GrowableTerraprismaPlayer prismaPlayer) =>
            prismaPlayer.ultraMinionActive = false;

        protected override void AfterThink(Player player)
        {
            var ultra = player.GetModPlayer<GrowableTerraprismaPlayer>();
            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(ultra))
            {
                if (behavior.CanRun(Projectile))
                    behavior.AI(Projectile);
            }
        }

        protected override void OnBeforeDraw(Player player)
        {
            var ultra = player.GetModPlayer<GrowableTerraprismaPlayer>();
            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(ultra))
                behavior.OnPreDraw(Projectile);
        }

        protected override void OnAfterDraw(Player player)
        {
            var ultra = player.GetModPlayer<GrowableTerraprismaPlayer>();
            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(ultra))
                behavior.OnPostDraw(Projectile);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnPassiveHitNPC(target, hit, damageDone);

            var ultra = Main.player[Projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();
            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(ultra))
                behavior.OnHitNPC(Projectile, target, hit, damageDone);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            OnPassiveModifyHitNPC(target, ref modifiers);

            var ultra = Main.player[Projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();
            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(ultra))
                behavior.ModifyHitNPC(Projectile, target, ref modifiers);
        }
    }
}
