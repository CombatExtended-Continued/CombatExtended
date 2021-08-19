namespace CombatExtended.Utilities
{
    public enum TrackedThingsRequestCategory
    {
        /// <summary>
        /// Defs that have Pawn as thingClass
        /// </summary>
        Pawns = 1,

        /// <summary>
        /// Defs that are AmmoDef
        /// </summary>
        Ammo = 2,

        /// <summary>
        /// Defs that have ThingDef.IsWeapon return true 
        /// </summary>
        Weapons = 4,

        /// <summary>
        /// Defs that have ThingDef.IsApparel return true 
        /// </summary>
        Apparel = 8,

        /// <summary>
        /// Defs that have ThingDef.IsMedicine return true 
        /// </summary>
        Medicine = 16,

        /// <summary>
        /// Defs that have ThingDef.IsMedicine return true 
        /// </summary>
        Flares = 32,

        /// <summary>
        /// Defs that have ThingDef.IsMedicine return true 
        /// </summary>
        Attachments = 64
    }
}
