using Terraria;
using Terraria.ModLoader;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Global
{
    public class GrowableTerraprismaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnKill(NPC npc)
        {
            if (!npc.boss)
                return;

            int bossType = GetBossType(npc);

            int buffType = ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active)
                    continue;
                if (!player.HasBuff(buffType))
                    continue;
                if (!npc.playerInteraction[i])
                    continue;

                var growable = player.GetModPlayer<GrowableTerraprismaPlayer>();
                growable.defeatedBossTypes.Add(bossType);
            }
        }

        private static int GetBossType(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            {
                NPC main = Main.npc[npc.realLife];
                if (main.active)
                    return main.type;
            }
            return npc.type;
        }
    }
}
