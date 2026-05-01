using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using GrowableTerraprisma.Common.Configs;

namespace GrowableTerraprisma.Players
{
    public sealed class GrowableTerraprismaPlayer : ModPlayer
    {
        public HashSet<int> defeatedBossTypes = new();

        public bool growableMinionActive;

        public int miniPrismHitCounter;

        public int BossesBaseBonus
        {
            get
            {
                var cfg = ModContent.GetInstance<GrowableTerraprismaConfig>();
                int sum = 0;
                foreach (int t in defeatedBossTypes)
                    sum += GetBossBonus(t, cfg, defeatedBossTypes);
                return sum;
            }
        }

        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            if (!mediumCoreDeath)
            {
                return new[] { new Item(ModContent.ItemType<Content.Items.GrowableTerraprismaItem>()) };
            }
            return Array.Empty<Item>();
        }

        public override void ResetEffects()
        {
            growableMinionActive = false;
        }

        public override void Initialize()
        {
            defeatedBossTypes.Clear();
            miniPrismHitCounter = 0;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["bosses"] = defeatedBossTypes.ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            defeatedBossTypes = tag.GetList<int>("bosses")?.ToHashSet() ?? new HashSet<int>();
        }

        public static int GetBossBonus(int npcType, GrowableTerraprismaConfig cfg, HashSet<int> defeated)
        {
            // 多体 Boss 群组：必须全部击败，仅代表成员返回加成（其余返回 0 避免加倍）
            if (npcType == NPCID.Retinazer)
                return AllDefeated(defeated, NPCID.Retinazer, NPCID.Spazmatism) ? cfg.Phase3Bonus : 0;
            if (npcType == NPCID.Spazmatism)
                return 0;
            if (CalResolve.Is(npcType, Cal.ProfanedGuardianCommander))
                return AllDefeated(defeated, Cal.ProfanedGuardianCommander, Cal.ProfanedGuardianDefender, Cal.ProfanedGuardianHealer) ? cfg.Phase6Bonus : 0;
            if (CalResolve.Is(npcType, Cal.ProfanedGuardianDefender)
                || CalResolve.Is(npcType, Cal.ProfanedGuardianHealer))
                return 0;
            if (CalResolve.Is(npcType, Cal.Leviathan))
                return AllDefeated(defeated, Cal.Leviathan, Cal.Anahita) ? cfg.Phase4Bonus : 0;
            if (CalResolve.Is(npcType, Cal.Anahita))
                return 0;

            if (IsPhase9Boss(npcType))  return cfg.Phase9Bonus;
            if (IsPhase8Boss(npcType))  return cfg.Phase8Bonus;
            if (IsPhase7Boss(npcType))  return cfg.Phase7Bonus;
            if (IsPhase6Boss(npcType))  return cfg.Phase6Bonus;
            if (IsPhase5Boss(npcType))  return cfg.Phase5Bonus;
            if (IsPhase4Boss(npcType))  return cfg.Phase4Bonus;
            if (IsPhase3Boss(npcType))  return cfg.Phase3Bonus;
            if (IsPhase2Boss(npcType))  return cfg.Phase2Bonus;
            if (IsPhase1Boss(npcType))  return cfg.Phase1Bonus;
            return cfg.Phase1Bonus;
        }

        private static bool AllDefeated(HashSet<int> defeated, params int[] types)
        {
            foreach (int t in types)
                if (!defeated.Contains(t))
                    return false;
            return true;
        }

        #region Phase classification

        // Phase 1: 史莱姆王、荒漠灾虫、克苏鲁之眼、菌生蟹、EoW/BoC
        private static bool IsPhase1Boss(int t) =>
            t == NPCID.KingSlime
            || t == NPCID.EyeofCthulhu
            || t == NPCID.EaterofWorldsHead
            || t == NPCID.BrainofCthulhu
            || CalResolve.Is(t, Cal.DesertScourgeHead)
            || CalResolve.Is(t, Cal.Crabulon);

        // Phase 2: 腐巢意志/血肉宿主、蜂王、骷髅王、独眼巨鹿、史莱姆之神、血肉墙
        private static bool IsPhase2Boss(int t) =>
            t == NPCID.QueenBee
            || t == NPCID.SkeletronHead
            || t == NPCID.Deerclops
            || t == NPCID.WallofFlesh
            || CalResolve.Is(t, Cal.HiveMind)
            || CalResolve.Is(t, Cal.PerforatorHive)
            || CalResolve.Is(t, Cal.SlimeGodCore);

        // Phase 3: 史莱姆皇后、渊海灾虫、硫磺火元素、极地之灵、毁灭者、双子魔眼、机械骷髅王
        private static bool IsPhase3Boss(int t) =>
            t == NPCID.QueenSlimeBoss
            || t == NPCID.TheDestroyer
            || t == NPCID.SkeletronPrime
            || CalResolve.Is(t, Cal.AquaticScourgeHead)
            || CalResolve.Is(t, Cal.BrimstoneElemental)
            || CalResolve.Is(t, Cal.Cryogen);

        // Phase 4: 灾厄之影、世纪之花、利维坦和阿娜希塔、白金星舰
        private static bool IsPhase4Boss(int t) =>
            t == NPCID.Plantera
            || CalResolve.Is(t, Cal.CalamitasClone)
            || CalResolve.Is(t, Cal.Leviathan)
            || CalResolve.Is(t, Cal.Anahita)
            || CalResolve.Is(t, Cal.AstrumAureus);

        // Phase 5: 石巨人、瘟疫使者歌莉娅、猪龙鱼公爵、光之女皇、毁灭魔像、拜月教徒、星神游龙、月亮领主
        private static bool IsPhase5Boss(int t) =>
            t == NPCID.Golem
            || t == NPCID.DukeFishron
            || t == NPCID.HallowBoss
            || t == NPCID.CultistBoss
            || t == NPCID.MoonLordCore
            || CalResolve.Is(t, Cal.PlaguebringerGoliath)
            || CalResolve.Is(t, Cal.RavagerBody)
            || CalResolve.Is(t, Cal.AstrumDeusHead);

        // Phase 6: 亵渎守卫、痴愚金龙、亵渎天神
        private static bool IsPhase6Boss(int t) =>
            CalResolve.Is(t, Cal.ProfanedGuardianCommander)
            || CalResolve.Is(t, Cal.ProfanedGuardianDefender)
            || CalResolve.Is(t, Cal.ProfanedGuardianHealer)
            || CalResolve.Is(t, Cal.Dragonfolly)
            || CalResolve.Is(t, Cal.Providence);

        // Phase 7: 风暴编织者、无尽虚空、西格纳斯、噬魂幽花、硫海遗爵
        private static bool IsPhase7Boss(int t) =>
            CalResolve.Is(t, Cal.StormWeaverHead)
            || CalResolve.Is(t, Cal.CeaselessVoid)
            || CalResolve.Is(t, Cal.Signus)
            || CalResolve.Is(t, Cal.Polterghast)
            || CalResolve.Is(t, Cal.OldDuke);

        // Phase 8: 神明吞噬者、犽戎
        private static bool IsPhase8Boss(int t) =>
            CalResolve.Is(t, Cal.DevourerofGodsHead)
            || CalResolve.Is(t, Cal.Yharon);

        // Phase 9: 星流巨械、至尊灾厄、始源妖龙
        private static bool IsPhase9Boss(int t) =>
            CalResolve.Is(t, Cal.ThanatosHead)
            || CalResolve.Is(t, Cal.AresBody)
            || CalResolve.Is(t, Cal.Apollo)
            || CalResolve.Is(t, Cal.Artemis)
            || CalResolve.Is(t, Cal.SupremeCalamitas)
            || CalResolve.Is(t, Cal.PrimordialWyrmHead);

        #endregion

        /// <summary>Lazy-resolved Calamity NPC type IDs. -1 if Calamity not loaded or boss not found.</summary>
        internal static class Cal
        {
            internal static int DesertScourgeHead;
            internal static int Crabulon;
            internal static int HiveMind;
            internal static int PerforatorHive;
            internal static int SlimeGodCore;
            internal static int AquaticScourgeHead;
            internal static int BrimstoneElemental;
            internal static int Cryogen;
            internal static int CalamitasClone;
            internal static int Leviathan;
            internal static int Anahita;
            internal static int AstrumAureus;
            internal static int PlaguebringerGoliath;
            internal static int RavagerBody;
            internal static int AstrumDeusHead;
            internal static int ProfanedGuardianCommander;
            internal static int ProfanedGuardianDefender;
            internal static int ProfanedGuardianHealer;
            internal static int Dragonfolly;
            internal static int Providence;
            internal static int StormWeaverHead;
            internal static int CeaselessVoid;
            internal static int Signus;
            internal static int Polterghast;
            internal static int OldDuke;
            internal static int DevourerofGodsHead;
            internal static int Yharon;
            internal static int ThanatosHead;
            internal static int AresBody;
            internal static int Apollo;
            internal static int Artemis;
            internal static int SupremeCalamitas;
            internal static int PrimordialWyrmHead;
        }

        /// <summary>Lazy one-time resolver. Returns true if t matches a cached Calamity type. Returns false for unresolved (negative) IDs.</summary>
        private static class CalResolve
        {
            private static bool _resolved;

            public static bool Is(int t, int calType)
            {
                if (!_resolved) Resolve();
                return calType > 0 && t == calType;
            }

            private static void Resolve()
            {
                if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    _resolved = true;
                    return;
                }

                Cal.DesertScourgeHead       = FindNPC(calamity, "DesertScourgeHead");
                Cal.Crabulon                = FindNPC(calamity, "Crabulon");
                Cal.HiveMind                = FindNPC(calamity, "HiveMind");
                Cal.PerforatorHive          = FindNPC(calamity, "PerforatorHive");
                Cal.SlimeGodCore            = FindNPC(calamity, "SlimeGodCore");
                Cal.AquaticScourgeHead      = FindNPC(calamity, "AquaticScourgeHead");
                Cal.BrimstoneElemental      = FindNPC(calamity, "BrimstoneElemental");
                Cal.Cryogen                 = FindNPC(calamity, "Cryogen");
                Cal.CalamitasClone          = FindNPC(calamity, "CalamitasClone");
                Cal.Leviathan               = FindNPC(calamity, "Leviathan");
                Cal.Anahita                 = FindNPC(calamity, "Anahita");
                Cal.AstrumAureus            = FindNPC(calamity, "AstrumAureus");
                Cal.PlaguebringerGoliath    = FindNPC(calamity, "PlaguebringerGoliath");
                Cal.RavagerBody             = FindNPC(calamity, "RavagerBody");
                Cal.AstrumDeusHead          = FindNPC(calamity, "AstrumDeusHead");
                Cal.ProfanedGuardianCommander = FindNPC(calamity, "ProfanedGuardianCommander");
                Cal.ProfanedGuardianDefender  = FindNPC(calamity, "ProfanedGuardianDefender");
                Cal.ProfanedGuardianHealer    = FindNPC(calamity, "ProfanedGuardianHealer");
                Cal.Dragonfolly             = FindNPC(calamity, "Dragonfolly");
                Cal.Providence              = FindNPC(calamity, "Providence");
                Cal.StormWeaverHead         = FindNPC(calamity, "StormWeaverHead");
                Cal.CeaselessVoid           = FindNPC(calamity, "CeaselessVoid");
                Cal.Signus                  = FindNPC(calamity, "Signus");
                Cal.Polterghast             = FindNPC(calamity, "Polterghast");
                Cal.OldDuke                 = FindNPC(calamity, "OldDuke");
                Cal.DevourerofGodsHead      = FindNPC(calamity, "DevourerofGodsHead");
                Cal.Yharon                  = FindNPC(calamity, "Yharon");
                Cal.ThanatosHead            = FindNPC(calamity, "ThanatosHead");
                Cal.AresBody                = FindNPC(calamity, "AresBody");
                Cal.Apollo                  = FindNPC(calamity, "Apollo");
                Cal.Artemis                 = FindNPC(calamity, "Artemis");
                Cal.SupremeCalamitas        = FindNPC(calamity, "SupremeCalamitas");
                Cal.PrimordialWyrmHead      = FindNPC(calamity, "PrimordialWyrmHead");

                _resolved = true;
            }

            private static int FindNPC(Mod mod, string name) =>
                mod.TryFind(name, out ModNPC npc) ? npc.Type : -1;
        }
    }
}
