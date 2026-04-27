using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Content.Buffs;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Content.Items
{
    public class GrowableTerraprismaItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 54;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.width = 26;
            Item.height = 28;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(0, 20);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item82;
            Item.shoot = ProjectileID.EmpressBlade;
            Item.shootSpeed = 10f;
            Item.buffType = ModContent.BuffType<GrowableTerraprismaBuff>();
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var gtPlayer = player.GetModPlayer<GrowableTerraprismaPlayer>();
            int scaledDamage = (int)(damage * gtPlayer.DamageMultiplier);

            player.AddBuff(Item.buffType, 2);
            var proj = Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero, type, scaledDamage, knockback, player.whoAmI);
            proj.originalDamage = scaledDamage;
            proj.localAI[2] = 1f;  // gtprisma 标记，供 GlobalProjectile 识别
            proj.localAI[2] = 1f; // 标记此弹幕由 gtprisma 生成
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var gtPlayer = Main.LocalPlayer.GetModPlayer<GrowableTerraprismaPlayer>();
            tooltips.Add(new TooltipLine(Mod, "GrowthKills", $"召唤物击杀: {gtPlayer.minionKillCounter}"));
            tooltips.Add(new TooltipLine(Mod, "GrowthBosses", $"独特Boss击败: {gtPlayer.UniqueBossesDefeated}"));
            tooltips.Add(new TooltipLine(Mod, "GrowthDamage", $"伤害倍率: {gtPlayer.DamageMultiplier:F2}x"));
        }
    }
}
