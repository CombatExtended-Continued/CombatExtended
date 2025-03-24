﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;


namespace CombatExtended.WorldObjects
{
    public class HostilityComp : WorldObjectComp, IWorldCompCE
    {
        private int lastRaidTick = -1;
        private int lastTick = -1;

        public HostilitySheller sheller;
        public HostilityRaider raider;

        #region Cache

        private int _configTick = -1;
        private float _shellingPropability;
        private float _raidMTBDays;
        private float _raidPropability;
        private List<ShellingResponseDef.ShellingResponsePart_Projectile> _availableProjectiles;

        #endregion

        #region ResponseConfigParameters        

        public virtual float RaidPropability
        {
            get
            {
                UpdateCachedConfig();
                return _raidPropability;
            }
        }
        public virtual float RaidMTBDays
        {
            get
            {
                UpdateCachedConfig();
                return _raidMTBDays;
            }
        }
        public virtual float ShellingPropability
        {
            get
            {
                UpdateCachedConfig();
                return _shellingPropability;
            }
        }
        public virtual List<ShellingResponseDef.ShellingResponsePart_Projectile> AvailableProjectiles
        {
            get
            {
                UpdateCachedConfig();
                return _availableProjectiles;
            }
        }

        #endregion     

        public HostilityComp()
        {
            sheller = new HostilitySheller();
            sheller.comp = this;
            raider = new HostilityRaider();
            raider.comp = this;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
            Scribe_Values.Look(ref lastRaidTick, "lastRaidTick", -1);
            Scribe_Deep.Look(ref sheller, "sheller");
            Scribe_Deep.Look(ref raider, "raider");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (raider == null)
                {
                    raider = new HostilityRaider();
                }
                raider.comp = this;
                if (sheller == null)
                {
                    sheller = new HostilitySheller();
                }
                sheller.comp = this;
            }
        }

        public virtual void ThrottledCompTick()
        {
            sheller.ThrottledTick();
            raider.ThrottledTick();
        }

        /// <summary>
        /// Retaliate against a world object due to hostile shelling.
        /// </summary>
        /// <param name="attackingFaction">The faction to retaliate against.</param>
        /// <param name="sourceInfo">The tile the shelling came from.</param>
        public virtual void TryHostilityResponse(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {

            this.UpdateCachedConfig();
            this.Notify_Shelled(attackingFaction, sourceInfo);
            if (parent is MapParent mapParent && mapParent.HasMap && mapParent.Map != null)
            {
                return;
            }
            if (parent.Faction == null || parent.Faction.IsPlayer || parent.Faction.defeated)
            {
                return;
            }
            Map attackerMap = sourceInfo.Map;
            MapParent attackerMapParent = Find.World.worldObjects.MapParentAt(sourceInfo.Tile);
            if (attackerMap == null)
            {
                if (attackerMapParent != null && attackerMapParent.HasMap && attackerMapParent.Map != null && Find.Maps.Contains(attackerMapParent.Map))
                {
                    attackerMap = attackerMapParent.Map;
                }
            }
            float revengePoints;
            if (attackerMap != null)
            {
                revengePoints = StorytellerUtility.DefaultThreatPointsNow(attackerMap) * Mathf.Max((int)parent.Faction.def.techLevel - 4, 1) / 2f;
                revengePoints = Mathf.Max(500, revengePoints);
            }
            else
            {
                revengePoints = Rand.Range(500f, 500 * Mathf.Max((int)parent.Faction.def.techLevel - 2, 2));
#if DEBUG
                if (Controller.settings.DebugVerbose)
                {
                    Log.Warning("CE: Had to use random values for revengePoints");
                }
#endif
            }
#if DEBUG
            if (Controller.settings.DebugVerbose)
            {
                Log.Warning($"CE: Threat points {revengePoints}");
            }
#endif
            // Only allow retaliation shelling if this faction is hostile to the owner of the site at the source location,
            // in case players shell world objects from other faction maps.
            Faction sourceTileFaction = attackerMapParent?.Faction;
            if (!sheller.Shooting && Rand.Chance(ShellingPropability) && parent.Faction.HostileTo(sourceTileFaction))
            {
                sheller.TryStartShelling(sourceInfo, revengePoints, attackingFaction);
            }
            if (attackerMap != null)
            {
                int raidMTBTicks = (int)(RaidMTBDays * 60000);
                int ticksSinceRaided = lastRaidTick > 0 ? GenTicks.TicksGame - lastRaidTick : raidMTBTicks + 1;
                if (ticksSinceRaided != raidMTBTicks && ticksSinceRaided > raidMTBTicks / 2f && Rand.Chance(RaidPropability / Mathf.Max(raidMTBTicks - ticksSinceRaided, 1)) && raider.TryRaid(attackerMap, revengePoints))
                {
                    lastRaidTick = GenTicks.TicksGame;
                    Messages.Message("CE_Message_CounterRaid".Translate(parent.Label, attackingFaction.Name, attackerMap.Parent.Label), MessageTypeDefOf.ThreatBig);
                }
            }
        }

        public virtual void Notify_Destoyed(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
        }

        public virtual void Notify_Shelled(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
            Faction faction = parent.Faction;
            if (faction != null && attackingFaction != null && attackingFaction != faction) // check the projectile faction
            {
                FactionRelation relation = faction.RelationWith(attackingFaction, true);
                if (relation == null)
                {
                    faction.TryMakeInitialRelationsWith(attackingFaction);
                    relation = faction.RelationWith(attackingFaction, true);
                }
                faction.TryAffectGoodwillWith(attackingFaction, -75, true, true, HistoryEventDefOf.AttackedSettlement, sourceInfo);
            }
        }

        protected virtual void UpdateCachedConfig()
        {
            if (_configTick == GenTicks.TicksGame)
            {
                return;
            }
            ShellingResponseDef shellingResponseDef = parent.Faction.GetShellingResponseDef();
            ShellingResponseDef.ShellingResponsePart_WorldObject objectConfig;
            if ((objectConfig = shellingResponseDef.worldObjects?.FirstOrDefault(w => w.worldObject == parent.def) ?? null) == null)
            {
                _raidPropability = shellingResponseDef.defaultRaidPropability;
                _raidMTBDays = shellingResponseDef.defaultRaidMTBDays;
                _shellingPropability = shellingResponseDef.defaultShellingPropability;
            }
            else
            {
                _raidPropability = objectConfig.raidPropability;
                _raidMTBDays = objectConfig.raidMTBDays;
                _shellingPropability = objectConfig.shellingPropability;
            }
            _configTick = GenTicks.TicksGame;
            _availableProjectiles = shellingResponseDef.projectiles;
        }
    }
}

