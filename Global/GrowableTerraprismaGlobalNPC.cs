using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Global
{
    public class GrowableTerraprismaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnKill(NPC npc)
        {
            // 追踪 Boss 击败
            if (npc.boss)
            {
                TrackDefeat(npc);
            }

            // 追踪特殊非 Boss NPC（双足翼龙 Betsy 无 npc.boss 标记但应被追踪）
            if (npc.type == NPCID.DD2Betsy)
            {
                TrackDefeat(npc);
            }
        }

        private static void TrackDefeat(NPC npc)
        {
            int bossType = GetBossType(npc);

            int gtBuff = ModContent.BuffType<Content.Buffs.GrowableTerraprismaBuff>();
            int ultraBuff = ModContent.BuffType<Content.Buffs.UltraTerraprismaBuff>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active)
                    continue;
                if (!player.HasBuff(gtBuff) && !player.HasBuff(ultraBuff))
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
