﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{

    public class RacePropertiesExtensionCE : DefModExtension
    {
        public BodyShapeDef bodyShape;
        public bool canParry = false;
        public int maxParry = 1;
    }
}
