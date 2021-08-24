using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public interface IReloadable
    {        
        /// <summary>
        /// The ammoset used by this reloadable
        /// </summary>
        public abstract AmmoSetDef AmmoSet
        {
            get;
        }        
        /// <summary>
        /// Returns the current number of projectiles in the magazine.
        /// </summary>
        public abstract int CurMagCount
        {
            get;
            set;
        }
        /// <summary>
        /// Returns the currently selected ammo.
        /// </summary>
        public abstract AmmoDef SelectedAmmo
        {
            get;
            set;
        }
        /// <summary>
        /// Returns the current ammo in the magazine.
        /// </summary>
        public abstract AmmoDef CurrentAmmo
        {
            get;
        }       
        /// <summary>
        /// Return the magazine size.
        /// </summary>
        public abstract int MagSize
        {
            get;
        }
        /// <summary>
        /// Return wether magazine is empty.
        /// </summary>
        public abstract bool MagazineFull
        {
            get;
        }
        /// <summary>
        /// Return wether the current magazine is empty.
        /// </summary>
        public abstract bool MagazineEmpty
        {
            get;
        }
        /// <summary>
        /// Return wether ammo is in the inventory of the parent pawn.
        /// </summary>
        public abstract bool HasAmmo
        {
            get;
        }       
        /// <summary>
        /// Return wether this is capable of firing now
        /// </summary>
        public abstract bool CanBeFiredNow
        {
            get;
        }
        /// <summary>
        /// Return available ammo
        /// </summary>
        public abstract IEnumerable<ThingDefCount> AvailableAmmoDefs
        {
            get;
        }
        /// <summary>
        /// Indicate to GUI wether this should have unloading actions
        /// </summary>
        public abstract bool IsEquippedGun
        {
            get;
        }       
        /// <summary>
        /// Try to start a reload job using the selected ammo and if no selected ammo is available use any ammo
        /// </summary>
        /// <returns>Wether was able to start a reload job</returns>
        public abstract bool TryStartReload();
        
        /// <summary>
        /// Try unload the weapon and either carry the ammo or drop it.
        /// </summary>
        /// <param name="dropUnloadedAmmo">Wether to drop the ammo on the ground after unloading.</param>
        /// <returns>Wether weapon was unloaded or not.</returns>
        public abstract bool TryUnload(bool forceUnload = false);
    }
}
