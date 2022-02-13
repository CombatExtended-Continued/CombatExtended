using System;
using System.IO;
using Verse;

namespace CombatExtended
{
    class SaveLoadoutDialog : FileListDialog
    {
        internal SaveLoadoutDialog(string storageTypeName, Action<FileInfo, FileListDialog> fileAction, string initialSaveFilename) : base(storageTypeName, fileAction)
        {
            this.interactButLabel = "OverwriteButton".Translate();
            this.typingName = initialSaveFilename;
        }

        protected override bool ShouldDoTypeInField => true;
    }
}

