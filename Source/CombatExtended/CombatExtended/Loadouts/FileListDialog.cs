using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class FileListDialog : Window
    {
        protected string interactButLabel = "Error";

        protected float bottomAreaHeight;

        protected List<FileInfo> files = new List<FileInfo>();

        protected Vector2 scrollPosition = Vector2.zero;

        protected string typingName = string.Empty;

        private bool focusedNameArea;

        private static readonly Color DefaultFileTextColor = new Color(1f, 1f, 0.6f);

        public static readonly Color UnimportantTextColor = new Color(1f, 1f, 1f, 0.5f);

        private readonly string StorageTypeName;

        private Action<FileInfo, FileListDialog> fileAction;

        [StaticConstructorOnStartup]
        private static class Texture
        {
            public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
        }

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        protected virtual bool ShouldDoTypeInField => false;

        public FileListDialog(string storageTypeName, Action<FileInfo, FileListDialog> fileAction)
        {
            this.StorageTypeName = storageTypeName;
            this.fileAction = fileAction;

            this.closeOnClickedOutside = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.ReloadFiles();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 vector = new Vector2(inRect.width - 16f, 36f);
            Vector2 vector2 = new Vector2(100f, vector.y - 2f);
            inRect.height -= 45f;
            float num = vector.y + 3f;
            float height = (float)this.files.Count * num;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, height);
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            outRect.height -= this.bottomAreaHeight;
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect, true);
            float num2 = 0f;
            int num3 = 0;
            foreach (FileInfo current in this.files)
            {
                Rect rect = new Rect(0f, num2, vector.x, vector.y);
                if (num3 % 2 == 0)
                {
                    Widgets.DrawAltRect(rect);
                }
                Rect position = rect.ContractedBy(1f);
                GUI.BeginGroup(position);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(current.Name);
                GUI.color = this.FileNameColor(current);
                Rect rect2 = new Rect(15f, 0f, position.width, position.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                Widgets.Label(rect2, fileNameWithoutExtension);
                GUI.color = Color.white;
                Rect rect3 = new Rect(270f, 0f, 200f, position.height);
                DrawDateAndVersion(current, rect3);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                float num4 = vector.x - 2f - vector2.x - vector2.y;
                Rect rect4 = new Rect(num4, 0f, vector2.x, vector2.y);
                if (Widgets.ButtonText(rect4, this.interactButLabel, true, false, true))
                {
                    fileAction(current, this);
                }
                Rect rect5 = new Rect(num4 + vector2.x + 5f, 0f, vector2.y, vector2.y);
                if (Widgets.ButtonImage(rect5, Texture.DeleteX))
                {
                    FileInfo localFile = current;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(localFile.Name), delegate
                    {
                        localFile.Delete();
                        ReloadFiles();
                    }, destructive: true));
                }
                TooltipHandler.TipRegion(rect5, "DeleteThisSavegame".Translate());
                GUI.EndGroup();
                num2 += vector.y + 3f;
                num3++;
            }
            Widgets.EndScrollView();
            if (this.ShouldDoTypeInField)
            {
                this.DoTypeInField(inRect.AtZero());
            }
        }

        protected void ReloadFiles()
        {
            TryGetDirectoryPath(this.StorageTypeName, out string path);

            this.files.Clear();
            foreach (string file in Directory.GetFiles(path))
            {
                try
                {
                    this.files.Add(new FileInfo(file));
                }
                catch (Exception ex)
                {
                    Log.Error("Exception reloading " + file + ": " + ex.ToString());
                }
            }
        }

        protected virtual void DoTypeInField(Rect rect)
        {
            GUI.BeginGroup(rect);
            bool flag = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
            float y = rect.height - 52f;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.SetNextControlName("MapNameField");
            Rect rect2 = new Rect(5f, y, 400f, 35f);
            string str = Widgets.TextField(rect2, this.typingName);
            if (GenText.IsValidFilename(str))
            {
                this.typingName = str;
            }
            if (!this.focusedNameArea)
            {
                UI.FocusControl("MapNameField", this);
                this.focusedNameArea = true;
            }
            Rect rect3 = new Rect(420f, y, rect.width - 400f - 20f, 35f);
            if (Widgets.ButtonText(rect3, "SaveGameButton".Translate(), true, false, true) || flag)
            {
                if (this.typingName.NullOrEmpty())
                {
                    Messages.Message("NeedAName".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    FileInfo fi;
                    TryGetFileInfo(this.StorageTypeName, this.typingName, out fi);
                    fileAction(fi, this);
                }
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private static bool TryGetDirectoryPath(string storageTypeName, out string path)
        {
            if (TryGetDirectoryName(storageTypeName, out path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
                return true;
            }
            return false;
        }

        private static bool TryGetDirectoryName(string storageTypeName, out string path)
        {
            try
            {
                path = (string)typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
                {
                    "SaveStorageSettings/" + storageTypeName
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("SaveStorageSettings: Failed to get folder name - " + ex);
                path = null;
                return false;
            }
        }
        
        private static bool TryGetFileInfo(string storageTypeName, string fileName, out FileInfo fi)
        {
            string path;
            if (TryGetDirectoryPath(storageTypeName, out path))
            {
                fi = new FileInfo(Path.Combine(path, fileName.ToString() + ".xml"));
                return true;
            }
            fi = null;
            return false;
        }

        protected virtual Color FileNameColor(FileInfo fi)
        {
            return DefaultFileTextColor;
        }

        public static void DrawDateAndVersion(FileInfo fi, Rect rect)
        {
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect2 = new Rect(0f, 2f, rect.width, rect.height / 2f);
            GUI.color = UnimportantTextColor;
            Widgets.Label(rect2, fi.LastWriteTime.ToString("g"));
            Rect rect3 = new Rect(0f, rect2.yMax, rect.width, rect.height / 2f);
            GUI.EndGroup();
        }
    }
}
