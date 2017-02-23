using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.AI;

namespace CombatExtended
{
    public class CompSquadBrain : ThingComp
    {
        private bool canSuppress = false;
        private bool canFlank = false;
        private bool canSnipe = false;
        private bool canSap = false;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            UpdatePawnRoles();
        }

        public void UpdatePawnRoles()
        {
            // TODO Update based on available equipment in inventory
        }

        public bool CanDoRole(CombatRole role)
        {
            switch (role)
            {
                case CombatRole.Sapper:
                    return canSap;
                case CombatRole.Flanker:
                    return canFlank;
                case CombatRole.Sniper:
                    return canSnipe;
                case CombatRole.Suppressor:
                    return canSuppress;
                default:
                    return true;
            }
        }
    }
}
