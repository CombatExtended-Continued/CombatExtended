using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class PatchOperationFindMod : PatchOperation
    {
        protected string modName;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (modName.NullOrEmpty())
            {
                return false;
            }
            return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == modName);
        }
    }
}
