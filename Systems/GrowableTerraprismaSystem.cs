using Terraria.ModLoader;
using GrowableTerraprisma.Behaviors;
using GrowableTerraprisma.Behaviors.Vanilla;
using GrowableTerraprisma.Content.Buffs;
using GrowableTerraprisma.Content.Items;
using GrowableTerraprisma.Content.Projectiles;

namespace GrowableTerraprisma.Systems
{
    public class GrowableTerraprismaSystem : ModSystem
    {
        public override void OnModLoad() => RegisterBehaviors();

        public override void OnModUnload() => UprismaBehaviorRegistry.Clear();

        public override void PostSetupContent() => RegisterSummonersAssociation();

        private static void RegisterBehaviors()
        {
            UprismaBehaviorRegistry.Clear();

            UprismaBehaviorRegistry.Register(new SwiftBladesBehavior());
            UprismaBehaviorRegistry.Register(new StarAegisBehavior());
            UprismaBehaviorRegistry.Register(new RazorTyphoonBehavior());
            UprismaBehaviorRegistry.Register(new EtherealDanceBehavior());
            UprismaBehaviorRegistry.Register(new DragonsFuryBehavior());
        }

        private static void RegisterSummonersAssociation()
        {
            if (!ModLoader.TryGetMod("SummonersAssociation", out Mod sa))
                return;

            sa.Call("AddMinionInfo",
                ModContent.ItemType<GrowableTerraprismaItem>(),
                ModContent.BuffType<GrowableTerraprismaBuff>(),
                ModContent.ProjectileType<GrowableTerraprismaProjectile>());

            sa.Call("AddMinionInfo",
                ModContent.ItemType<UltraTerraprismaItem>(),
                ModContent.BuffType<UltraTerraprismaBuff>(),
                ModContent.ProjectileType<UltraTerraprismaProjectile>());

            sa.Call("AddPersistentBuff", ModContent.BuffType<GrowableTerraprismaBuff>());
            sa.Call("AddPersistentBuff", ModContent.BuffType<UltraTerraprismaBuff>());
        }
    }
}
