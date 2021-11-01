using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class AssetBundleHelper
    {        
        private const string PLATFORM_MACOS = "OSX";
        private const string PLATFORM_LINUX = "Linux";
        private const string PLATFORM_WINDOWS = "Windows";
        private const string MOD_DIR = "Assets";
        private const string MOD_ASSETS_CONFIG = "Assets.xml";
        
        public static readonly List<AssetBundle> bundles = new List<AssetBundle>();

        public static void Initialize(ModContentPack mod)
        {            
            string assetsConfig = Path.Combine(mod.RootDir, MOD_DIR, MOD_ASSETS_CONFIG);
            if (!File.Exists(assetsConfig))
            {
                Log.Warning($"CE: Assets.xml is required for loading CE assets doesn't exist at {assetsConfig}");
                return;
            }                    
            string platformDirectory = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformDirectory = PLATFORM_WINDOWS;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platformDirectory = PLATFORM_LINUX;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platformDirectory = PLATFORM_MACOS;
            }            
            var assetsPath = Path.Combine(mod.RootDir, MOD_DIR, platformDirectory);
            var document = new XmlDocument();
            document.Load(assetsConfig);
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                if (node.Name.ToLower() != "li")
                {
                    Log.Error($"CE: Assets.xml has an unknown node {node.Name}");
                    continue;
                }
                if (node.InnerText.NullOrEmpty())
                {
                    Log.Error($"CE: Assets.xml has an invalid element");
                    continue;
                }
                string currentBundlePath = Path.Combine(assetsPath, node.InnerText.Trim());
                AssetBundle assetBundle = AssetBundle.LoadFromFile(currentBundlePath);
                if (assetBundle != null)
                {
                    mod.assetBundles.loadedAssetBundles.Add(assetBundle);
                    bundles.Add(assetBundle);
#if DEBUG
                    Log.Message($"CE: Loaded assetbundle {node.InnerText.Trim()}");
#endif
                }
                else
                {
                    Log.Error($"CE: Could not load asset bundle at {currentBundlePath}");
                }
            }
        }      
    }
}

