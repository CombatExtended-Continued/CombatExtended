using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Runtime.CompilerServices;
using RimWorld.Planet;

namespace CombatExtended
{
    public static class CE_Utility
    {
        #region Camera

        public static float CameraAltitude
        {
            get
            {
                return Find.CameraDriver?.CurrentRealPosition.y ?? -1;
            }
        }

        #endregion

        #region Math

        public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
        {
            float u, v, S;
            do
            {
                u = 2.0f * UnityEngine.Random.value - 1.0f;
                v = 2.0f * UnityEngine.Random.value - 1.0f;
                S = u * u + v * v;
            }
            while (S >= 1.0f);
            float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
            float mean = (minValue + maxValue) / 2.0f;
            float sigma = (maxValue - mean) / 5.0f;
            return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
        }

        #endregion

        #region Attachments

        /// <summary>
        /// A comibination of StatWorker.FinalizeValue and GetStatValue but with the ability to get stats without attachments affecting the calculations or using a custom list of stats
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="stat"></param>
        /// <param name="applyPostProcess"></param>
        /// <returns></returns>
        public static float GetWeaponStatWith(this WeaponPlatform platform, StatDef stat, List<AttachmentLink> links, bool applyPostProcess = true)
        {
            StatRequest req = StatRequest.For(platform);
            float val = stat.Worker.GetValueUnfinalized(StatRequest.For(platform), true);
            if (stat.parts != null)
            {
                for (int i = 0; i < stat.parts.Count; i++)
                {
                    if (!(stat.parts[i] is StatPart_Attachments))
                    {
                        stat.parts[i].TransformValue(req, ref val);
                    }
                }
                if (links != null)
                {
                    stat.TransformValue(links, ref val);
                }
            }
            if (applyPostProcess && stat.postProcessCurve != null)
            {
                val = stat.postProcessCurve.Evaluate(val);
            }
            if (applyPostProcess && stat.postProcessStatFactors != null)
            {
                for (int j = 0; j < stat.postProcessStatFactors.Count; j++)
                {
                    val *= req.Thing.GetStatValue(stat.postProcessStatFactors[j]);
                }
            }
            if (Find.Scenario != null)
            {
                val *= Find.Scenario.GetStatFactor(stat);
            }
            if (Mathf.Abs(val) > stat.roundToFiveOver)
            {
                val = Mathf.Round(val / 5f) * 5f;
            }
            if (stat.roundValue)
            {
                val = Mathf.RoundToInt(val);
            }
            if (applyPostProcess)
            {
                val = Mathf.Clamp(val, stat.minValue, stat.maxValue);
            }
            return val;
        }

        /// <summary>
        /// Used to setup the weapon from the current loadout
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool TrySyncPlatformLoadout(this WeaponPlatform platform, Pawn pawn)
        {
            Loadout loadout = pawn.GetLoadout();
            if (loadout == null)
            {
                return false;
            }
            LoadoutSlot slot = loadout.Slots.FirstOrFallback(s => s.weaponPlatformDef == platform.Platform);
            // if no slot mention this or it allows everything return false
            if (slot == null || slot.allowAllAttachments)
            {
                return false;
            }
            bool update = false;
            // check if the current setup include everything we need.
            foreach (AttachmentDef def in slot.attachments)
            {
                if (!platform.TargetConfig.Any(a => a == def))
                {
                    update = true;
                    break;
                }
            }
            if (update)
            {
                // sync the loadout with the weapon.
                platform.TargetConfig = slot.attachments;
                platform.UpdateConfiguration();
            }
            return update;
        }

        /// <summary>
        /// Used to obtain the explaination for stat values for weapons with attachments.
        /// </summary>
        /// <param name="stat">StatDef</param>
        /// <param name="links">AttachmentLinks</param>
        /// <returns>Explaination</returns>
        public static string ExplainAttachmentsStat(this StatDef stat, IEnumerable<AttachmentLink> links)
        {
            if (links == null || links.Count() == 0)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            bool anyOffsets = false;
            bool anyFactors = false;
            foreach (AttachmentLink link in links)
            {
                StatModifier modifier = link.statReplacers?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null)
                {
                    continue;
                }
                // stop since we found an override modifier.
                sb.AppendLine("Replaced with " + link.attachment.LabelCap + ": " + modifier.value);
                break;
            }
            foreach (AttachmentLink link in links)
            {
                StatModifier modifier = link.statOffsets?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null || modifier.value == 0)
                {
                    continue;
                }
                if (!anyOffsets)
                {
                    sb.AppendLine("Attachment offsets:");
                    anyOffsets = true;
                }
                sb.AppendLine("    " + link.attachment.LabelCap + ": " + stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Offset));
            }
            foreach (AttachmentLink link in links)
            {
                StatModifier modifier = link.statMultipliers?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null || modifier.value == 0 || modifier.value == 1)
                {
                    continue;
                }
                if (!anyFactors)
                {
                    sb.AppendLine("Attachment factors:");
                    anyFactors = true;
                }
                sb.AppendLine("    " + link.attachment.LabelCap + ": " + stat.Worker.ValueToString(modifier.value, finalized: false, ToStringNumberSense.Factor));
            }
            return sb.ToString();
        }

        /// <summary>
        /// A comibination of StatWorker.FinalizeValue and GetStatValue but with the ability to get stats without attachments affecting the calculations or using a custom list of stats.
        /// This version can be used with only the weapon def.
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="stat"></param>
        /// <param name="applyPostProcess"></param>
        /// <returns></returns>
        public static float GetWeaponStatAbstractWith(this WeaponPlatformDef platform, StatDef stat, List<AttachmentLink> links, bool applyPostProcess = true)
        {
            platform.statBases.FirstOrFallback(s => s.stat == stat);
            StatModifier statBase = platform.statBases.FirstOrFallback(s => s.stat == stat);
            float val = statBase != null ? statBase.value : stat.defaultBaseValue;
            if (stat.parts != null)
            {
                for (int i = 0; i < stat.parts.Count; i++)
                {
                    if (stat.parts[i] is StatPart_Quality || stat.parts[i] is StatPart_Quality_Offset)
                    {
                        stat.parts[i].TransformValue(new StatRequest()
                        {
                            qualityCategoryInt = QualityCategory.Normal
                        }, ref val);
                    }
                }
                if (links != null)
                {
                    stat.TransformValue(links, ref val);
                }
            }
            if (applyPostProcess && stat.postProcessCurve != null)
            {
                val = stat.postProcessCurve.Evaluate(val);
            }
            if (Find.Scenario != null)
            {
                val *= Find.Scenario.GetStatFactor(stat);
            }
            if (Mathf.Abs(val) > stat.roundToFiveOver)
            {
                val = Mathf.Round(val / 5f) * 5f;
            }
            if (stat.roundValue)
            {
                val = Mathf.RoundToInt(val);
            }
            if (applyPostProcess)
            {
                val = Mathf.Clamp(val, stat.minValue, stat.maxValue);
            }
            return val;
        }

        /// <summary>
        /// Return wether you attach an attachment to a weapon with having to remove stuff.
        /// </summary>
        /// <param name="attachment">Attachment</param>
        /// <param name="platform">Weapon</param>
        /// <returns>Wether you can attach without conflicts</returns>
        public static bool CanAttachTo(this AttachmentDef attachment, WeaponPlatform platform)
        {
            foreach (AttachmentLink link in platform.attachments)
            {
                if (!platform.Platform.AttachmentsCompatible(link.attachment, attachment))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Used to tranform a stat for a given attachment link list. It will first check for overriden stats then apply offsets and multipliers.
        /// </summary>
        /// <param name="stat">StatDef</param>
        /// <param name="links">The current attachment links</param>
        /// <param name="val">Val</param>
        public static void TransformValue(this StatDef stat, List<AttachmentLink> links, ref float val)
        {
            if (links == null || links.Count == 0)
            {
                return;
            }
            for (int i = 0; i < links.Count; i++)
            {
                AttachmentLink link = links[i];
                StatModifier modifier = link.statReplacers?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null)
                {
                    continue;
                }
                // stop since we found an override modifier.
                val = modifier.value;
                return;
            }
            for (int i = 0; i < links.Count; i++)
            {
                AttachmentLink link = links[i];
                StatModifier modifier = link.statOffsets?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null)
                {
                    continue;
                }
                val += modifier.value;
            }
            for (int i = 0; i < links.Count; i++)
            {
                AttachmentLink link = links[i];
                StatModifier modifier = link.statMultipliers?.FirstOrFallback(m => m.stat == stat, null) ?? null;
                if (modifier == null || modifier.value <= 0)
                {
                    continue;
                }
                val *= modifier.value;
            }
        }

        #endregion

        #region Blitting
        private const int blitMaxDimensions = 64;

        /// <summary>
        /// Code from https://gamedev.stackexchange.com/questions/92285/unity3d-resize-texture-without-corruption
        /// </summary>
        /// <param name="texture">Any texture with or without read-write protection</param>
        /// <param name="blitRect">The Rect to be extracted from the <i>rtSize</i>'d render of <i>texture</i> (.x+.width, .y+.height smaller than <i>rtSize</i>)</param>
        /// <param name="rtSize">The size that <i>texture</i> is to be rendered at</param>
        /// <returns>Texture2D of size <i>blitRect</i>.width, <i>blitRect</i>.height extracted from a <i>rtSize</i>[0] width, <i>rtSize</i>[1] height render of <i>texture</i> starting at position (<i>blitRect</i>.x, <i>blitRect</i>.y).</returns>
        public static Texture2D Blit(this Texture2D texture, Rect blitRect, int[] rtSize)
        {
            var prevFilterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;

            RenderTexture rt = RenderTexture
                               .GetTemporary(rtSize[0],                        //render width
                                             rtSize[1],                       //render height
                                             0,                               //no depth buffer
                                             RenderTextureFormat.Default,     //default (=automatic) color mode
                                             RenderTextureReadWrite.Default,  //default (=automatic) r/w mode
                                             1);                              //no anti-aliasing (1=none,2=2x,4=4x,8=8x)

            rt.filterMode = FilterMode.Point;

            RenderTexture.active = rt;

            Graphics.Blit(texture, rt);

            Texture2D blit = new Texture2D((int)blitRect.width, (int)blitRect.height);
            blit.ReadPixels(blitRect, 0, 0);
            blit.Apply();

            RenderTexture.active = null;

            texture.filterMode = prevFilterMode;

            return blit;
        }

        /// <summary>
        /// Texture2D.GetPixels() method circumventing the read-write protection and taking into account <i>blitMaxDimensions</i>.
        /// </summary>
        /// <param name="texture">Any texture with/without read-write protection, of any size (but will be scaled to blitMaxDimensions if larger than those)</param>
        /// <param name="width">Final width of Color[]</param>
        /// <param name="height">Final height of Color[]</param>
        /// <returns>Color[] array after resizing to fit blitMaxDimensions</returns>
        public static Color[] GetColorSafe(this Texture2D texture, out int width, out int height)
        {
            width = texture.width;
            height = texture.height;
            if (texture.width > texture.height)
            {
                width = Math.Min(width, blitMaxDimensions);
                height = (int)((float)width * ((float)texture.height / (float)texture.width));
            }
            else if (texture.height > texture.width)
            {
                height = Math.Min(height, blitMaxDimensions);
                width = (int)((float)height * ((float)texture.width / (float)texture.height));
            }
            else
            {
                width = Math.Min(width, blitMaxDimensions);
                height = Math.Min(height, blitMaxDimensions);
            }

            Color[] color = null;

            var blitRect = new Rect(0, 0, width, height);
            var rtSize = new[] { width, height };

            if (width == texture.width && height == texture.height)
            {
                try
                {
                    color = texture.GetPixels();
                }
                catch
                {
                    color = texture.Blit(blitRect, rtSize).GetPixels();
                }
            }
            else
            {
                color = texture.Blit(blitRect, rtSize).GetPixels();
            }
            return color;
        }

        public static Texture2D BlitCrop(this Texture2D texture, Rect blitRect)
        {
            return texture.Blit(blitRect, new int[] { texture.width, texture.height });
        }
        #endregion

        #region Misc

        /// <summary>
        /// Gets the true rating of armor with partial stats taken into account
        /// </summary>
        public static float PartialStat(this Apparel apparel, StatDef stat, BodyPartRecord part)
        {
            if (!apparel.def.apparel.CoversBodyPart(part))
            {
                return 0;
            }

            float result = apparel.GetStatValue(stat);

            if (Controller.settings.PartialStat)
            {
                if (apparel.def.HasModExtension<PartialArmorExt>())
                {
                    foreach (ApparelPartialStat partial in apparel.def.GetModExtension<PartialArmorExt>().stats)
                    {
                        if ((partial?.parts?.Contains(part.def) ?? false) | ((partial?.parts?.Contains(part?.parent?.def) ?? false) && part.depth == BodyPartDepth.Inside))
                        {

                            if (partial.staticValue > 0f)
                            {
                                return partial.staticValue;
                            }
                            result *= partial.mult;
                            break;

                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the true rating of armor with partial stats taken into account
        /// </summary>
        public static float PartialStat(this Pawn pawn, StatDef stat, BodyPartRecord part, float damage = 0f, float AP = 0f)
        {
            float result = pawn.GetStatValue(stat);

            if (Controller.settings.PartialStat)
            {
                if (pawn.def.HasModExtension<PartialArmorExt>())
                {
                    foreach (ApparelPartialStat partial in pawn.def.GetModExtension<PartialArmorExt>().stats)
                    {
                        if (partial.stat == stat)
                        {
                            if ((partial?.parts?.Contains(part.def) ?? false) | ((partial?.parts?.Contains(part?.parent?.def) ?? false) && part.depth == BodyPartDepth.Inside))
                            {

                                if (partial.staticValue > 0f)
                                {
                                    return partial.staticValue;
                                }
                                result *= partial.mult;
                                break;

                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// version of PartialStat used for display in StatWorker_ArmorPartial
        /// </summary>
        public static float PartialStat(this Apparel apparel, StatDef stat, BodyPartDef part)
        {
            float result = apparel.GetStatValue(stat);
            if (apparel.def.HasModExtension<PartialArmorExt>())
            {
                foreach (ApparelPartialStat partial in apparel.def.GetModExtension<PartialArmorExt>().stats)
                {
                    if ((partial?.parts?.Contains(part) ?? false))
                    {

                        if (partial.staticValue > 0f)
                        {
                            return partial.staticValue;
                        }
                        result *= partial.mult;
                        break;

                    }
                }
            }
            return result;
        }

        /// <summary>
        /// version of PartialStat used for display in StatWorker_ArmorPartial
        /// </summary>
        public static float PartialStat(this Pawn pawn, StatDef stat, BodyPartDef part)
        {
            float result = pawn.GetStatValue(stat);
            if (pawn.def.HasModExtension<PartialArmorExt>())
            {
                foreach (ApparelPartialStat partial in pawn.def.GetModExtension<PartialArmorExt>().stats)
                {
                    if ((partial?.parts?.Contains(part) ?? false))
                    {
                        if (partial.staticValue > 0f)
                        {
                            return partial.staticValue;
                        }
                        result *= partial.mult;
                        break;

                    }
                }
            }
            return result;
        }

        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        public static void UpdateLabel(this Def def, string label)
        {
            def.label = label;
            def.cachedLabelCap = "";
        }

        /// <summary>
        /// Generates a random Vector2 in a circle with given radius
        /// </summary>
        public static Vector2 GenRandInCircle(float radius)
        {
            //Fancy math to get random point in circle
            double angle = Rand.Value * Math.PI * 2;
            double range = Rand.Value * radius;
            return new Vector2((float)(range * Math.Cos(angle)), (float)(range * Math.Sin(angle)));
        }

        /// <summary>
        /// Calculates the actual current movement speed of a pawn
        /// </summary>
        /// <param name="pawn">Pawn to calculate speed of</param>
        /// <returns>Move speed in cells per second</returns>
        public static float GetMoveSpeed(Pawn pawn)
        {
            if (!pawn.pather.Moving)
            {
                return 0f;
            }
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false);    //Movement per tick

            //pawn.pather.nextCellCostLeft
            //the orginial is (pawn.Position, false, pawn.Position) before 1.3

            movePerTick += pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(pawn.pather.nextCell, perceivedStatic: false, pawn.Position);
            Building edifice = pawn.Position.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                movePerTick += (int)edifice.PathWalkCostFor(pawn);
            }

            //Case switch to handle walking, jogging, etc.
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        movePerTick *= 3;
                        if (movePerTick < 60)
                        {
                            movePerTick = 60;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        movePerTick *= 2;
                        if (movePerTick < 50)
                        {
                            movePerTick = 50;
                        }
                        break;
                    case LocomotionUrgency.Jog:
                        break;
                    case LocomotionUrgency.Sprint:
                        movePerTick = Mathf.RoundToInt(movePerTick * 0.75f);
                        break;
                }
            }
            return 60 / movePerTick;
        }

        public static float GetLightingShift(Thing caster, float glowAtTarget)
        {
            return Mathf.Max((1.0f - glowAtTarget) * (1.0f - caster.GetStatValue(CE_StatDefOf.NightVisionEfficiency)), 0f);
        }

        public static float ClosestDistBetween(Vector2 origin, Vector2 destination, Vector2 target)
        {
            return Mathf.Abs((destination.y - origin.y) * target.x - (destination.x - origin.x) * target.y + destination.x * origin.y - destination.y * origin.x) / (destination - origin).magnitude;
        }

        public static float DistanceBetweenTiles(int firstTile, int endTile, int maxCells = 500)
        {
            return Find.WorldGrid.TraversalDistanceBetween(firstTile, endTile, true, maxDist: maxCells);
        }

        public static IntVec3 ExitCell(this Ray ray, Map map)
        {
            Vector3 mapSize = map.Size.ToVector3();
            mapSize.y = Mathf.Max(mapSize.x, mapSize.z);
            Bounds mapBounds = new Bounds(mapSize.Yto0() / 2f, mapSize);
            mapBounds.IntersectRay(ray, out float dist);
            Vector3 exitCell = ray.GetPoint(dist);
            exitCell.x = Mathf.Clamp(exitCell.x, 0, mapSize.x - 1);
            exitCell.z = Mathf.Clamp(exitCell.z, 0, mapSize.z - 1);
            exitCell.y = 0;
            return exitCell.ToIntVec3();
        }

        /// <summary>
        /// Attempts to find a turret operator. Accepts any Thing as input and does a sanity check to make sure it is an actual turret.
        /// </summary>
        /// <param name="thing">The turret to check for an operator</param>
        /// <returns>Turret operator if one is found, null if not</returns>
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            // Building_TurretGunCE DOES NOT inherit from Building_TurretGun!!!
            if (thing is Building_Turret)
            {
                CompMannable comp = thing.TryGetComp<CompMannable>();
                if (comp != null)
                {
                    return comp.ManningPawn;
                }
            }
            return null;
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped a shield
        /// </summary>
        /// <returns>True if the pawn has a shield equipped</returns>
        public static bool HasShield(this Pawn pawn)
        {
            if ((pawn.apparel?.WornApparelCount ?? 0) == 0)
            {
                return false;
            }
            return pawn.apparel.WornApparel.Any(a => a is Apparel_Shield);
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped two handed weapon
        /// </summary>
        /// <returns>True if the pawn has equipped a two handed weapon</returns>
        public static bool HasTwoWeapon(this Pawn pawn)
        {
            if (pawn.equipment?.Primary == null)
            {
                return false;
            }
            return !(pawn.equipment.Primary.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped two handed weapon
        /// </summary>
        /// <returns>True if the pawn has equipped a two handed weapon</returns>
        public static bool IsTwoHandedWeapon(this Thing weapon)
        {
            return !(weapon.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
        }

        /// <summary>
        /// Extension method to determine whether a ranged weapon has ammo available to it
        /// </summary>
        /// <returns>True if the gun has no CompAmmoUser, doesn't use ammo or has ammo in its magazine or carrier inventory, false otherwise</returns>
        public static bool HasAmmo(this ThingWithComps gun)
        {
            CompAmmoUser comp = gun.TryGetComp<CompAmmoUser>();
            if (comp == null)
            {
                return true;
            }
            return !comp.UseAmmo || comp.CurMagCount > 0 || comp.HasAmmo;
        }

        public static bool CanBeStabilized(this Hediff diff)
        {
            HediffWithComps hediff = diff as HediffWithComps;
            if (hediff == null)
            {
                return false;
            }
            if (hediff.BleedRate == 0f || hediff.IsTended() || hediff.IsPermanent())
            {
                return false;
            }
            HediffComp_Stabilize comp = hediff.TryGetComp<HediffComp_Stabilize>();
            return comp != null && !comp.Stabilized;
        }

        /// <summary>
        /// Attempts to get the weapon from the equipper of the weapon that launched the projectile
        /// </summary>
        /// <param name="launcher">The equipper of the weapon that launched the projectile</param>
        /// <returns>Weapon if one is found, null if not</returns>
        /*
         * Fundamentally broken - will null ref if launcher pawn drops equipment in-between firing the projectile and it impacting -NIA
        public static Thing GetWeaponFromLauncher(Thing launcher)
        {
            if (launcher is Pawn pawn)
                return pawn.equipment?.Primary;
            if (launcher is Building_TurretGunCE turretCE)
                return turretCE.Gun;
            return null;
        }
        */

        #endregion Misc

        #region MoteThrower
        public static void GenerateAmmoCasings(ProjectilePropertiesCE projProps, Vector3 drawPosition, Map map, float shotRotation = -180f, float recoilAmount = 2f)
        {
            if (projProps.dropsCasings)
            {
                if (Controller.settings.ShowCasings)
                {
                    ThrowEmptyCasing(drawPosition, map, DefDatabase<FleckDef>.GetNamed(projProps.casingMoteDefname), recoilAmount, shotRotation);
                }
                if (Controller.settings.CreateCasingsFilth)
                {
                    MakeCasingFilth(drawPosition.ToIntVec3(), map, DefDatabase<ThingDef>.GetNamed(projProps.casingFilthDefname));
                }
            }
        }


        public static void ThrowEmptyCasing(Vector3 loc, Map map, FleckDef casingFleckDef, float recoilAmount, float shotRotation, float size = 1f)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            if (recoilAmount <= 0)
            {
                recoilAmount = 1; //avoid division errors in case of guns without recoil
            }
            Rand.PushState();
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, casingFleckDef);
            creationData.airTimeLeft = 1.5f;
            creationData.scale = Rand.Range(0.5f, 0.3f) * size;
            creationData.spawnPosition = loc;
            creationData.velocitySpeed = Rand.Range(0.6f, 0.4f) * recoilAmount;
            int randomAngle = Rand.Range(-20, 20);
            //shotRotation goes from -270 to +90, while fleck angle uses 0 to 360 degrees (0 deg being North for both cases), so a conversion is used
            //+90 makes casings fly to gun's right side
            creationData.velocityAngle = shotRotation > 0 ? 360 - shotRotation + 90 + randomAngle : 0 - shotRotation + 90 + randomAngle;
            creationData.rotation = creationData.velocityAngle + Rand.Range(-3f, 4f);
            creationData.rotationRate = (float)Rand.Range(-150, 150) / recoilAmount;
            map.flecks.CreateFleck(creationData);
            Rand.PopState();
        }

        public static void MakeCasingFilth(IntVec3 position, Map map, ThingDef casingFilthDef)
        {
            Rand.PushState();
            float makeFilthChance = Rand.Range(0f, 1f);
            if (makeFilthChance > 0.9f && position.Walkable(map))
            {
                FilthMaker.TryMakeFilth(position, map, casingFilthDef, 1, FilthSourceFlags.None);
            }
            Rand.PopState();
        }

        public static void MakeIconOverlay(Pawn pawn, ThingDef moteDef)
        {
            MoteThrownAttached moteThrown = (MoteThrownAttached)ThingMaker.MakeThing(moteDef);
            moteThrown.Attach(pawn);
            moteThrown.exactPosition = pawn.DrawPos;
            moteThrown.Scale = 1.0f;
            moteThrown.SetVelocity(Rand.Range(20f, 25f), 0.4f);
            GenSpawn.Spawn(moteThrown, pawn.Position, pawn.Map);
        }
        #endregion

        #region Physics
        /// <summary>
        /// Gravity constant in meters per second squared
        /// </summary>
        public const float GravityConst = 9.8f * 0.2f;

        public static Bounds GetBoundsFor(IntVec3 cell, RoofDef roof)
        {
            if (roof == null)
            {
                return new Bounds();
            }

            float height = CollisionVertical.WallCollisionHeight;

            if (roof.isNatural)
            {
                height *= CollisionVertical.NaturalRoofThicknessMultiplier;
            }

            if (roof.isThickRoof)
            {
                height *= CollisionVertical.ThickRoofThicknessMultiplier;
            }

            height = Mathf.Max(0.1f, height - CollisionVertical.WallCollisionHeight);

            Vector3 center = cell.ToVector3Shifted();
            center.y = CollisionVertical.WallCollisionHeight + height / 2f;

            return new Bounds(center,
                              new Vector3(1f, height, 1f));
        }

        public static Bounds GetBoundsFor(Thing thing)
        {
            if (thing == null)
            {
                return new Bounds();
            }
            var height = new CollisionVertical(thing);
            float length;
            float width;
            var thingPos = thing.DrawPos;
            thingPos.y = height.Max - height.HeightRange.Span / 2;
            if (thing is Building)
            {
                IntVec2 rotatedSize = thing.RotatedSize;
                length = rotatedSize.x;
                width = rotatedSize.z;
            }
            else
            {
                width = GetCollisionWidth(thing);
                length = width;
            }
            Bounds bounds = new Bounds(thingPos, new Vector3(length, height.HeightRange.Span, width));
            return bounds;
        }

        /// <summary>
        /// Calculates the width of an object for purposes of bullet collision. Return value is distance from center of object to its edge in cells, so a wall filling out an entire cell has a width of 0.5.
        /// Also accounts for general body type, humanoids must be specified in the humanoidBodyList and will have reduced width relative to their overall body size.
        /// </summary>
        /// <param name="thing">The Thing to measure width of</param>
        /// <returns>Distance from center of Thing to its edge in cells</returns>
        public static float GetCollisionWidth(Thing thing)
        {
            /* Possible solution for fixing tree widths
            if (thing.IsTree())
            {
                return (thing as Plant).def.graphicData.shadowData.volume.x;
            }*/

            var pawn = thing as Pawn;
            if (pawn != null)
            {
                return GetCollisionBodyFactors(pawn).x;
            }

            return 1f;    //Buildings, etc. fill out a full square
        }

        /// <summary>
        /// Calculates whether a line segment intercepts a radius circle. Used to determine if a projectile crosses a shield.
        /// </summary>
        /// <param name="center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="pointA">The first point of the line segment</param>
        /// <param name="pointB">The second point of the line segment</param>
        /// <returns>True if the line segment intercepts the circle</returns>
        public static bool IntersectLineSphericalOutline(Vector3 center, float radius, Vector3 pointA, Vector3 pointB)
        {
            var radSq = radius * radius;
            var aInside = ((center - pointA).sqrMagnitude <= radSq);
            var bInside = ((center - pointB).sqrMagnitude <= radSq);
            if (aInside != bInside) // One end is inside, one is outside -- we crossed
            {
                return true;
            }
            if (aInside && bInside) // Both are inside, we did not cross
            {
                return false;
            }

            // Both are outside, so check if the point on the line-segment AB closest to point C is inside.

            Vector3 direction = pointB - pointA;
            Vector3 displacement = pointA - center;

            // Calculate the dot product between the relative position at the start and the direction of travel.
            float dotProduct = displacement.x * direction.x + displacement.y * direction.y + displacement.z * direction.z;

            if (dotProduct < 0) // Moving *away* from the shield
            {
                return false;
            }

            if (dotProduct > direction.sqrMagnitude) // Still moving closer, might hit next tick but it hasn't arrived.  We don't need to check if the end point is inside, because we already did that.
            {
                return false;
            }

            // The center lies some distance perpindicular to the line segment AB
            // Rotate the direction vector 90 degrees, and find its dot product with the relative displacement to find that distance.
            dotProduct = displacement.x * direction.z - displacement.z * direction.x + displacement.y * direction.y;

            if (dotProduct * dotProduct <= direction.sqrMagnitude * displacement.sqrMagnitude)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates whether a line segment intercepts a radius circle. Used to get intersection points.
        /// </summary>
        /// <param name="p1">The first point of the line segment</param>
        /// <param name="p2">The second point of the line segment</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <returns><code>Vector3[] { Vector3.zero, Vector3.zero }</code> if there's no intersection, othrewise returns two intersection points</returns>
        public static Vector3[] IntersectionPoint(Vector3 p1, Vector3 p2, Vector3 center, float radius)
        {
            Vector3 dp = new Vector3();
            Vector3[] sect;
            float a, b, c;
            float bb4ac;
            float mu1;
            float mu2;

            //  get the distance between X and Z on the segment
            dp.x = p2.x - p1.x;
            dp.z = p2.z - p1.z;
            //   I don't get the math here
            a = dp.x * dp.x + dp.z * dp.z;
            b = 2 * (dp.x * (p1.x - center.x) + dp.z * (p1.z - center.z));
            c = center.x * center.x + center.z * center.z;
            c += p1.x * p1.x + p1.z * p1.z;
            c -= 2 * (center.x * p1.x + center.z * p1.z);
            c -= radius * radius;
            bb4ac = b * b - 4 * a * c;
            if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
            {
                //  line does not intersect
                return new Vector3[] { Vector3.zero, Vector3.zero };
            }
            var sqrtbb4ac = Mathf.Sqrt(bb4ac);
            mu1 = (-b + sqrtbb4ac) / (2 * a);
            mu2 = (-b - sqrtbb4ac) / (2 * a);
            sect = new Vector3[2];
            sect[0] = new Vector3(p1.x + mu1 * (p2.x - p1.x), 0, p1.z + mu1 * (p2.z - p1.z));
            sect[1] = new Vector3(p1.x + mu2 * (p2.x - p1.x), 0, p1.z + mu2 * (p2.z - p1.z));

            return sect;
        }
        /// <summary>
        /// Calculates body scale factors based on body type
        /// </summary>
        /// <param name="pawn">Which pawn to measure for</param>
        /// <returns>Width factor as First, height factor as second</returns>
        public static Vector2 GetCollisionBodyFactors(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn");
                return new Vector2(1, 1);
            }

            Vector2 factors;
            if (Compatibility.Patches.GetCollisionBodyFactors(pawn, out factors))
            {
                return factors;
            }

            factors = BoundsInjector.ForPawn(pawn);

            if (pawn.GetPosture() != PawnPosture.Standing || pawn.Downed)
            {
                RacePropertiesExtensionCE props = pawn.def.GetModExtension<RacePropertiesExtensionCE>() ?? new RacePropertiesExtensionCE();

                var shape = props.bodyShape;

                if (shape == CE_BodyShapeDefOf.Invalid)
                {
                    Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn.ToString(), 35000198 + pawn.GetHashCode());
                }

                factors.x *= shape.widthLaying / shape.width;
                factors.y *= shape.heightLaying / shape.height;
                if (pawn.Downed)
                {
                    factors.y *= shape.heightLaying;
                }
            }

            return factors;
        }

        /// <summary>
        /// Determines whether a pawn should be currently crouching down or not
        /// </summary>
        /// <returns>True for humanlike pawns currently doing a job during which they should be crouching down</returns>
        public static bool IsCrouching(this Pawn pawn)
        {
            return pawn.RaceProps.Humanlike && !pawn.Downed && (pawn.CurJob?.def.GetModExtension<JobDefExtensionCE>()?.isCrouchJob ?? false);
        }

        public static bool IsPlant(this Thing thing)
        {
            return thing.def.category == ThingCategory.Plant;
        }

        public static float MaxProjectileRange(float shotHeight, float shotSpeed, float shotAngle, float gravityFactor)
        {
            //Fragment at 0f height early opt-out
            if (shotHeight < 0.001f)
            {
                return (Mathf.Pow(shotSpeed, 2f) / gravityFactor) * Mathf.Sin(2f * shotAngle);
            }
            return ((shotSpeed * Mathf.Cos(shotAngle)) / gravityFactor) * (shotSpeed * Mathf.Sin(shotAngle) + Mathf.Sqrt(Mathf.Pow(shotSpeed * Mathf.Sin(shotAngle), 2f) + 2f * gravityFactor * shotHeight));
        }

        #endregion Physics

        #region Inventory

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn != null)
            {
                CompInventory comp = pawn.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    comp.UpdateInventory();
                }
            }
        }

        public static void TryUpdateInventory(ThingOwner owner)
        {
            Pawn pawn = owner?.Owner?.ParentHolder as Pawn;
            if (pawn != null)
            {
                TryUpdateInventory(pawn);
            }
        }

        /// <summary>
        /// Get all weapons a pawn has.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="weapons">Weapons</param>
        /// <param name="rebuild">(Slow) wether to rebuild the cache</param>
        /// <returns>If this pawn has a CompInventory or not</returns>
        public static bool TryGetAllWeaponsInInventory(this Pawn_InventoryTracker inventoryTracker, out IEnumerable<ThingWithComps> weapons, Func<ThingWithComps, bool> predicate = null, bool rebuildInvetory = false)
        {
            Pawn pawn = inventoryTracker.pawn;
            weapons = null;
            CompInventory compInventory = pawn.TryGetComp<CompInventory>();
            // check is this pawn has a CompInventory
            if (compInventory == null)
            {
                return false;
            }
            if (rebuildInvetory)
            {
                compInventory.UpdateInventory();
            }
            // Add all weapons in the inventory
            weapons = (predicate == null ? compInventory.weapons : compInventory.weapons.Where(w => predicate.Invoke(w)));
            return true;
        }

        #endregion

        #region Lighting

        /// <summary>
        /// Used to get the lighting penalty multiplier for a given range
        /// </summary>
        /// <returns></returns>
        public static float LightingRangeMultiplier(float range)
        {
            return lightingCurve.Evaluate(range);
        }

        private static Map[] _mapsLighting = new Map[20];
        private static LightingTracker[] _lightingTrackers = new LightingTracker[20];

        public static LightingTracker GetLightingTracker(this Map map)
        {
            int index = map?.Index ?? -1;
            if (index < 0)
            {
                return null;
            }
            if (index >= _mapsLighting.Length)
            {
                int expandedLength = Mathf.Max(_mapsLighting.Length * 2, index + 1);
                Map[] maps = new Map[expandedLength];
                LightingTracker[] trackers = new LightingTracker[expandedLength];
                Array.Copy(_mapsLighting, maps, _mapsLighting.Length);
                Array.Copy(_lightingTrackers, trackers, _lightingTrackers.Length);
                _mapsLighting = maps;
                _lightingTrackers = trackers;
            }
            if (_mapsLighting[index] == map)
            {
                return _lightingTrackers[index];
            }
            return _lightingTrackers[index] = (_mapsLighting[index] = map).GetComponent<LightingTracker>();
        }

        private static Map[] _mapsDanger = new Map[20];
        private static DangerTracker[] _DangerTrackers = new DangerTracker[20];

        public static DangerTracker GetDangerTracker(this Map map)
        {
            int index = map?.Index ?? -1;
            if (index < 0)
            {
                return null;
            }
            if (index >= _mapsDanger.Length)
            {
                int expandedLength = Mathf.Max(_mapsDanger.Length * 2, index + 1);
                Map[] maps = new Map[expandedLength];
                DangerTracker[] trackers = new DangerTracker[expandedLength];
                Array.Copy(_mapsDanger, maps, _mapsDanger.Length);
                Array.Copy(_DangerTrackers, trackers, _DangerTrackers.Length);
                _mapsDanger = maps;
                _DangerTrackers = trackers;
            }
            if (_mapsDanger[index] == map)
            {
                return _DangerTrackers[index];
            }
            return _DangerTrackers[index] = (_mapsDanger[index] = map).GetComponent<DangerTracker>();
        }

        #endregion

        #region Initialization

        private static readonly SimpleCurve lightingCurve = new SimpleCurve();

        static CE_Utility()
        {
            lightingCurve.Add(05.00f, 0.05f);
            lightingCurve.Add(10.00f, 0.15f);
            lightingCurve.Add(22.00f, 0.475f);
            lightingCurve.Add(35.00f, 1.00f);
            lightingCurve.Add(60.00f, 1.20f);
            lightingCurve.Add(90.00f, 2.00f);
        }

        #endregion

        public static float DistanceToSegment(this Vector3 point, Vector3 lineStart, Vector3 lineEnd, out Vector3 closest)
        {
            float dx = lineEnd.x - lineStart.x;
            float dz = lineEnd.z - lineStart.z;
            if ((dx == 0) && (dz == 0))
            {
                closest = lineStart;
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
                return Mathf.Sqrt(dx * dx + dz * dz);
            }
            float t = ((point.x - lineStart.x) * dx + (point.z - lineStart.z) * dz) / (dx * dx + dz * dz);
            if (t < 0)
            {
                closest = new Vector3(lineStart.x, 0, lineStart.z);
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
            }
            else if (t > 1)
            {
                closest = new Vector3(lineEnd.x, 0, lineEnd.z);
                dx = point.x - lineEnd.x;
                dz = point.z - lineEnd.z;
            }
            else
            {
                closest = new Vector3(lineStart.x + t * dx, 0, lineStart.z + t * dz);
                dx = point.x - closest.x;
                dz = point.z - closest.z;
            }
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static Vector3 ToVec3Gridified(this Vector3 originalVec3)
        {
            Vector2 tempVec2 = new Vector2(originalVec3.normalized.x, originalVec3.normalized.z);
            float factor = Math.Max(Mathf.Abs(tempVec2.x), Mathf.Abs(tempVec2.y));
            // If factor <= 0.6f, something has definitely gone wrong (or the vector is a zero vector);
            if (factor <= 0.6f)
            {
                return originalVec3;
            }
            //Log.Warning("ToVec3Gridified " + (new Vector3(originalVec3.x / highestNormalCoord, originalVec3.y, originalVec3.z / highestNormalCoord)).ToString());
            return new Vector3(originalVec3.x / factor, originalVec3.y, originalVec3.z / factor);
        }

        public static object LaunchProjectileCE(ThingDef projectileDef,
                                                Vector2 origin,
                                                LocalTargetInfo target,
                                                Thing shooter,
                                                float shotAngle,
                                                float shotRotation,
                                                float shotHeight,
                                                float shotSpeed)
        {
            projectileDef = projectileDef.GetProjectile();
            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(projectileDef, null);
            GenSpawn.Spawn(projectile, shooter.Position, shooter.Map);

            projectile.ExactPosition = origin;
            projectile.canTargetSelf = false;
            projectile.minCollisionDistance = 1;
            projectile.intendedTarget = target;
            projectile.mount = null;
            projectile.AccuracyFactor = 1;


            projectile.Launch(
                shooter,
                origin,
                shotAngle,
                shotRotation,
                shotHeight,
                shotSpeed,
                shooter);
            return projectile;
        }

        public static ThingDef GetProjectile(this ThingDef thingDef)
        {
            if (thingDef.projectile != null)
            {
                return thingDef;
            }
            if (thingDef is AmmoDef ammoDef)
            {
                ThingDef user;
                if ((user = ammoDef.Users.FirstOrFallback(null)) != null)
                {
                    CompProperties_AmmoUser props = user.GetCompProperties<CompProperties_AmmoUser>();
                    AmmoSetDef asd = props.ammoSet;
                    AmmoLink ammoLink;
                    if ((ammoLink = asd.ammoTypes.FirstOrFallback(null)) != null)
                    {
                        return ammoLink.projectile;
                    }
                }
                else
                {
                    return ammoDef.detonateProjectile;
                }
            }
            return thingDef;
        }

        public static void DamageOutsideSquishy(DamageWorker_AddInjury __instance, DamageInfo dinfo, Pawn pawn, float totalDamage, DamageWorker.DamageResult result, float lastHitPartHealth)
        {
            var hitPart = dinfo.HitPart;

            if (dinfo.Def != DamageDefOf.SurgicalCut && dinfo.Def != DamageDefOf.ExecutionCut && hitPart.IsInGroup(CE_BodyPartGroupDefOf.OutsideSquishy))
            {
                var parent = hitPart.parent;
                if (parent != null)
                {
                    float hitPartHealth = lastHitPartHealth;
                    if (hitPartHealth > totalDamage)
                    {
                        return;
                    }

                    dinfo.SetHitPart(parent);
                    float parentPartHealth = pawn.health.hediffSet.GetPartHealth(parent);
                    if (parentPartHealth != 0f && parent.coverageAbs > 0f)
                    {
                        Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent), pawn, null);
                        hediff_Injury.Part = parent;
                        hediff_Injury.source = dinfo.Weapon;
                        hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
                        hediff_Injury.Severity = totalDamage - (hitPartHealth * hitPartHealth / totalDamage);
                        if (hediff_Injury.Severity <= 0f)
                        {
                            hediff_Injury.Severity = 1f;
                        }
                        __instance.FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
                    }
                }
            }
        }

        private static readonly List<PawnKindDef> _validPawnKinds = new List<PawnKindDef>();

        public static Pawn GetRandomWorldPawn(this Faction faction, bool capableOfCombat = true)
        {
            Pawn pawn = Find.World.worldPawns.AllPawnsAlive.Where(p => p.Faction == faction && (!capableOfCombat || p.kindDef.isFighter || p.kindDef.isGoodBreacher)).RandomElementWithFallback();
            if (pawn != null)
            {
                return pawn;
            }
            Log.Warning($"CE: Couldn't find world pawns for faction {faction}. CE had to create a new one..");
            _validPawnKinds.Clear();
            foreach (PawnGroupMaker group in capableOfCombat ? faction.def.pawnGroupMakers.Where((PawnGroupMaker x) => x.kindDef == PawnGroupKindDefOf.Combat) : faction.def.pawnGroupMakers)
            {
                foreach (PawnGenOption option in group.options)
                {
                    _validPawnKinds.Add(option.kind);
                }
            }
            if (faction.def.fixedLeaderKinds != null)
            {
                _validPawnKinds.AddRange(faction.def.fixedLeaderKinds);
            }
            if (_validPawnKinds.TryRandomElement(out var result))
            {
                PawnGenerationRequest request = new PawnGenerationRequest(result, faction, PawnGenerationContext.NonPlayer, -1, faction.def.leaderForceGenerateNewPawn);
                Gender gender = faction.ideos.PrimaryIdeo.SupremeGender;
                if (gender != 0)
                {
                    request.FixedGender = gender;
                }
                pawn = PawnGenerator.GeneratePawn(request);
                if (pawn.RaceProps.IsFlesh)
                {
                    pawn.relations.everSeenByPlayer = true;
                }
                if (!Find.WorldPawns.Contains(pawn))
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                }
            }
            return pawn;
        }

        public static FactionStrengthTracker GetStrengthTracker(this Faction faction) => Find.World.GetComponent<WorldStrengthTracker>().GetFactionTracker(faction);
    }
}
