using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Global
{
    /// <summary>
    /// 为 gtprisma 生成的 EmpressBlade 弹幕提供生命周期管理。
    /// 复刻原版模式：player.empressBlade → timeLeft = 2，
    /// 替换为 gtPlayer.gtprismaMinionActive → timeLeft = 2。
    /// 仅处理 localAI[2] == 1f 的弹幕（gtprisma 标记），不影响原版泰拉棱镜。
    /// </summary>
    public class GrowableTerraprismaGlobalProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == ProjectileID.EmpressBlade;
        }

        public override void PostAI(Projectile projectile)
        {
            if (projectile.localAI[2] != 1f)
                return;

            var gtPlayer = Main.player[projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();
            if (gtPlayer.gtprismaMinionActive)
            {
                projectile.timeLeft = 2;
            }
        }
    }
}
