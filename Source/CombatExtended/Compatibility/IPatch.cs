using System.Collections.Generic;
namespace CombatExtended.Compatibility {
    public interface IPatch
    {
	public bool CanInstall();
	public void Install();
	public IEnumerable<string> GetCompatList();
    }
}
