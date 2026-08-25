using Terraria;
using Terraria.ID;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors.Vanilla
{
    /// <summary>
    /// 加速之刃 — 始终激活。玩家移动速度 +15%。
    /// </summary>
    public class SwiftBladesBehavior : IUprismaBehavior
    {
        public string Name => "Mods.GrowableTerraprisma.Behaviors.SwiftBlades";
        public string Description => "Mods.GrowableTerraprisma.Behaviors.SwiftBladesDescription";

        // 始终激活，无解锁门槛
        public bool IsUnlocked(GrowableTerraprismaPlayer player) => true;

        public bool CanRun(Projectile proj) => false; // 纯持有者增益，无弹幕 AI

        public void AI(Projectile proj) { } // 不需要弹幕 AI

        public void UpdatePlayer(Player player)
        {
            player.moveSpeed += 0.15f;
        }
    }
}