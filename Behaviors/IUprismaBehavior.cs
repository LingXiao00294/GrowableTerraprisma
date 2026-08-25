using Terraria;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors
{
    /// <summary>
    /// 究极泰拉棱镜行为接口 — 击败特定 Boss 后解锁的叠加层行为。
    /// 行为在原版 EmpressBlade AI 之后执行，不替换原版逻辑。
    /// </summary>
    public interface IUprismaBehavior
    {
        /// <summary>行为名称（本地化键或原始名称）。</summary>
        string Name { get; }

        /// <summary>行为描述（本地化键或原始描述）。</summary>
        string Description { get; }

        /// <summary>给定玩家是否已解锁此行为。</summary>
        bool IsUnlocked(GrowableTerraprismaPlayer player);

        /// <summary>每帧判断此行为是否执行。返回 false 跳过本帧。</summary>
        bool CanRun(Projectile proj);

        /// <summary>每帧 AI 逻辑，在原版 BatOfLight AI 之后执行。</summary>
        void AI(Projectile proj);

        /// <summary>持有者增益，在 Buff.Update 中调用。用于发光、移速、栏位等。</summary>
        void UpdatePlayer(Player player) { }

        /// <summary>在弹幕命中 NPC 时调用。</summary>
        void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) { }

        /// <summary>在弹幕命中 NPC 前，修改命中参数。</summary>
        void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) { }

        /// <summary>在原版 EmpressBlade 渲染前调用（拖尾之前）。</summary>
        void OnPreDraw(Projectile proj) { }

        /// <summary>在原版 EmpressBlade 渲染后调用（精灵之后）。</summary>
        void OnPostDraw(Projectile proj) { }
    }
}