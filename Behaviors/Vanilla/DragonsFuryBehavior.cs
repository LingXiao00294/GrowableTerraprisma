using Terraria;
using Terraria.ID;
using GrowableTerraprisma.Common.Balance;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors.Vanilla
{
    /// <summary>
    /// 龙之怒 — 击败双足翼龙后解锁。目标生命值低于 50% 时 +8% 伤害。
    /// </summary>
    public class DragonsFuryBehavior : IUprismaBehavior
    {
        public string Name => "Mods.GrowableTerraprisma.Behaviors.DragonsFury";
        public string Description => "Mods.GrowableTerraprisma.Behaviors.DragonsFuryDescription";

        public bool IsUnlocked(GrowableTerraprismaPlayer player)
            => player.defeatedBossTypes.Contains(NPCID.DD2Betsy);

        public bool CanRun(Projectile proj) => false; // 纯 ModifyHitNPC，无弹幕 AI

        public void AI(Projectile proj) { }

        public void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            float hpRatio = (float)target.life / target.lifeMax;
            if (hpRatio < 0.5f)
            {
                modifiers.FinalDamage *= GrowthBalance.DragonsFuryDamageMultiplier;
            }
        }
    }
}
