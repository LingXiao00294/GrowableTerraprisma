using System.Collections.Generic;
using System.Linq;
using GrowableTerraprisma.Players;

namespace GrowableTerraprisma.Behaviors
{
    /// <summary>
    /// 究极泰拉棱镜行为注册表 — 静态列表，OnModLoad 注册内置行为，Mod.Call 支持外部注册。
    /// </summary>
    public static class UprismaBehaviorRegistry
    {
        private static readonly List<IUprismaBehavior> _behaviors = new();

        public static IReadOnlyList<IUprismaBehavior> Behaviors => _behaviors;

        public static void Register(IUprismaBehavior behavior) => _behaviors.Add(behavior);

        public static IEnumerable<IUprismaBehavior> GetUnlocked(GrowableTerraprismaPlayer player)
            => _behaviors.Where(b => b.IsUnlocked(player));

        internal static void Clear() => _behaviors.Clear();
    }
}