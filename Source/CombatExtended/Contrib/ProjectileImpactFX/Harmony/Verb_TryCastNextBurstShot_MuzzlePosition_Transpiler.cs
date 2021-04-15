using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using Verse.Sound;
using System.Reflection.Emit;
using UnityEngine;

namespace ProjectileImpactFX.HarmonyInstance
{

    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    public static class Verb_TryCastNextBurstShot_MuzzlePosition_Transpiler
    {

	public static bool Oversized = false;
        public static void ThrowMuzzleFlash(IntVec3 cell, Map map, ThingDef moteDef, float scale, Verb verb)
        {
            if (verb.EquipmentSource != null)
            {
                if (verb.verbProps.range > 1.48f)
                {
                    ThingDef mote = moteDef;
                    Vector3 origin = verb.CasterIsPawn ? verb.CasterPawn.Drawer.DrawPos : verb.Caster.DrawPos;
                    CompEquippable equippable = verb.EquipmentSource.TryGetComp<CompEquippable>();
                    float aimAngle = (verb.CurrentTarget.CenterVector3 - origin).AngleFlat();
                    if (verb.EquipmentSource.def.HasModExtension<BarrelOffsetExtension>())
                    {
                        BarrelOffsetExtension ext = verb.EquipmentSource.def.GetModExtension<BarrelOffsetExtension>();
                        EffectProjectileExtension ext2 = verb.GetProjectile().HasModExtension<EffectProjectileExtension>() ? verb.GetProjectile().GetModExtension<EffectProjectileExtension>() : null;
                        float offset = ext.barrellength;
                        origin += (verb.CurrentTarget.CenterVector3 - origin).normalized * (verb.EquipmentSource.def.graphic.drawSize.magnitude * (offset));
                        if (ext2 != null && ext2.muzzleFlare)
                        {
                            ThingDef muzzleFlaremote = DefDatabase<ThingDef>.GetNamed(!ext2.muzzleSmokeDef.NullOrEmpty() ? ext2.muzzleFlareDef : "Mote_SparkFlash");
                            MoteMaker.MakeStaticMote(origin, map, muzzleFlaremote, ext2.muzzleFlareSize);
                        }
                        else if (ext.muzzleFlare)
                        {
                            ThingDef muzzleFlaremote = DefDatabase<ThingDef>.GetNamed(!ext.muzzleSmokeDef.NullOrEmpty() ? ext.muzzleFlareDef : "Mote_SparkFlash");
                            MoteMaker.MakeStaticMote(origin, map, muzzleFlaremote, ext.muzzleFlareSize);
                        }
                        if (ext2 != null && ext2.muzzleSmoke)
                        {
                            string muzzleSmokemote = !ext2.muzzleSmokeDef.NullOrEmpty() ? ext2.muzzleSmokeDef : "OG_Mote_SmokeTrail";
                            TrailThrower.ThrowSmoke(origin, ext2.muzzleSmokeSize, map, muzzleSmokemote);
                        }
                        else if (ext.muzzleSmoke)
                        {
                            string muzzleSmokemote = !ext.muzzleSmokeDef.NullOrEmpty() ? ext.muzzleSmokeDef : "OG_Mote_SmokeTrail";
                            TrailThrower.ThrowSmoke(origin, ext.muzzleSmokeSize, map, muzzleSmokemote);
                        }
                    }
                    MoteMaker.MakeStaticMote(origin, map, mote, scale);
                    return;
                }
            }

            {
                MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, moteDef, scale);
            }
		}



		public static float meleeXOffset = 0.4f;
		public static float rangedXOffset = 0.1f;
		public static float meleeZOffset = 0f;
		public static float rangedZOffset = 0f;
		public static float meleeAngle = 270f;
		public static bool meleeMirrored = true;
		public static float rangedAngle = 135f;
		public static bool rangedMirrored = true;
		public static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, float aimAngle, Thing thing, ref Vector3 offsetMainHand, ref Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle, bool mainHandAiming, bool offHandAiming)
		{

			Pawn pawn = thing as Pawn;

			bool Melee = pawn != null;
			if (Melee)
			{
				Melee = Verb_TryCastNextBurstShot_MuzzlePosition_Transpiler.IsMeleeWeapon(pawn.equipment.Primary);
			}

			bool Dual = false;

			float num = meleeMirrored ? (360f - meleeAngle) : meleeAngle;
			float num2 = rangedMirrored ? (360f - rangedAngle) : rangedAngle;
			Vector3 offset = AdjustRenderOffsetFromDir(thing.Rotation, null, offHandAiming);
			if (thing.Rotation == Rot4.East)
			{
				offsetMainHand.z += offset.z;
				offsetMainHand.x += offset.x;
				offsetOffHand.y = -1f;
				offsetOffHand.z = 0.1f;
				offsetOffHand.z += offset.z;
				offsetOffHand.x += offset.x;
				offHandAngle = mainHandAngle;
			}
			else
			{
				if (thing.Rotation == Rot4.West)
				{
					if (Dual) offsetMainHand.y = -1f;
					offsetMainHand.z += offset.z;
					offsetMainHand.x += offset.x;
					offsetOffHand.z = -0.1f;
					offsetOffHand.z += offset.z;
					offsetOffHand.x += offset.x;
					offHandAngle = mainHandAngle;
				}
				else
				{
					if (thing.Rotation == Rot4.North)
					{
						if (!mainHandAiming)
						{
							offsetMainHand.x = offset.x + (Dual ? (Melee ? meleeXOffset : rangedXOffset) : 0);
							offsetOffHand.x = -offset.x + (Melee ? -meleeXOffset : -rangedXOffset);
							offsetMainHand.z = offset.z + (Dual ? (Melee ? meleeZOffset : rangedZOffset) : 0);
							offsetOffHand.z = offset.z + (Melee ? meleeZOffset : rangedZOffset);
						}
						else
						{
							offsetOffHand.x = -0.1f;
						}
					}
					else
					{
						if (!mainHandAiming)
						{
							offsetMainHand.y = 1f;
							offsetMainHand.x = -offset.x + (Dual ? (Melee ? -meleeXOffset : -rangedXOffset) : 0);
							offsetOffHand.x = offset.x + (Melee ? meleeXOffset : rangedXOffset);
							offsetMainHand.z = offset.z + (Dual ? (Melee ? meleeZOffset : rangedZOffset) : 0);
							offsetOffHand.z = offset.z + (Melee ? meleeZOffset : rangedZOffset);
						}
						else
						{
							offsetOffHand.y = 1f;
							offHandAngle = (!Melee ? num : num2);
							offsetOffHand.x = 0.1f;
						}
					}
				}
			}

		}

		private static Vector3 AdjustRenderOffsetFromDir(Rot4 curDir, object compOversizedWeapon, bool Offhand = false)
		{

			Vector3 curOffset = Vector3.zero;



			return curOffset;
		}
		// Token: 0x06000080 RID: 128 RVA: 0x00006594 File Offset: 0x00004794
		private static bool IsMeleeWeapon(ThingWithComps eq)
		{
			bool flag = eq == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
				bool flag2 = compEquippable != null;
				if (flag2)
				{
					bool isMeleeAttack = compEquippable.PrimaryVerb.IsMeleeAttack;
					if (isMeleeAttack)
					{
						return true;
					}
				}
				result = false;
			}
			return result;
		}
	}

}
