using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace GrowableTerraprisma.Systems
{
    public class ItemFetchLockSystem : ModSystem
    {
        private static readonly Dictionary<int, int> _lockedItems = new();
        private static readonly Dictionary<int, long> _cooldownItems = new();

        public static bool TryLockItem(int itemWhoAmI, int projWhoAmI)
        {
            if (_lockedItems.TryGetValue(itemWhoAmI, out int owner))
                return owner == projWhoAmI;

            _lockedItems[itemWhoAmI] = projWhoAmI;
            return true;
        }

        public static void UnlockItem(int itemWhoAmI, int projWhoAmI)
        {
            if (_lockedItems.TryGetValue(itemWhoAmI, out int owner) && owner == projWhoAmI)
                _lockedItems.Remove(itemWhoAmI);
        }

        public static bool IsItemLocked(int itemWhoAmI)
        {
            return _lockedItems.ContainsKey(itemWhoAmI);
        }

        public static bool IsItemOnCooldown(int itemWhoAmI)
        {
            if (!_cooldownItems.TryGetValue(itemWhoAmI, out long expireTick))
                return false;
            if (Main.GameUpdateCount > (ulong)expireTick)
            {
                _cooldownItems.Remove(itemWhoAmI);
                return false;
            }
            return true;
        }

        public static void SetCooldown(int itemWhoAmI, int frames)
        {
            _cooldownItems[itemWhoAmI] = (long)(Main.GameUpdateCount + (ulong)frames);
        }

        public override void OnWorldUnload()
        {
            _lockedItems.Clear();
            _cooldownItems.Clear();
        }

        public override void PostUpdateInput()
        {
            if (Main.GameUpdateCount % 120 != 0)
                return;

            List<int> toRemove = new();
            foreach (var (itemIdx, projIdx) in _lockedItems)
            {
                if (itemIdx >= Main.maxItems || projIdx >= Main.maxProjectiles ||
                    !Main.item[itemIdx].active || !Main.projectile[projIdx].active)
                    toRemove.Add(itemIdx);
            }
            foreach (var key in toRemove)
                _lockedItems.Remove(key);

            foreach (var (itemIdx, expireTick) in _cooldownItems)
            {
                if (Main.GameUpdateCount > (ulong)expireTick)
                    toRemove.Add(itemIdx);
            }
            foreach (var key in toRemove)
                _cooldownItems.Remove(key);
            toRemove.Clear();
        }
    }
}
