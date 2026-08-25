using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Content.Items;

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
                .Register();
        }
    }
}
