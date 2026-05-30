using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using GrowableTerraprisma.Content.Items;
using GrowableTerraprisma.Players;


namespace GrowableTerraprisma.Recipes
{
    public class GrowableTerraprismaRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            Recipe.Create(ModContent.ItemType<UltraTerraprismaItem>())
                .AddIngredient(ItemID.EmpressBlade)
                .AddIngredient(ModContent.ItemType<GrowableTerraprismaItem>())
                .AddTile(TileID.MythrilAnvil)
                .AddCondition(
                    Language.GetText("Mods.GrowableTerraprisma.Recipes.UltraTerraprismaCondition"),
                    () =>
                    {
                        var growable = Main.LocalPlayer.GetModPlayer<GrowableTerraprismaPlayer>();
                        return NPC.downedEmpressOfLight && growable.BossesBaseBonus >= 200;
                    })
                .Register();
        }
    }
}
