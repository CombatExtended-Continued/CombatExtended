﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Utility;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Grammar;
using UnityEngine;

namespace CombatExtended;
public class Verb_LaunchProjectileStaticCE : Verb_LaunchProjectileCE
{
    public override bool MultiSelect
    {
        get
        {
            return true;
        }
    }

    public override Texture2D UIIcon
    {
        get
        {
            return TexCommand.Attack;
        }
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        return base.ValidateTarget(target, showMessages) && ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, EquipmentSource, true);
    }

    [Compatibility.Multiplayer.SyncMethod]
    public override void OrderForceTarget(LocalTargetInfo target)
    {
        Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic, target);
        job.verbToUse = this;
        CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}
