using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace CombatExtended.WorldObjects
{
    public class HealthComp : WorldObjectComp, IWorldCompCE
    {
        public const float HEALTH_HEALRATE_DAY = 0.15f;

        private int lastTick = -1;
        private int _configTick = -1;
        private float health = 1.0f;
        private float negateChance = 0f;
        private float armorDamageMultiplier = 1f;
        private bool destroyedInstantly = false;
        public List<WorldDamageInfo> recentShells = new List<WorldDamageInfo>();
        public float Health
        {
            get => health;
            set => health = Mathf.Clamp01(value);
        }
        public virtual float TotalDamageRequired
        {
            get => DestoyedInstantly ? 1f : 100 * ArmorDamageMultiplier;
        }
        public virtual float DamageRequired
        {
            get => TotalDamageRequired * health;
        }
        public virtual float HealingRatePerTick
        {
            get => parent.Faction != null ? ((int)parent.Faction.def.techLevel) / 4f * HEALTH_HEALRATE_DAY / 60000f : 0f;
        }

        public virtual float ArmorDamageMultiplier
        {
            get
            {
                UpdateCacheValues();
                return armorDamageMultiplier;
            }
            protected set => armorDamageMultiplier = value;
        }
        public virtual float NegateChance
        {
            get
            {
                UpdateCacheValues();
                return negateChance;
            }
            protected set => negateChance = Mathf.Clamp01(value);
        }
        public virtual bool DestoyedInstantly
        {
            get
            {
                UpdateCacheValues();
                return destroyedInstantly;
            }
            protected set => destroyedInstantly = value;
        }

        public WorldObjectCompProperties_Health Props
        {
            get => props as WorldObjectCompProperties_Health;
        }
        public virtual void UpdateCacheValues()
        {
            if (_configTick == GenTicks.TicksGame)
            {
                return;
            }
            _configTick = GenTicks.TicksGame;
            if (recentShells == null)
            {
                recentShells = new List<WorldDamageInfo>();
            }

            bool techLevelNoImpact = Props.techLevelNoImpact;
            ArmorDamageMultiplier = 1f;
            NegateChance = 0f;
            DestoyedInstantly = Props?.destoyedInstantly ?? false;

            if (Props.healthModifier > 0f)
            {
                ArmorDamageMultiplier *= Props.healthModifier;
            }

            var factionExtension = parent.Faction?.def.GetModExtension<WorldObjectHealthExtension>();

            if (factionExtension != null)
            {
                if (factionExtension.healthModifier > 0f)
                {
                    ArmorDamageMultiplier *= factionExtension.healthModifier;
                }
                if (factionExtension.chanceToNegateDamage >= 0f)
                {
                    NegateChance = factionExtension.chanceToNegateDamage;
                }
                techLevelNoImpact |= factionExtension.techLevelNoImpact;
                DestoyedInstantly |= factionExtension.destoyedInstantly;

            }


            if (parent is Site site)
            {
                foreach (var sitePart in site.parts)
                {
                    var sitePartExtension = sitePart?.def?.GetModExtension<WorldObjectHealthExtension>();
                    if (sitePartExtension != null)
                    {
                        if (sitePartExtension.healthModifier > 0f)
                        {
                            ArmorDamageMultiplier *= sitePartExtension.healthModifier;
                        }
                        if (sitePartExtension.chanceToNegateDamage >= 0f)
                        {
                            NegateChance = sitePartExtension.chanceToNegateDamage;
                        }
                        techLevelNoImpact |= sitePartExtension.techLevelNoImpact;
                        DestoyedInstantly |= sitePartExtension.destoyedInstantly;
                    }

                }
            }

            if (!techLevelNoImpact)
            {
                ArmorDamageMultiplier *= (parent.Faction != null ? Mathf.Max((float)parent.Faction.def.techLevel, 1f) : 1f);
            }
        }
        public HealthComp()
        {
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref health, "health", 1.0f);
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
            Scribe_Collections.Look<WorldDamageInfo>(ref recentShells, nameof(recentShells), lookMode: LookMode.Deep);
        }

        public virtual void ThrottledCompTick()
        {
            float oldHealth = Health;
            Health += HealingRatePerTick * WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
            if (Health != oldHealth)
            {
                var decrease = Health - oldHealth;
                while (recentShells.Any() && decrease > 0f)
                {
                    var oldestShell = recentShells.First();
                    var value = Mathf.Min(decrease, oldestShell.Value);
                    oldestShell.Value -= value;
                    decrease -= value;
                    if (oldestShell.Value <= 0f)
                    {
                        recentShells.Remove(oldestShell);
                    }
                }
            }
        }

        /// <summary>
        /// Clean up quests associated with a world object and update ideology raiding state.
        /// </summary>
        /// <param name="attackingFaction">The faction that destroyed this world object via intertile shelling.</param>
        /// <param name="sourceInfo">The tile the shelling originated from.</param>
        protected virtual void TryFinishDestroyQuests(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
            Map launcherMap = sourceInfo.Map;

            QuestUtility.SendQuestTargetSignals(parent.questTags, "AllEnemiesDefeated", parent.Named("SUBJECT"), new NamedArgument(launcherMap, "MAP"));
            int num;
            List<Quest> quests = Find.QuestManager.QuestsListForReading;
            for (int i = 0; i < quests.Count; i = num + 1)
            {
                Quest quest = quests[i];
                if (!quest.Historical && quest.QuestLookTargets.Contains(this.parent))
                {
                    Find.SignalManager.SendSignal(new Signal($"Quest{quest.id}.conditionCauser.Destroyed", new NamedArgument(launcherMap, "MAP")));
                }
                num = i;
            }

            foreach (var quest in RelatedQuests)
            {
                quest.End(QuestEndOutcome.Fail);
            }

            if (attackingFaction == Faction.OfPlayer && Find.Maps.Contains(launcherMap))
            {
                IdeoUtility.Notify_PlayerRaidedSomeone(launcherMap.mapPawns.FreeColonistsSpawned);
            }
        }

        IEnumerable<Quest> RelatedQuests => Find.QuestManager.QuestsListForReading.Where(x => !x.Historical && x.QuestLookTargets.Contains(parent));
        public void ApplyDamage(ThingDef shellDef, Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
            if (Rand.Chance(NegateChance))
            {
                return;
            }
            if (DestoyedInstantly)
            {
                TryFinishDestroyQuests(attackingFaction, sourceInfo);
                TryDestroy();
                return;
            }
            var damage = shellDef.GetWorldObjectDamageWorker().ApplyDamage(this, shellDef);
            recentShells.Add(new WorldDamageInfo() { Value = damage, ShellDef = shellDef });
            Notify_DamageTaken(attackingFaction, sourceInfo);
        }


        void TryDestroy()
        {
            if (!parent.Destroyed)
            {
                parent.Destroy();
            }
        }
        public virtual void Notify_DamageTaken(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
            if (health <= 1e-4)
            {
                TryFinishDestroyQuests(attackingFaction, sourceInfo);
                Notify_PreDestroyed(attackingFaction, sourceInfo);
                Destroy();
                return;
            }
        }

        public virtual void Notify_PreDestroyed(Faction attackingFaction, GlobalTargetInfo sourceInfo)
        {
            foreach (var turret in Find.Maps.SelectMany(x => x.GetComponent<TurretTracker>().Turrets).Where(x => x.Faction == attackingFaction && x is Building_TurretGunCE).Cast<Building_TurretGunCE>().Where(x => (x.globalTargetInfo.WorldObject?.tileInt ?? -2) == parent.tileInt))
            {
                turret.ResetForcedTarget();
                turret.ResetCurrentTarget();
            }
        }
        public virtual void Destroy(Faction attackingFaction = null)
        {
            int tile = parent.Tile;
            Faction faction = parent.Faction;
            FactionStrengthTracker tracker = faction.GetStrengthTracker();
            if (parent is Settlement settlement)
            {
                string message;
                if (faction == null)
                {
                    message = "CE_Message_SettlementDestroyed_Description".Translate().Formatted(parent.Label);
                }
                else
                {
                    if (attackingFaction != null)
                    {
                        message = "CE_Message_SettlementDestroyed_Description_Responsibility".Translate(attackingFaction.Name, parent.Label, faction.Name);
                    }
                    else
                    {
                        message = "CE_Message_SettlementDestroyed_Faction_Description".Translate().Formatted(parent.Label, faction.Name);
                    }
                }
                Find.LetterStack.ReceiveLetter("CE_Message_SettlementDestroyed_Label".Translate(), message, LetterDefOf.NeutralEvent);
                TryDestroy();
                if (tracker != null)
                {
                    tracker.Notify_SettlementDestroyed();
                }
            }
            else
            {
                string message = null;
                if (faction == null)
                {
                    message = "CE_Message_SiteDestroyed".Translate().Formatted(parent.Label);
                }
                else
                {
                    message = "CE_Message_SiteDestroyed_Faction".Translate().Formatted(parent.Label, faction.Name);
                }
                Messages.Message(message, MessageTypeDefOf.SituationResolved);
                if (tracker != null && parent is Site)
                {
                    tracker.Notify_SiteDestroyed();
                }
                TryDestroy();
            }
            if (faction != null && faction.def.humanlikeFaction && attackingFaction != null && attackingFaction != faction) // check the projectile faction
            {
                FactionRelation relation = faction.RelationWith(attackingFaction, true);
                if (relation == null)
                {
                    faction.TryMakeInitialRelationsWith(attackingFaction);
                    relation = faction.RelationWith(attackingFaction, true);
                }
                faction.TryAffectGoodwillWith(attackingFaction, -100, true, true, HistoryEventDefOf.DestroyedEnemyBase, null);
            }
        }
        public override string CompInspectStringExtra()
        {
            if (DamageRequired != TotalDamageRequired || Prefs.DevMode)
            {
                StringBuilder builder = new StringBuilder($"{DamageRequired:0}/{TotalDamageRequired:0} HP");
                if (Prefs.DevMode && recentShells.Any())
                {
                    builder.Append('\n');
                    builder.Append(string.Join("\n", recentShells.Select(x => $"{x.ShellDef.defName} = {x.Value}")));
                }
                return builder.ToString();
            }
            return base.CompInspectStringExtra();
        }
        public class WorldDamageInfo : IExposable
        {
            private float value;
            private ThingDef shellDef;
            public float Value { get => value; set => this.value = value; }
            public ThingDef ShellDef { get => shellDef; set => shellDef = value; }

            public void ExposeData()
            {
                Scribe_Defs.Look(ref shellDef, nameof(ShellDef));
                Scribe_Values.Look(ref value, nameof(Value));
            }
        }
    }
}

