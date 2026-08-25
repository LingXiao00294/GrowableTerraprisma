using Terraria;
using Terraria.ID;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors.Vanilla
{
    /// <summary>
    /// 星辰加护 — 击败月亮领主后解锁。最大召唤栏位 +2。
    /// </summary>
    public class StarAegisBehavior : IUprismaBehavior
    {
        public string Name => "Mods.GrowableTerraprisma.Behaviors.StarAegis";
        public string Description => "Mods.GrowableTerraprisma.Behaviors.StarAegisDescription";

        public bool IsUnlocked(GrowableTerraprismaPlayer player)
            => player.defeatedBossTypes.Contains(NPCID.MoonLordCore);

        public bool CanRun(Projectile proj) => false; // 纯持有者增益

        public void AI(Projectile proj) { }

        public void UpdatePlayer(Player player)
        {
            player.maxMinions += 2;
        }
    }
}