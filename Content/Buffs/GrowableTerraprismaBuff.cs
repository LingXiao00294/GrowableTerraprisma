using Terraria;
using Terraria.ModLoader;

namespace GrowableTerraprisma.Content.Buffs
{
    public class GrowableTerraprismaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
