using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace GrowableTerraprisma.Common.Configs
{
    public class GrowableTerraprismaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("BaseStats")]
        [DefaultValue(10)]
        [Range(1, 100)]
        [Slider]
        public int BaseDamage;

        [Header("GrowthCurve")]
        [DefaultValue(0.35f)]
        [Range(0.1f, 0.75f)]
        [Increment(0.05f)]
        [Slider]
        public float BossGrowthRate;

        [Header("PhaseCaps")]
        [DefaultValue(10)]
        [Range(0, 100)]
        [Slider]
        public int Phase1Cap;

        [DefaultValue(15)]
        [Range(0, 150)]
        [Slider]
        public int Phase2Cap;

        [DefaultValue(24)]
        [Range(0, 200)]
        [Slider]
        public int Phase3Cap;

        [DefaultValue(30)]
        [Range(0, 250)]
        [Slider]
        public int Phase4Cap;

        [DefaultValue(45)]
        [Range(0, 300)]
        [Slider]
        public int Phase5Cap;

        [DefaultValue(70)]
        [Range(0, 400)]
        [Slider]
        public int Phase6Cap;

        [DefaultValue(100)]
        [Range(0, 500)]
        [Slider]
        public int Phase7Cap;

        [DefaultValue(150)]
        [Range(0, 750)]
        [Slider]
        public int Phase8Cap;

        [DefaultValue(200)]
        [Range(0, 1000)]
        [Slider]
        public int Phase9Cap;

        [Header("UltraTerraprisma")]
        [DefaultValue(1.15f)]
        [Range(1f, 2f)]
        [Increment(0.05f)]
        [Slider]
        public float UltraTerraprismaDamageMultiplier;
    }
}
