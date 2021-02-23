using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    public class Command_ReloadArmor : Command_Action
    {
        public CompReloadable compReloadable;

        public override bool GroupsWith(Gizmo other)
        {
            var order = other as Command_ReloadArmor;
            return order != null;
        }

        public override void ProcessInput(Event ev)
        {
            if (compReloadable == null)
            {
                Log.Error("Command_ReloadArmor without reloadable comp");
                return;
            }

            else if (compReloadable.RemainingCharges < compReloadable.MaxCharges)
            {
                base.ProcessInput(ev);
            }

        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
		yield break;
            }
        }


    }
}
