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
            int bossType = GetBossType(npc);
            if (bossType < 0)
                return;

            bool isBoss = npc.boss || IsBossBody(npc);
            if (!isBoss && !IsMiniBoss(bossType))
                return;

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

                var gtPlayer = player.GetModPlayer<GrowableTerraprismaPlayer>();
                if (isBoss)
                    gtPlayer.defeatedBossTypes.Add(bossType);
                else
                    gtPlayer.defeatedMiniBossTypes.Add(bossType);
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

        private static bool IsBossBody(NPC npc)
        {
            if (npc.realLife < 0 || npc.realLife >= Main.maxNPCs)
                return false;
            NPC main = Main.npc[npc.realLife];
            return main.active && main.boss;
        }

        private static bool IsMiniBoss(int npcType)
        {
            return npcType == NPCID.IceGolem
                || npcType == NPCID.SandElemental
                || npcType == NPCID.BloodNautilus
                || npcType == NPCID.PirateShip
                || npcType == NPCID.Pumpking
                || npcType == NPCID.IceQueen
                || npcType == NPCID.MartianSaucerCore;
        }
    }
}
