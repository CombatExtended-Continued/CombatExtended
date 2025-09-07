using CombatExtended.CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class Building_CIWS_CE : Building_Turret_MultiVerbs
{
    #region Caching

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<TurretTracker>().Register(this);
    }
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetComponent<TurretTracker>()?.Unregister(this);
        base.DeSpawn(mode);
    }
    #endregion

    private List<ThingDef> ignoredDefs = new List<ThingDef>();
    public IEnumerable<ThingDef> IgnoredDefsSettings
    {
        get
        {
            return ignoredDefs ??= new List<ThingDef>();
        }
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref ignoredDefs, nameof(ignoredDefs));
    }
    static Texture2D icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (Controller.settings.EnableCIWS && this.Faction == Faction.OfPlayer)
        {
            yield return new Command_Action()
            {
                action = () => Find.WindowStack.Add(new Dialog_ManageCIWSTargets(GunCompEq.AllVerbs.OfType<VerbCIWS>().SelectMany(x => x.Props.AllTargets).Distinct().ToList(), ignoredDefs)),
                icon = icon,
                defaultLabel = "Dialog_ManageCIWS".Translate(),
                defaultDesc = "Dialog_ManageCIWSDesc".Translate()
            };
        }
    }

    public override void Tick()
    {
        base.Tick();
        if (CurrentTarget.IsValid && CurrentTarget.HasThing)
        {
            this.top.CurRotation = (CurrentTarget.Thing.DrawPos - this.DrawPos).AngleFlat();
        }
    }
}
