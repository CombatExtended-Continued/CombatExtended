using System;
using System.IO;
using Verse;

namespace CombatExtended
{
    class LoadLoadoutDialog : FileListDialog
    {
        internal LoadLoadoutDialog(string storageTypeName, Action<FileInfo, FileListDialog> fileAction) : base(storageTypeName, fileAction)
        {
            base.interactButLabel = "LoadGameButton".Translate();
        }

        protected override bool ShouldDoTypeInField => false;
    }

}
