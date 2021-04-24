using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.Lasers
{
    class SpinningLaserGun : SpinningLaserGunBase
    {
        bool IsBrusting(Pawn pawn)
        {
            if (pawn.CurrentEffectiveVerb == null) return false;
            return pawn.CurrentEffectiveVerb.Bursting;
        }

        public override void UpdateState()
        {
            var holder = ParentHolder as Pawn_EquipmentTracker;
            if (holder == null) return;

            Stance stance = holder.pawn.stances.curStance;
            Stance_Warmup warmup;

            switch (state)
            {
                case State.Idle:
                    warmup = stance as Stance_Warmup;
                    if (warmup != null)
                    {
                        state = State.Spinup;
                        ReachRotationSpeed(def.rotationSpeed, warmup.ticksLeft);
                    }
                    break;
                case State.Spinup:
                    if (IsBrusting(holder.pawn))
                    {
                        state = State.Spinning;
                    }
                    else
                    {
                        warmup = stance as Stance_Warmup;
                        if (warmup == null)
                        {
                            state = State.Idle;
                            ReachRotationSpeed(0.0f, 30);
                        }
                    }
                    break;
                case State.Spinning:
                    if (!IsBrusting(holder.pawn))
                    {
                        state = State.Idle;
                        Stance_Cooldown cooldown = stance as Stance_Cooldown;
                        if (cooldown != null)
                            ReachRotationSpeed(0.0f, cooldown.ticksLeft);
                        else
                            ReachRotationSpeed(0.0f, 0);
                    }
                    break;
            }
        }
    }
}
