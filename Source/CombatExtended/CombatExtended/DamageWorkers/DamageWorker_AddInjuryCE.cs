using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    /// <summary>
    /// Custom damage worker using new armor system, cloned from DamageWorker_AddInjury
    /// </summary>
	public class DamageWorker_AddInjuryCE : DamageWorker
	{
        private const BindingFlags UniversalBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
 
        private struct LocalInjuryResult
		{
			public bool wounded;
			public bool headshot;
            public bool deflected;
			public bool absorbed;
			public BodyPartRecord lastHitPart;
			public float totalDamageDealt;

			public static LocalInjuryResult MakeNew()
			{
				return new LocalInjuryResult
				{
					wounded = false,
					headshot = false,
                    deflected = false,
					absorbed = false,
					lastHitPart = null,
					totalDamageDealt = 0f
				};
			}
		}

		private const float SpreadDamageChance = 0.5f;

        private static Func<BodyPartRecord, float> cache0;      // Populated by CheckPropagateDamageToInnerSolidParts, returns absoluteFleshCoverage of a BodyPartRecord
        private static Func<LifeStageAge, SoundDef> cache1;     // Populated by PlayWoundedVoiceSound, returns soundWounded of LifeStageAge

		public override float Apply(DamageInfo dinfo, Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null)
			{
				return base.Apply(dinfo, thing);
			}
			return ApplyToPawn(dinfo, pawn);
		}

		private float ApplyToPawn(DamageInfo dinfo, Pawn pawn)
		{
			if (dinfo.Amount <= 0)
			{
				return 0f;
			}
			if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
			{
				return 0f;
			}
            LocalInjuryResult localInjuryResult = LocalInjuryResult.MakeNew();
            Map mapHeld = pawn.MapHeld;
			if (dinfo.Def.spreadOut)
			{
				if (pawn.apparel != null)
				{
					List<Apparel> wornApparel = pawn.apparel.WornApparel;
					for (int i = wornApparel.Count - 1; i >= 0; i--)
					{
						CheckApplySpreadDamage(dinfo, wornApparel[i]);
					}
				}
				if (pawn.equipment != null && pawn.equipment.Primary != null)
				{
					CheckApplySpreadDamage(dinfo, pawn.equipment.Primary);
				}
				if (pawn.inventory != null)
				{
                    ThingContainer innerContainer = pawn.inventory.innerContainer;
                    for (int j = innerContainer.Count - 1; j >= 0; j--)
                    {
                        this.CheckApplySpreadDamage(dinfo, innerContainer[j]);
                    }
                }
			}
			if (!FragmentDamageForDamageType(dinfo, pawn, ref localInjuryResult))
			{
				ApplyDamagePartial(dinfo, pawn, ref localInjuryResult);
				CheckDuplicateSmallPawnDamageToPartParent(dinfo, pawn, ref localInjuryResult);
			}
			if (localInjuryResult.wounded)
			{
				PlayWoundedVoiceSound(dinfo, pawn);
				pawn.Drawer.Notify_DamageApplied(dinfo);
				InformPsychology(dinfo, pawn);
			}
			if (localInjuryResult.headshot && pawn.Spawned)
			{
				MoteMaker.ThrowText(new Vector3((float)pawn.Position.x + 1f, (float)pawn.Position.y, (float)pawn.Position.z + 1f), pawn.Map, "Headshot".Translate(), Color.white, -1);
				if (dinfo.Instigator != null)
				{
					Pawn pawn2 = dinfo.Instigator as Pawn;
					if (pawn2 != null)
					{
						pawn2.records.Increment(RecordDefOf.Headshots);
					}
				}
			}
            if (localInjuryResult.absorbed && pawn.Spawned)
            {
				if (pawn.health.deflectionEffecter == null)
				{
					pawn.health.deflectionEffecter = EffecterDef.Named("ArmorRating").Spawn();
				}
				pawn.health.deflectionEffecter.Trigger(pawn, pawn);
			}
			else if (mapHeld != null)
            {
                ImpactSoundUtility.PlayImpactSound(pawn, dinfo.Def.impactSoundType, mapHeld);
            }
			return localInjuryResult.totalDamageDealt;
		}

		private void CheckApplySpreadDamage(DamageInfo dinfo, Thing t)
		{
            if (UnityEngine.Random.value < 0.5f && t != null)
            {
                dinfo.SetAmount(Mathf.CeilToInt((float)dinfo.Amount * Rand.Range(0.35f, 0.7f)));
                t.TakeDamage(dinfo);
            }
		}

		private bool FragmentDamageForDamageType(DamageInfo dinfo, Pawn pawn, ref LocalInjuryResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return false;
			}
			if (dinfo.Amount < 9)
			{
				return false;
			}
			if (!dinfo.Def.spreadOut)
			{
				return false;
			}
			int num = Rand.RangeInclusive(3, 4);
			for (int i = 0; i < num; i++)
			{
				DamageInfo dinfo2 = dinfo;
				dinfo2.SetAmount(dinfo.Amount / num);
				ApplyDamagePartial(dinfo2, pawn, ref result);
			}
			return true;
		}

        private void CheckDuplicateSmallPawnDamageToPartParent(DamageInfo dinfo, Pawn pawn, ref LocalInjuryResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return;
			}
			if (result.lastHitPart != null
                && dinfo.Def.harmsHealth
                && result.lastHitPart != pawn.RaceProps.body.corePart
                && result.lastHitPart.parent != null
                && pawn.health.hediffSet.GetPartHealth(result.lastHitPart.parent) > 0f
                && dinfo.Amount >= 10
                && pawn.HealthScale <= 0.5001f)
			{
                DamageInfo dinfo2 = dinfo;
                dinfo2.SetForcedHitPart(result.lastHitPart.parent); //vanilla
                this.ApplyDamagePartial(dinfo2, pawn, ref result);
            }
		}

		private void ApplyDamagePartial(DamageInfo dinfo, Pawn pawn, ref LocalInjuryResult result, bool shotDeflected = false)
		{
			BodyPartRecord exactPartFromDamageInfo = GetExactPartFromDamageInfo(dinfo, pawn);
			if (exactPartFromDamageInfo == null)
			{
				return;
            }

            // Only apply armor if we propagate damage to the outside or the body part itself is outside, secondary damage types should directly damage organs, bypassing armor
            bool involveArmor = !dinfo.InstantOldInjury 
                && !shotDeflected
                && (dinfo.Def.harmAllLayersUntilOutside || exactPartFromDamageInfo.depth == BodyPartDepth.Outside);
			int damageAmount = dinfo.Amount;

			if (involveArmor)
            {
                damageAmount = CE_Utility.GetAfterArmorDamage(pawn, dinfo.Amount, exactPartFromDamageInfo, dinfo, true, ref shotDeflected);
            }
			if ((double)damageAmount < 0.001)
			{
				result.absorbed = true;
				return;
            }

            // Shot absorbed and converted into blunt
            DamageDef_CE damageDefCE = dinfo.Def as DamageDef_CE;
            if (damageDefCE != null 
                && damageDefCE.deflectable 
                && shotDeflected
                && dinfo.Def != CE_Utility.absorbDamageDef)
            {
                // Get outer parent of struck part
                BodyPartRecord currentPart = exactPartFromDamageInfo;
                while (currentPart != null && currentPart.parent != null && currentPart.depth != BodyPartDepth.Outside)
                {
                    currentPart = currentPart.parent;
                }
                DamageInfo dinfo2 = new DamageInfo(CE_Utility.absorbDamageDef, damageAmount, dinfo.Angle, dinfo.Instigator, currentPart, dinfo.WeaponGear);
                ApplyDamagePartial(dinfo2, pawn, ref result, true);
                return;
            }

            //Creating the Hediff
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, exactPartFromDamageInfo);
			Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
			hediff_Injury.Part = exactPartFromDamageInfo;
			hediff_Injury.source = dinfo.WeaponGear;
			hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
			hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
			hediff_Injury.Severity = (float)damageAmount;
			if (dinfo.InstantOldInjury)
			{
				HediffComp_GetsOld hediffComp_GetsOld = hediff_Injury.TryGetComp<HediffComp_GetsOld>();
				if (hediffComp_GetsOld != null)
				{
					hediffComp_GetsOld.IsOld = true;
				}
				else
				{
					Log.Error(string.Concat(new object[]
					{
						"Tried to create instant old injury on Hediff without a GetsOld comp: ",
						hediffDefFromDamage,
						" on ",
						pawn
					}));
				}
			}
			result.wounded = true;
			result.lastHitPart = hediff_Injury.Part;
			if (IsHeadshot(dinfo, hediff_Injury, pawn))
			{
				result.headshot = true;
			}
			if (dinfo.InstantOldInjury && (hediff_Injury.def.CompPropsFor(typeof(HediffComp_GetsOld)) == null || hediff_Injury.Part.def.oldInjuryBaseChance == 0f || hediff_Injury.Part.def.IsSolid(hediff_Injury.Part, pawn.health.hediffSet.hediffs) || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(hediff_Injury.Part)))
			{
				return;
			}
			FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, ref result);
			CheckPropagateDamageToInnerSolidParts(dinfo, pawn, hediff_Injury, !dinfo.InstantOldInjury, damageAmount, ref result);
			CheckDuplicateDamageToOuterParts(dinfo, pawn, hediff_Injury, !dinfo.InstantOldInjury, damageAmount, ref result);
		}

		private void FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, ref LocalInjuryResult result)
		{
			CalculateOldInjuryDamageThreshold(pawn, injury);
			result.totalDamageDealt += Mathf.Min(injury.Severity, pawn.health.hediffSet.GetPartHealth(injury.Part));
			pawn.health.AddHediff(injury, null, new DamageInfo?(dinfo));
		}

		private void CalculateOldInjuryDamageThreshold(Pawn pawn, Hediff_Injury injury)
		{
			HediffCompProperties_GetsOld hediffCompProperties = injury.def.CompProps<HediffCompProperties_GetsOld>();
            if (hediffCompProperties == null)
			{
				return;
			}
			if (injury.Part.def.IsSolid(injury.Part, pawn.health.hediffSet.hediffs) || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(injury.Part) || injury.IsOld() || injury.Part.def.oldInjuryBaseChance < 1E-05f)
			{
				return;
			}
			bool isDelicate = injury.Part.def.IsDelicate;
			if ((Rand.Value <= injury.Part.def.oldInjuryBaseChance * hediffCompProperties.becomeOldChance && injury.Severity >= injury.Part.def.GetMaxHealth(pawn) * 0.25f && injury.Severity >= 7f) || isDelicate)
			{
				HediffComp_GetsOld hediffComp_GetsOld = injury.TryGetComp<HediffComp_GetsOld>();
				float num = 1f;
				float num2 = injury.Severity / 2f;
				if (num <= num2)
				{
					hediffComp_GetsOld.oldDamageThreshold = Rand.Range(num, num2);
				}
				if (isDelicate)
				{
					hediffComp_GetsOld.oldDamageThreshold = injury.Severity;
					hediffComp_GetsOld.IsOld = true;
				}
			}
		}

		private void CheckPropagateDamageToInnerSolidParts(DamageInfo dinfo, Pawn pawn, Hediff_Injury injury, bool involveArmor, int postArmorDamage, ref LocalInjuryResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return;
			}
			if (Rand.Value >= HealthTunings.ChanceToAdditionallyDamageInnerSolidPart)
			{
				return;
			}
			if (dinfo.Def.hasChanceToAdditionallyDamageInnerSolidParts && !injury.Part.def.IsSolid(injury.Part, pawn.health.hediffSet.hediffs) && injury.Part.depth == BodyPartDepth.Outside)
			{
				IEnumerable<BodyPartRecord> enumerable = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined).Where(x => x.parent == injury.Part && x.def.IsSolid(x, pawn.health.hediffSet.hediffs) && x.depth == BodyPartDepth.Inside);
                BodyPartRecord part;
                if (cache0 == null)
                {
                    cache0 = delegate(BodyPartRecord x) { return x.absoluteFleshCoverage; };
                }
                if (enumerable.TryRandomElementByWeight(cache0, out part))
				{
					HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, part);
					Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
					hediff_Injury.Part = part;
					hediff_Injury.source = injury.source;
					hediff_Injury.sourceBodyPartGroup = injury.sourceBodyPartGroup;
					hediff_Injury.Severity = (float)(dinfo.Amount / 2);
					if (involveArmor)
					{
						hediff_Injury.Severity = (float)postArmorDamage;
                    }
					if (hediff_Injury.Severity <= 0f)
					{
						return;
					}
					result.lastHitPart = hediff_Injury.Part;
					FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, ref result);
				}
			}
		}

		private void CheckDuplicateDamageToOuterParts(DamageInfo dinfo, Pawn pawn, Hediff_Injury injury, bool involveArmor, int postArmorDamage, ref LocalInjuryResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return;
			}
			if (dinfo.Def.harmAllLayersUntilOutside && injury.Part.depth == BodyPartDepth.Inside)
			{
				BodyPartRecord parent = injury.Part.parent;
				do
				{
					if (pawn.health.hediffSet.GetPartHealth(parent) != 0f)
					{
						HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent);
						Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
						hediff_Injury.Part = parent;
						hediff_Injury.source = injury.source;
						hediff_Injury.sourceBodyPartGroup = injury.sourceBodyPartGroup;
						hediff_Injury.Severity = (float)dinfo.Amount;
						if (involveArmor)
                        {
                            hediff_Injury.Severity = (float)postArmorDamage;
                        }
						if (hediff_Injury.Severity <= 0f)
						{
							hediff_Injury.Severity = 1f;
						}
						result.lastHitPart = hediff_Injury.Part;
						FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, ref result);
					}
					if (parent.depth == BodyPartDepth.Outside)
					{
						break;
					}
					parent = parent.parent;
				}
				while (parent != null);
			}
		}

		private static void InformPsychology(DamageInfo dinfo, Pawn pawn)
		{
		}

		private static bool IsHeadshot(DamageInfo dinfo, Hediff_Injury injury, Pawn pawn)
		{
			return !dinfo.InstantOldInjury && injury.Part.groups.Contains(BodyPartGroupDefOf.FullHead) && dinfo.Def == DamageDefOf.Bullet;
		}

		internal static BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
		{
            if (dinfo.ForceHitPart != null)
            {
                return (from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        where x == dinfo.ForceHitPart
                        select x).FirstOrDefault<BodyPartRecord>();
            }
            BodyPartRecord randomNotMissingPart = pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
            if (randomNotMissingPart == null)
            {
                Log.Warning("GetRandomNotMissingPart returned null (any part).");
            }
            return randomNotMissingPart;
        }

		private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
		{
			if (pawn.Dead)
			{
				return;
			}
			if (dinfo.InstantOldInjury)
			{
				return;
			}
			if (dinfo.Def.externalViolence)
			{
				if (cache1 == null)
				{
					cache1 = delegate(LifeStageAge ls) { return ls.soundWounded; };
				}
				LifeStageUtility.PlayNearestLifestageSound(pawn, cache1);
			}
		}
	}
}
