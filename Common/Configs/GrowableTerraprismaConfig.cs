using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace GrowableTerraprisma.Common.Configs
{
    public class GrowableTerraprismaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("BaseStats")]
        [DefaultValue(15)]
        [Range(1, 100)]
        [Slider]
        public int BaseDamage;

        [Header("Phase1")]
        [DefaultValue(3)]
        [Range(0, 50)]
        [Slider]
        public int Phase1Bonus;

        [Header("Phase2")]
        [DefaultValue(5)]
        [Range(0, 50)]
        [Slider]
        public int Phase2Bonus;

        [Header("Phase3")]
        [DefaultValue(8)]
        [Range(0, 50)]
        [Slider]
        public int Phase3Bonus;

        [Header("Phase4")]
        [DefaultValue(15)]
        [Range(0, 100)]
        [Slider]
        public int Phase4Bonus;

        [Header("Phase5")]
        [DefaultValue(25)]
        [Range(0, 100)]
        [Slider]
        public int Phase5Bonus;

        [Header("Phase6")]
        [DefaultValue(50)]
        [Range(0, 200)]
        [Slider]
        public int Phase6Bonus;

        [Header("Phase7")]
        [DefaultValue(100)]
        [Range(0, 300)]
        [Slider]
        public int Phase7Bonus;

        [Header("Phase8")]
        [DefaultValue(200)]
        [Range(0, 500)]
        [Slider]
        public int Phase8Bonus;

        [Header("Phase9")]
        [DefaultValue(400)]
        [Range(0, 1000)]
        [Slider]
        public int Phase9Bonus;

        [Header("UltraTerraprisma")]
        [DefaultValue(1.3f)]
        [Range(1f, 3f)]
        [Increment(0.05f)]
        [Slider]
        public float UltraTerraprismaDamageMultiplier;
    }
}
