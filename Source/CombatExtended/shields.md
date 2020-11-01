# Making (area) shields stop CE Projectiles
If you want to stop CE Projectiles in flight, there are a couple extra steps 
relative to stopping normal projectiles.  First, CE Projectiles are not instances
of the normal projectile class, so scanning the map for projectiles will miss 
them.  Second, the fastest projectiles travel more than 1 Cell/tick, so if you
check the position of projectiles once a tick, some will leak through.  To 
facilitate detecting and stopping CE Projectiles, you can register a callback which
runs each time a CE projectile moves.

### Registration API

#### BlockerRegistry.RegisterCheckForCollisionCallback
This function registers a callback which direct fire projectiles call to check if a 
cell is "solid". You'll want to track either the projectile's origin, or it's previous
position in order to tell if it crossed an arbitrary line.
The function you register must have the signature `bool FuncName(ProjectileCE, IntVec3, Thing)`.
If you want to change where the projectile "dies", change its `ExactPosition` before returning;
Return `true` if the projectile should be removed.

#### BlockerRegistry.RegisterImpactSomethingCallback
This function registers a callback which indirect fire projectiles call before exploding/hitting 
something (the ground if nothing else). You'll want to track either the projectile's origin, and
its current position in order to tell if it crossed an arbitrary line.
The function you register must have the signature `bool FuncName(ProjectileCE, Thing)`.
Return `true` if the projectile should be removed.

#### Tip for performance
There are *lots* of CE projectiles flying during a major combat.  You want to take reasonable
steps to make your callbacks efficient.  **Do not** iterate all things on the map in each 
invocation.  Instead, if you need to know what shields are on the map, either register/unregister
them on spawn/despawn, or iterate the map once per tick on the first projectile and cache the result
until the next tick.

#### Example
Here is a trivial example.  If you need a more complete example, check out the 
examples in `Source/CombatExtended/Compatibility`.

        using CombatExtended.Compatibility;
        namespace example {
                [StaticConstructorOnStartup]
            class Example {
                        static Example() {
                                BlockerRegistry.RegisterCheckForCollisionCallback(CheckForCollision);
                                BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomethingCallback);
                        }
                static bool CheckForCollision(ProjectileCE p, IntVec3 cell, Thing launcher) {
                            return false;
                        }
                        static bool ImpactSomethingCallback(ProjectileCE p, Thing launcher) {
                            return false;
                        }
                }
        }

