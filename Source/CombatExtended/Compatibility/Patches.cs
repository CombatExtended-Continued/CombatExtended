using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CombatExtended.Compatibility
{
    class Patches
    {
        private List<IPatch> patches;
        public Patches() {
            patches = new List<IPatch>();
            Type iPatch = typeof(IPatch);
            foreach(Type patchType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                    .Where(x => iPatch.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)) {
                IPatch patch = ((IPatch)patchType.GetConstructor(new Type[]{}).Invoke(new object[]{}));
                patches.Add(patch);
            }
        }

        public void Install() {
            foreach(IPatch patch in patches) {
                if (patch.CanInstall()) {
                    patch.Install();
                }
            }
        }

        public IEnumerable<string> GetCompatList() {
            foreach(IPatch patch in patches) {
                if (patch.CanInstall()) {
                    foreach(string s in patch.GetCompatList()) {
                        yield return s;
                    }
                }
            }
        }
    }
}
