using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using GrowableTerraprisma.Content.Buffs;
using GrowableTerraprisma.Players;
using GrowableTerraprisma.Systems;

namespace GrowableTerraprisma.Content.Projectiles
{
    public class GrowableTerraprismaProjectile : ModProjectile
    {
        private const int AttackTimerStart = 40;
        private const int AttackTimerEnd = 80;
        private const int ApproachThreshold = AttackTimerStart + 1;
        private const int ApproachMax = AttackTimerStart - 1;
        private const int DashMax = AttackTimerEnd - 1;

        private const int FetchApproachState = -2;
        private const int FetchReturnState = -3;
        private const int FetchSearchInterval = 120;
        private const int FetchSearchStagger = 5;

        private List<int> _blacklist = new();
        private int _lifeStealCounter;

        public bool FocusOnFetching = false;

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;

            // 预加载原版 EmpressBlade 贴图，避免首次绘制时纹理未就绪导致模型不可见
            Main.instance.LoadProjectile(ProjectileID.EmpressBlade);
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft *= 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 0.75f;
            Projectile.manualDirectionChange = true;
            Projectile.DamageType = DamageClass.Summon;

            int trailLength = ProjectileID.Sets.TrailCacheLength[Type];
            if (Projectile.oldPos.Length != trailLength)
            {
                Array.Resize(ref Projectile.oldPos, trailLength);
                Array.Resize(ref Projectile.oldRot, trailLength);
                Array.Resize(ref Projectile.oldSpriteDirection, trailLength);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(FocusOnFetching);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            FocusOnFetching = reader.ReadBoolean();
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var growable = player.GetModPlayer<GrowableTerraprismaPlayer>();

            player.AddBuff(ModContent.BuffType<GrowableTerraprismaBuff>(), 3600);

            if (player.dead)
            {
                growable.growableMinionActive = false;
            }

            if (growable.growableMinionActive)
            {
                Projectile.timeLeft = 2;
            }

            // --- gtprisma 被动能力（仅在召唤物存活时生效） ---
            if (growable.defeatedBossTypes.Contains(NPCID.WallofFlesh))
            {
                Lighting.AddLight(Projectile.Center, 1.2f, 0.9f, 1.8f);
            }

            if (growable.defeatedBossTypes.Contains(NPCID.QueenBee))
            {
                Projectile.localAI[2] -= 1f;
                if (Projectile.localAI[2] <= 0f)
                {
                    Projectile.localAI[2] = 150f;
                    // 首个召唤物，仅服务器/单人执行
                    if (Projectile.owner == Main.myPlayer)
                    {
                        int myIdx = 0;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile p = Main.projectile[i];
                            if (p.active && p.owner == Projectile.owner && p.type == Projectile.type && Projectile.whoAmI > i)
                                myIdx++;
                        }
                        if (myIdx == 0)
                        {
                            int beeDmg = (int)(Projectile.damage * 0.35);
                            var bee = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center,
                                Vector2.Zero, ProjectileID.Bee, beeDmg, 2f, Projectile.owner);
                            bee.originalDamage = beeDmg;
                            bee.DamageType = DamageClass.Summon;
                        }
                    }
                }
            }

            _blacklist.Clear();
            AI_156_Think(player, _blacklist);
        }

        private void AI_156_Think(Player player, List<int> blacklist)
        {
            var growable = player.GetModPlayer<GrowableTerraprismaPlayer>();
            bool fetchUnlocked = growable.defeatedBossTypes.Contains(NPCID.TheDestroyer);

            if (player.active && Vector2.Distance(player.Center, Projectile.Center) > 2000f)
            {
                if (Projectile.ai[0] == FetchApproachState || Projectile.ai[0] == FetchReturnState)
                    CancelFetch();
                Projectile.ai[0] = 0f;
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }

            AI_GetMyGroupIndexAndFillBlackList(blacklist, out int groupIndex, out int groupTotal);
            ulong staggerFrame = Main.GameUpdateCount + (ulong)(groupIndex * FetchSearchStagger);

            bool inFetch = Projectile.ai[0] == FetchApproachState || Projectile.ai[0] == FetchReturnState;
            bool inCombat = Projectile.ai[0] > 0f;

            if (fetchUnlocked && !inFetch && staggerFrame % (ulong)FetchSearchInterval == 0)
            {
                bool urgentOnly = inCombat && !FocusOnFetching;
                if (TryFindAndLockItem(player, urgentOnly))
                {
                    Projectile.ai[0] = FetchApproachState;
                    Projectile.netUpdate = true;
                }
            }

            if (Projectile.ai[0] == FetchApproachState)
            {
                RunFetchApproachAI(player);
                return;
            }

            if (Projectile.ai[0] == FetchReturnState)
            {
                RunFetchReturnAI(player, groupIndex, groupTotal);
                return;
            }

            if (Projectile.ai[0] == -1f)
            {
                AI_GetMyGroupIndexAndFillBlackList(blacklist, out int index, out int total);
                GetIdlePosition(index, total, player, out Vector2 idleSpot, out float idleRotation);
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Projectile.Center.MoveTowards(idleSpot, 32f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation, 0.2f);
                if (Projectile.Distance(idleSpot) < 2f)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                return;
            }

            if (Projectile.ai[0] == 0f)
            {
                AI_GetMyGroupIndexAndFillBlackList(blacklist, out int index, out int total);
                GetIdlePosition(index, total, player, out Vector2 idleSpot, out float idleRotation);
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, idleSpot, 0.45f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation, 0.45f);

                if (Main.rand.NextBool(20))
                {
                    int target = TryAttackingNPCs(player, blacklist);
                    if (target != -1)
                    {
                        StartAttack();
                        Projectile.ai[0] = AttackTimerEnd;
                        Projectile.ai[1] = target;
                        Projectile.netUpdate = true;
                    }
                }
                return;
            }

            RunAttackAI(player, blacklist);
        }

        private bool TryFindAndLockItem(Player player, bool urgentOnly)
        {
            float searchRadius = 50f * 16f;
            float bestDist = float.MaxValue;
            int bestItem = -1;
            bool bestUrgent = false;

            bool needLife = (float)player.statLife / player.statLifeMax2 < 0.9f;
            bool needMana = (float)player.statMana / player.statManaMax2 < 0.9f;

            foreach (var item in Main.ActiveItems)
            {
                if (item.noGrabDelay > 0 ||
                    item.beingGrabbed ||
                    ItemFetchLockSystem.IsItemLocked(item.whoAmI) ||
                    ItemFetchLockSystem.IsItemOnCooldown(item.whoAmI) ||
                    !player.CanPullItem(item, player.ItemSpace(item)))
                    continue;

                bool heart = IsHeart(item);
                bool star = IsManaStar(item);

                if (heart && !needLife)
                    continue;
                if (star && !needMana)
                    continue;

                bool urgent = heart || star;
                if (urgentOnly && !urgent)
                    continue;

                float dist = item.Distance(Projectile.Center);
                if (dist >= searchRadius)
                    continue;

                bool better = (urgent && !bestUrgent) || (urgent == bestUrgent && dist < bestDist);
                if (better)
                {
                    bestDist = dist;
                    bestItem = item.whoAmI;
                    bestUrgent = urgent;
                }
            }

            if (bestItem >= 0 && ItemFetchLockSystem.TryLockItem(bestItem, Projectile.whoAmI))
            {
                Projectile.ai[1] = bestItem;
                return true;
            }
            return false;
        }

        private static bool IsHeart(Item item)
        {
            return item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane;
        }

        private static bool IsManaStar(Item item)
        {
            return item.type == ItemID.Star || item.type == ItemID.SoulCake || item.type == ItemID.SugarPlum;
        }

        private void RunFetchApproachAI(Player player)
        {
            int itemIndex = (int)Projectile.ai[1];
            if (!Main.item.IndexInRange(itemIndex))
            {
                CancelFetch();
                return;
            }

            Item item = Main.item[itemIndex];
            if (!item.active || item.beingGrabbed)
            {
                CancelFetch();
                return;
            }

            float distance = Projectile.Distance(item.Center);
            if (distance < 20f)
            {
                item.Center = Projectile.Center;
                item.velocity = Vector2.Zero;
                Projectile.ai[0] = FetchReturnState;
                Projectile.netUpdate = true;
            }
            else
            {
                Vector2 dashDirection = Projectile.Center.DirectionTo(item.Center);
                Projectile.velocity = dashDirection * 22f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }

        private void RunFetchReturnAI(Player player, int index, int total)
        {
            int itemIndex = (int)Projectile.ai[1];
            if (!Main.item.IndexInRange(itemIndex))
            {
                CancelFetch();
                return;
            }

            Item item = Main.item[itemIndex];
            if (!item.active)
            {
                CancelFetch();
                return;
            }

            item.Center = Projectile.Center;
            item.velocity = Vector2.Zero;
            Projectile.velocity = Vector2.Zero;

            float distToPlayer = Projectile.Distance(player.Center);
            if (distToPlayer < 40f)
            {
                item.Center = player.Center;
                item.velocity = Vector2.Zero;
                item.noGrabDelay = 0;
                ItemFetchLockSystem.UnlockItem(itemIndex, Projectile.whoAmI);
                ItemFetchLockSystem.SetCooldown(itemIndex, 120);
                Projectile.ai[0] = 0f;
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.Center = Projectile.Center.MoveTowards(player.Center, 18f);
                Projectile.rotation = Projectile.rotation.AngleLerp(
                    Projectile.AngleTo(player.Center) + MathHelper.PiOver2, 0.4f);
            }
        }

        private void CancelFetch()
        {
            int itemIndex = (int)Projectile.ai[1];
            ItemFetchLockSystem.UnlockItem(itemIndex, Projectile.whoAmI);
            if (Main.item.IndexInRange(itemIndex))
                Main.item[itemIndex].noGrabDelay = 0;
            Projectile.ai[0] = -1f;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
        }

        private void RunAttackAI(Player player, List<int> blacklist)
        {
            int num14;
            int num15;
            int num16;

            if (Projectile.ai[0] >= ApproachThreshold)
            {
                num14 = 1;
                num15 = DashMax;
                num16 = ApproachThreshold;
            }
            else
            {
                num14 = 0;
                num15 = ApproachMax;
                num16 = 0;
            }

            int targetIndex = (int)Projectile.ai[1];

            if (!Main.npc.IndexInRange(targetIndex))
            {
                int newTarget = TryAttackingNPCs(player, blacklist, skipBodyCheck: true);
                if (newTarget != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList(AttackTimerStart, AttackTimerEnd);
                    Projectile.ai[1] = newTarget;
                    StartAttack();
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                }
                Projectile.netUpdate = true;
                return;
            }

            NPC npc = Main.npc[targetIndex];
            if (!npc.CanBeChasedBy(Projectile))
            {
                int newTarget = TryAttackingNPCs(player, blacklist, skipBodyCheck: true);
                if (newTarget != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList(AttackTimerStart, AttackTimerEnd);
                    Projectile.ai[1] = newTarget;
                    StartAttack();
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                }
                Projectile.netUpdate = true;
                return;
            }

            Projectile.ai[0] -= 1f;

            if (Projectile.ai[0] >= num15)
            {
                Projectile.direction = (Projectile.Center.X < npc.Center.X) ? 1 : -1;
                if (Projectile.ai[0] == num15)
                {
                    Projectile.localAI[0] = Projectile.Center.X;
                    Projectile.localAI[1] = Projectile.Center.Y;
                }
            }

            float lerpValue = Utils.GetLerpValue(num15, num16, Projectile.ai[0], clamped: true);

            if (num14 == 0)
            {
                // Approach phase: arc toward target
                Vector2 startPos = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                if (lerpValue >= 0.5f)
                {
                    startPos = Vector2.Lerp(npc.Center, player.Center, 0.5f);
                }

                float angleToTarget = (npc.Center - startPos).ToRotation();
                float baseAngle = (Projectile.direction == 1) ? -(float)Math.PI : (float)Math.PI;
                float swingAngle = baseAngle + (0f - baseAngle) * lerpValue * 2f;
                Vector2 swingDirection = swingAngle.ToRotationVector2();
                swingDirection.Y *= 0.5f;
                swingDirection.Y *= 0.8f + (float)Math.Sin(Projectile.identity * 2.3f) * 0.2f;
                swingDirection = swingDirection.RotatedBy(angleToTarget);

                float swingRadius = (npc.Center - startPos).Length() / 2f;
                Projectile.Center = Vector2.Lerp(startPos, npc.Center, 0.5f) + swingDirection * swingRadius;

                float facingAngle = MathHelper.WrapAngle(angleToTarget + swingAngle + 0f);
                Projectile.rotation = facingAngle + (float)Math.PI / 2f;
                Projectile.velocity = facingAngle.ToRotationVector2() * 10f;
                Projectile.position -= Projectile.velocity;
            }
            else
            {
                // Dash phase: viper strike
                Vector2 startPos = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                startPos += new Vector2(0f, Utils.GetLerpValue(0f, 0.4f, lerpValue, clamped: true) * -100f);

                Vector2 toTarget = npc.Center - startPos;
                Vector2 dashDirection = toTarget.SafeNormalize(Vector2.Zero);
                Vector2 overshootPos = npc.Center + dashDirection * MathHelper.Clamp(toTarget.Length(), 60f, 150f);

                float approachLerp = Utils.GetLerpValue(0.4f, 0.6f, lerpValue, clamped: true);
                float dashLerp = Utils.GetLerpValue(0.6f, 1f, lerpValue, clamped: true);

                float targetAngle = dashDirection.ToRotation() + (float)Math.PI / 2f;
                Projectile.rotation = Projectile.rotation.AngleTowards(targetAngle, (float)Math.PI / 5f);

                Projectile.Center = Vector2.Lerp(startPos, npc.Center, approachLerp);
                if (dashLerp > 0f)
                {
                    Projectile.Center = Vector2.Lerp(npc.Center, overshootPos, dashLerp);
                }
            }

            if (Projectile.ai[0] == num16)
            {
                int newTarget = TryAttackingNPCs(player, blacklist, skipBodyCheck: true);
                if (newTarget != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList(AttackTimerStart, AttackTimerEnd);
                    Projectile.ai[1] = newTarget;
                    StartAttack();
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                }
                Projectile.netUpdate = true;
            }
        }

        private void StartAttack()
        {
            Projectile.ResetLocalNPCHitImmunity();
        }

        private int TryAttackingNPCs(Player player, List<int> blacklist, bool skipBodyCheck = false)
        {
            Vector2 playerCenter = player.Center;
            int result = -1;
            float closestDist = -1f;

            NPC ownerTarget = Projectile.OwnerMinionAttackTargetNPC;
            if (ownerTarget != null && ownerTarget.CanBeChasedBy(Projectile))
            {
                bool valid = true;
                if (!ownerTarget.boss && blacklist.Contains(ownerTarget.whoAmI))
                    valid = false;
                if (ownerTarget.Distance(playerCenter) > 1000f)
                    valid = false;
                if (!skipBodyCheck && !Projectile.CanHitWithOwnBody(ownerTarget))
                    valid = false;
                if (valid)
                    return ownerTarget.whoAmI;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile) && (npc.boss || !blacklist.Contains(i)))
                {
                    float dist = npc.Distance(playerCenter);
                    if (!(dist > 1000f) && (!(dist > closestDist) || closestDist == -1f) && (skipBodyCheck || Projectile.CanHitWithOwnBody(npc)))
                    {
                        closestDist = dist;
                        result = i;
                    }
                }
            }

            return result;
        }

        private void AI_GetMyGroupIndexAndFillBlackList(List<int> blacklist, out int index, out int totalIndexesInGroup)
        {
            index = 0;
            totalIndexesInGroup = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Projectile.owner && proj.type == Projectile.type)
                {
                    if (Projectile.whoAmI > i)
                        index++;
                    totalIndexesInGroup++;
                }
            }
        }

        private static void GetIdlePosition(int stackedIndex, int totalIndexes, Player player, out Vector2 idleSpot, out float idleRotation)
        {
            if (totalIndexes <= 0) totalIndexes = 1;
            int num = stackedIndex + 1;
            idleRotation = num * ((float)Math.PI * 2f) * (1f / 60f) * player.direction + (float)Math.PI / 2f;
            idleRotation = MathHelper.WrapAngle(idleRotation);

            int remainder = num % totalIndexes;
            Vector2 wobble = new Vector2(0f, 0.5f).RotatedBy(
                (player.miscCounterNormalized * (2f + remainder) + remainder * 0.5f + player.direction * 1.3f) * ((float)Math.PI * 2f)
            ) * 4f;

            idleSpot = idleRotation.ToRotationVector2() * 10f
                + player.MountedCenter
                + new Vector2(player.direction * (num * -6 - 16), player.gravDir * -15f);
            idleSpot += wobble;
            idleRotation += (float)Math.PI / 2f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            DrawTrail(player);

            // EmpressBladeDrawer 通过 ShaderData.Apply() 将 SpriteBatch 切到 Immediate 模式，
            // 后续 EntitySpriteDraw 需要 Deferred。手动恢复。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            DrawSprite(player);
            return false;
        }

        private void DrawTrail(Player player)
        {
            EmpressBladeDrawer drawer = default;
            float timeFactor = Main.GlobalTimeWrappedHourly % 3f / 3f;
            float maxMinions = MathHelper.Max(1f, player.maxMinions);
            float hueBase = (Projectile.identity % maxMinions / maxMinions + timeFactor);

            Color colorStart = Projectile.GetFairyQueenWeaponsColor(0f, 0f, hueBase % 1f);
            Color colorEnd = Projectile.GetFairyQueenWeaponsColor(0f, 0f, (hueBase + 0.5f) % 1f);

            drawer.ColorStart = colorStart;
            drawer.ColorEnd = colorEnd;
            drawer.Draw(Projectile);
        }

        private void DrawSprite(Player player)
        {
            float timeFactor = Main.GlobalTimeWrappedHourly % 3f / 3f;
            float maxMinions = MathHelper.Max(1f, player.maxMinions);
            float hueBase = (Projectile.identity % maxMinions / maxMinions + timeFactor);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
            Vector2 origin = texture.Size() / 2f;
            Color baseColor = Color.White * Projectile.Opacity;
            baseColor.A = (byte)(baseColor.A * 0.7f);

            Color glowColor = Projectile.GetFairyQueenWeaponsColor(0.5f, 0f, hueBase);
            glowColor.A /= 2;

            float scale = Projectile.scale;
            float angle = Projectile.rotation - (float)Math.PI / 2f;
            float glowAlpha = Projectile.Opacity * 0.3f;

            if (glowAlpha > 0f)
            {
                float ghost1, ghost2;
                if (Projectile.ai[0] == FetchApproachState || Projectile.ai[0] == FetchReturnState)
                {
                    ghost1 = 1f;
                    ghost2 = 1f;
                }
                else
                {
                    ghost1 = Utils.GetLerpValue(AttackTimerStart, 50f, Projectile.ai[0], clamped: true);
                    ghost2 = Utils.GetLerpValue(70f, 50f, Projectile.ai[0], clamped: true)
                        * Utils.GetLerpValue(AttackTimerStart, 45f, Projectile.ai[0], clamped: true);
                }

                for (float i = 0f; i < 1f; i += 1f / 6f)
                {
                    Vector2 offset = angle.ToRotationVector2() * -120f * i * ghost1;
                    Main.EntitySpriteDraw(texture, drawPos + offset, null, glowColor * glowAlpha * (1f - i) * ghost2,
                        angle, origin, scale * 1.5f, SpriteEffects.None);
                }

                for (float i = 0f; i < 1f; i += 0.25f)
                {
                    Vector2 offset = (i * ((float)Math.PI * 2f) + angle).ToRotationVector2() * 4f * scale;
                    Main.EntitySpriteDraw(texture, drawPos + offset, null, glowColor * glowAlpha,
                        angle, origin, scale, SpriteEffects.None);
                }
            }

            Main.EntitySpriteDraw(texture, drawPos, null, baseColor, angle, origin, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, glowColor * glowAlpha * 0.5f, angle, origin, scale, SpriteEffects.None);
        }

        public override bool MinionContactDamage()
        {
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            var growable = Main.player[Projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();

            if (growable.defeatedBossTypes.Contains(NPCID.WallofFlesh))
            {
                _lifeStealCounter++;
                if (_lifeStealCounter >= 10)
                {
                    _lifeStealCounter = 0;
                    Player player = Main.player[Projectile.owner];
                    player.Heal(1);
                }
            }

            if (growable.defeatedBossTypes.Contains(NPCID.Retinazer)
                && growable.defeatedBossTypes.Contains(NPCID.Spazmatism))
            {
                growable.miniPrismHitCounter++;
                if (growable.miniPrismHitCounter >= 20)
                {
                    growable.miniPrismHitCounter = 0;
                    Vector2 vel = Projectile.Center.DirectionTo(target.Center) * 12f;
                    int miniDmg = (int)(Projectile.originalDamage * 0.4f);
                    var mini = Projectile.NewProjectileDirect(
                        Projectile.GetSource_OnHit(target), Projectile.Center, vel,
                        ModContent.ProjectileType<TwinMiniPrismProjectile>(), miniDmg, 2f, Projectile.owner,
                        target.whoAmI);
                    mini.originalDamage = miniDmg;
                    mini.DamageType = DamageClass.Summon;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var growable = Main.player[Projectile.owner].GetModPlayer<GrowableTerraprismaPlayer>();
            if (growable.defeatedBossTypes.Contains(NPCID.SkeletronPrime))
            {
                modifiers.ArmorPenetration += 10;
            }
        }

        /// <summary>
        /// 精灵残影拖尾 — 遍历 oldPos/oldRot 绘制渐进透明精灵副本。
        /// 供 uprisma 使用；gtprisma 使用 EmpressBladeDrawer 顶点条带。
        /// </summary>
        public static void DrawSpriteTrail(Projectile proj, Player player)
        {
            float timeFactor = Main.GlobalTimeWrappedHourly % 3f / 3f;
            float maxMinions = MathHelper.Max(1f, player.maxMinions);
            float hueBase = (proj.identity % maxMinions / maxMinions + timeFactor);

            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
            Vector2 origin = texture.Size() / 2f;

            Color colorStart = proj.GetFairyQueenWeaponsColor(0f, 0f, hueBase % 1f);
            Color colorEnd = proj.GetFairyQueenWeaponsColor(0f, 0f, (hueBase + 0.5f) % 1f);

            for (int i = 1; i < proj.oldPos.Length; i++)
            {
                if (proj.oldPos[i] == Vector2.Zero)
                    break;

                float progress = 1f - i / (float)proj.oldPos.Length;
                Color trailColor = Color.Lerp(colorStart, colorEnd, progress) * (progress * 0.5f);
                trailColor.A = (byte)(trailColor.A * progress);

                Vector2 drawPos = proj.oldPos[i] + proj.Size / 2f - Main.screenPosition;

                Main.EntitySpriteDraw(texture, drawPos, null, trailColor,
                    proj.oldRot[i] - (float)Math.PI / 2f, origin, proj.scale * progress, SpriteEffects.None);
            }
        }
    }
}
