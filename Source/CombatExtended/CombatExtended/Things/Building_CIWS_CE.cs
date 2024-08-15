using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Building_CIWS_CE : Building_TurretGunCE
    {
        IEnumerable<CompCIWS> ciws;
        public IEnumerable<CompCIWS> CIWS => ciws ??= this.GetComps<CompCIWS>().ToList();

        public override Verb AttackVerb
        {
            get
            {
                foreach (var ciws in CIWS)
                {
                    if (ciws.HasTarget)
                    {
                        var verb = ciws.Verb;
                        if (verb != null)
                        {
                            return verb;
                        }
                    }
                }
                return base.AttackVerb;
            }
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (var verb in GunCompEq.AllVerbs.Except(AttackVerb))
            {
                float range = verb.verbProps.range;
                if (range < 90f)
                {
                    GenDraw.DrawRadiusRing(base.Position, range);
                }
                float num = verb.verbProps.EffectiveMinRange(true);
                if (num < 90f && num > 0.1f)
                {
                    GenDraw.DrawRadiusRing(base.Position, num);
                }
            }

        }
    }
}
