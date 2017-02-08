using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
	public class TurretTopCE
	{
        #region Constants

        private const float IdleTurnDegreesPerTick = 0.26f;
		private const int IdleTurnDuration = 140;
		private const int IdleTurnIntervalMin = 150;
		private const int IdleTurnIntervalMax = 350;

        #endregion

        #region Fields

        private Building_Turret parentTurret;
		private float curRotationInt;
		private int ticksUntilIdleTurn;
		private int idleTurnTicksLeft;
		private bool idleTurnClockwise;

        #endregion

        private float CurRotation
		{
			get
			{
				return curRotationInt;
			}
			set
			{
				curRotationInt = value;
				if (curRotationInt > 360f)
				{
					curRotationInt -= 360f;
				}
				if (curRotationInt < 0f)
				{
					curRotationInt += 360f;
				}
			}
		}

        public TurretTopCE(Building_Turret ParentTurret)
		{
			parentTurret = ParentTurret;
		}

		public void TurretTopTick()
		{
			LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
			if (currentTarget.IsValid)
			{
				float curRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
				CurRotation = curRotation;
				ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
			}
			else if (ticksUntilIdleTurn > 0)
			{
				ticksUntilIdleTurn--;
				if (ticksUntilIdleTurn == 0)
				{
					if (Rand.Value < 0.5f)
					{
						idleTurnClockwise = true;
					}
					else
					{
						idleTurnClockwise = false;
					}
					idleTurnTicksLeft = 140;
				}
			}
			else
			{
				if (idleTurnClockwise)
				{
					CurRotation += 0.26f;
				}
				else
				{
					CurRotation -= 0.26f;
				}
				idleTurnTicksLeft--;
				if (idleTurnTicksLeft <= 0)
				{
					ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
				}
			}
		}

		public void DrawTurret()
        {
            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 vec = new Vector3(1, 1, 1);
            Material topMat = parentTurret.def.building.turretTopMat;
            if (topMat.mainTexture.height >= 256 || topMat.mainTexture.width >= 256)
            {
                vec.x = 2;
                vec.z = 2;
            }
            matrix.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, CurRotation.ToQuat(), vec);
            Graphics.DrawMesh(MeshPool.plane20, matrix, parentTurret.def.building.turretTopMat, 0);
        }
    }
}
