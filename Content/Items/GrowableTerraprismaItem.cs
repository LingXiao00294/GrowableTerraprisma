using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using GrowableTerraprisma.Common.Configs;
using GrowableTerraprisma.Content.Buffs;
using GrowableTerraprisma.Content.Projectiles;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Content.Items
{
    public class GrowableTerraprismaItem : ModItem
    {
        private const int VanillaBaseDamage = 15;

        public override void SetDefaults()
        {
            Item.damage = VanillaBaseDamage;
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
            Item.shoot = ModContent.ProjectileType<GrowableTerraprismaProjectile>();
            Item.shootSpeed = 10f;
            Item.buffType = ModContent.BuffType<GrowableTerraprismaBuff>();
            Item.autoReuse = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var cfg = ModContent.GetInstance<GrowableTerraprismaConfig>();
            var growable = player.GetModPlayer<GrowableTerraprismaPlayer>();
            damage.Base += cfg.BaseDamage - VanillaBaseDamage + growable.BossesBaseBonus;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 2);
            var proj = Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
            proj.originalDamage = damage;
            return false;
        }

        public override bool ConsumeItem(Player player) => false;

        public override bool CanRightClick()
        {
            return Main.keyState.PressingShift();
        }

        public override void RightClick(Player player)
        {
            bool newValue = true;
            foreach (var p in Main.ActiveProjectiles)
            {
                if (p.type == Item.shoot && p.owner == player.whoAmI &&
                    p.ModProjectile is GrowableTerraprismaProjectile modProj)
                {
                    newValue = !modProj.FocusOnFetching;
                    break;
                }
            }
            foreach (var p in Main.ActiveProjectiles)
            {
                if (p.type == Item.shoot && p.owner == player.whoAmI &&
                    p.ModProjectile is GrowableTerraprismaProjectile modProj)
                {
                    modProj.FocusOnFetching = newValue;
                    p.netUpdate = true;
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var growable = Main.LocalPlayer.GetModPlayer<GrowableTerraprismaPlayer>();
            int bossBonus = growable.BossesBaseBonus;

            if (bossBonus <= 0)
                return;

            // 用 GetWeaponDamage 获取含 ModifyWeaponDamage 的真实面板值
            int realDamage = (int)Main.LocalPlayer.GetWeaponDamage(Item);
            string className = Item.DamageType.DisplayName.Value;

            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage")
                {
                    tooltips[i].Text = $"{realDamage} {className}";
                    break;
                }
            }

            tooltips.Add(new TooltipLine(Mod, "BossBonus",
                Language.GetTextValue("Mods.GrowableTerraprisma.Items.GrowableTerraprismaItem.BossBonus",
                    growable.defeatedBossTypes.Count, bossBonus)));
        }
    }
}
