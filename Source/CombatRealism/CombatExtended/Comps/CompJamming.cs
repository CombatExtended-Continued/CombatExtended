using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompJamming : ThingComp
    {
        public CompProperties_Jamming Props
        {
            get
            {
                return (CompProperties_Jamming)props;
            }
        }

        private Verb verbInt = null;
        private Verb verb
        {
            get
            {
                if (verbInt == null)
                {
                    CompEquippable compEquippable = parent.TryGetComp<CompEquippable>();
                    if (compEquippable != null)
                    {
                        verbInt = compEquippable.PrimaryVerb;
                    }
                    else
                    {
                        Log.ErrorOnce(parent.LabelCap + " has CompJamming but no CompEquippable.", 50010);
                    }
                }
                return verbInt;
            }
        }

        /// <summary>
        /// Returns a factor to scale malfunction chance by quality. If the parent doesn't have a CompQuality it will return a factor of 1.
        /// </summary>
        /// <returns>Quality-based scale factor</returns>
        private float GetQualityFactor()
        {
            CompQuality compQuality = parent.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                switch (compQuality.Quality)
                {
                    case QualityCategory.Awful:
                        return 4f;
                    case QualityCategory.Shoddy:
                        return 3f;
                    case QualityCategory.Poor:
                        return 2f;
                    case QualityCategory.Normal:
                        return 1f;
                    case QualityCategory.Good:
                        return 0.85f;
                    case QualityCategory.Excellent:
                        return 0.7f;
                    case QualityCategory.Superior:
                        return 0.55f;
                    case QualityCategory.Masterwork:
                        return 0.4f;
                    case QualityCategory.Legendary:
                        return 0.25f;
                }
            }
            return 1f;
        }

        public void DoMalfunction()
        {
            float jamChance = Props.baseMalfunctionChance * (1 - parent.HitPoints / parent.MaxHitPoints) * GetQualityFactor();
            float explodeChance = Mathf.Clamp01(jamChance);

            if (Props.canExplode && UnityEngine.Random.value < explodeChance)
            {
                Explode();
            }
            if (UnityEngine.Random.value < jamChance)
            {
                //TODO
            }
        }

        /// <summary>
        /// Causes explosion and destroys parent equipment
        /// </summary>
        private void Explode()
        {
            if (!parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
            // TODO
        }
    }
}
