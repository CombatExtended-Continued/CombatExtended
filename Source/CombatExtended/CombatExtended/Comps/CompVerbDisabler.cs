using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended;
public class CompVerbDisabler : ThingComp
{

    private bool _holdFire = false;
    private IVerbDisableable _disableableVerbCache;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _holdFire, "holdFire", false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            GrabInterface();
        }
    }
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        GrabInterface();
    }

    private void GrabInterface()
    {
        List<Verb> verbs = parent.GetComp<CompEquippable>()?.AllVerbs;
        if (verbs == null)
        {
            return;
        }
        foreach (var verb in verbs)
        {
            if (verb is not IVerbDisableable disableableVerb)
            {
                continue;
            }
            _disableableVerbCache = disableableVerb;
            _disableableVerbCache.HoldFire = _holdFire;
        }
    }

    private void ToggleHoldFire()
    {
        _disableableVerbCache.HoldFire = !_disableableVerbCache.HoldFire;
        _holdFire = _disableableVerbCache.HoldFire;

    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (!Controller.settings.EnableCIWS)
        {
            yield break;
        }
        if (_disableableVerbCache == null)
        {
            yield break;
        }
        var command = new Command_Toggle()
        {
            defaultDesc = _disableableVerbCache.HoldFireDesc.Translate(),
            defaultLabel = _disableableVerbCache.HoldFireLabel.Translate(),
            icon = _disableableVerbCache.HoldFireIcon,
            isActive = () => _disableableVerbCache.HoldFire,
            toggleAction = ToggleHoldFire,

        };
        yield return command;
    }
}

