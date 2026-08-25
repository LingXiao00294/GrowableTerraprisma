using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using GrowableTerraprisma.Behaviors;
using GrowableTerraprisma.Content.Projectiles;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Content.Buffs
{
    public class UltraTerraprismaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int projType = ModContent.ProjectileType<UltraTerraprismaProjectile>();
            var modPlayer = player.GetModPlayer<GrowableTerraprismaPlayer>();

            if (HasActiveMinion(player, projType))
                modPlayer.ultraMinionActive = true;

            if (!modPlayer.ultraMinionActive)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = 18000;

            var defeated = modPlayer.defeatedBossTypes;

            if (defeated.Contains(NPCID.KingSlime))
                Lighting.AddLight(player.Center, 0.6f, 0.5f, 0.9f);

            if (defeated.Contains(GrowableTerraprismaPlayer.Cal.SlimeGodCore))
                player.maxMinions += 1;

            if (defeated.Contains(NPCID.QueenSlimeBoss))
                player.moveSpeed += 0.1f;

            if (defeated.Contains(NPCID.Plantera))
                player.lifeRegen += 5;

            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(modPlayer))
                behavior.UpdatePlayer(player);
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var growable = Main.LocalPlayer.GetModPlayer<GrowableTerraprismaPlayer>();
            var defeated = growable.defeatedBossTypes;

            if (defeated.Contains(NPCID.QueenBee))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveQueenBee");
            if (defeated.Contains(GrowableTerraprismaPlayer.Cal.SlimeGodCore))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveSlimeGod");
            if (defeated.Contains(NPCID.WallofFlesh))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveWoF");
            if (defeated.Contains(NPCID.QueenSlimeBoss))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveQueenSlime");
            if (defeated.Contains(NPCID.TheDestroyer))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveDestroyer");
            if (defeated.Contains(NPCID.Retinazer) && defeated.Contains(NPCID.Spazmatism))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassiveTwins");
            if (defeated.Contains(NPCID.SkeletronPrime))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassivePrime");
            if (defeated.Contains(NPCID.Plantera))
                tip += "\n" + Language.GetTextValue("Mods.GrowableTerraprisma.Buffs.GrowableTerraprismaBuff.PassivePlantera");

            foreach (var behavior in UprismaBehaviorRegistry.GetUnlocked(growable))
            {
                tip += "\n" + Language.GetTextValue(behavior.Name) + "："
                     + Language.GetTextValue(behavior.Description);
            }
        }

        private static bool HasActiveMinion(Player player, int projType)
        {
            if (player.ownedProjectileCounts[projType] > 0)
                return true;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == projType)
                    return true;
            }

            return false;
        }
    }
}
