// SullysAutoPinner v1.4.0
// 
//  changed congig from custom to Bepinex compliant
// first attempt
// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using static SullysAutoPinner.PinScanner;
using BepInEx.Configuration;
using System.Reflection;

namespace SullysAutoPinner { 

[BepInPlugin("sullys.autopinner", "Sullys Auto Pinner", "1.4.0")]
public class SullysAutoPinner : BaseUnityPlugin
{

    private PinManager _pinManager;
    private ModConfig _config;

    
    private PinScanner _scanner;
    private PrefabTracker _prefabTracker;
    private float _lastScanTime;

    private LocalizationManager _localizationManager; // <-- add this at class level if not already
    private ConfigWatcher _configWatcher;


        private void Awake()
    {

            //_config = new ModConfig(this);
            _config = new ModConfig(Config, Logger);

            string configPath = Path.Combine(Paths.ConfigPath, "sullys.autopinner.cfg");

            _configWatcher = new ConfigWatcher(configPath, OnConfigReloaded, Logger);


            // Ensure translation files are present and load the selected language
            LocalizationManager.EnsureBaseLanguageFiles(Logger);
            _localizationManager = new LocalizationManager(Logger, _config.PreferredLanguage.Value);


            _pinManager = new PinManager(_config, Logger, _localizationManager);
            _prefabTracker = new PrefabTracker(Logger, _config);
            _scanner = new PinScanner(_config, _pinManager, _prefabTracker);


            Logger.LogWarning("SullysAutoPinner Initialized");
    }

        private void OnConfigReloaded()
        {
            Logger.LogWarning("[SullysAutoPinner] Reloading config from file...");

            Config.Reload();

            _localizationManager.Reload(_config.PreferredLanguage.Value);
            _pinManager.ReloadConfig(); // if needed

            if (_config.ClearAllUnwantedPins.Value)
            {
                Logger.LogWarning("[SullysAutoPinner] Remove Unwanted Pins Triggered");
                _pinManager.RemoveUnwantedPins(_config);
                _config.ClearAllUnwantedPins.Value = false;
            }
            if (_config.ClearAllMapPinsAndHistory.Value)
            {
                Logger.LogWarning("[SullysAutoPinner] Remove All Pins and History Triggered");
                _pinManager.RemoveAllPins();
                _config.ClearAllMapPinsAndHistory.Value = false;
            }

        }

        private void Update()
    {
        if (Player.m_localPlayer == null) return;

            if (Time.time - _lastScanTime > _config.ScanInterval.Value)
            {
                _lastScanTime = Time.time;
            _scanner.RunScan(Player.m_localPlayer.transform.position);
        }

        _pinManager.TryPeriodicSave();
        _prefabTracker.TryPeriodicSave();
    }

    private void OnApplicationQuit()
    {
        _pinManager.SaveImmediate();
        _prefabTracker.SaveImmediate();
    }
}
    // --- PrefabTracker ---
    public class PrefabTracker
    {
        private readonly HashSet<string> _seenPrefabs = new HashSet<string>();
        private readonly BepInEx.Logging.ManualLogSource _logger;
        private readonly ModConfig _config;
        private float _lastPrefabDumpTime;
        private const float DumpInterval = 300f;
        private const int PrefabFlushThreshold = 1000;
        private const string PrefabsFilePath = "BepInEx/config/SullysAutoPinnerFiles/SeenPrefabs.txt";

        private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>()
    {
        "tree", "bush", "log", "stone", "branch"
    };

        public PrefabTracker(BepInEx.Logging.ManualLogSource logger, ModConfig config)
        {
            _logger = logger;
            _config = config;
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabsFilePath));
        }

        public void LogPrefabHierarchy(GameObject rootGO)
        {
            if (!_config.EnablePrefabLogging.Value || rootGO == null) return;

            string rootName = rootGO.name.ToLowerInvariant();
            foreach (string ignore in IgnoredPrefabs)
            {
                if (rootName.Contains(ignore)) return;
            }

            List<string> childNames = new List<string>();
            foreach (Transform child in rootGO.transform)
            {
                if (child != null) childNames.Add(child.name);
            }

            childNames.Sort();
            string entry = rootGO.name + " | " + string.Join(",", childNames);

            if (_seenPrefabs.Add(entry) && _seenPrefabs.Count >= PrefabFlushThreshold)
            {
                SaveImmediate();
            }
        }

        public void TryPeriodicSave()
        {
            if (!_config.EnablePrefabLogging.Value) return;

            if (Time.time - _lastPrefabDumpTime > DumpInterval)
            {
                SaveImmediate();
            }
        }

        public void SaveImmediate()
        {
            _lastPrefabDumpTime = Time.time;

            try
            {
                HashSet<string> existingEntries = new HashSet<string>();

                if (File.Exists(PrefabsFilePath))
                {
                    string[] lines = File.ReadAllLines(PrefabsFilePath);
                    foreach (string line in lines)
                    {
                        existingEntries.Add(line);
                    }
                }

                using (StreamWriter writer = File.AppendText(PrefabsFilePath))
                {
                    foreach (string entry in _seenPrefabs)
                    {
                        if (!existingEntries.Contains(entry))
                        {
                            writer.WriteLine(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SullyAutoPinner >>> Error saving prefab list: " + ex.Message);
            }
        }
    }


    // --- PinScanner ---
    public class PinScanner
    {
        private readonly ModConfig _config;
        private readonly PinManager _pinManager;
        private readonly PrefabTracker _prefabTracker;

        public PinScanner(ModConfig config, PinManager pinManager, PrefabTracker prefabTracker)
        {
            _config = config;
            _pinManager = pinManager;
            _prefabTracker = prefabTracker;
        }

        public void RunScan(Vector3 playerPosition)
        {
            if (_config.ClearNearbyFallbackPinsOnNextScan.Value)
            {
                _pinManager.RemoveNearbyFallbackPins(playerPosition, _config.ScanRadius.Value);
                _config.ClearNearbyFallbackPinsOnNextScan.Value = false;
                // optionally persist ClearNearbyFallbackPinsOnNextScan reset here if needed
            }

            if (playerPosition.y > 1000f)
            {
                return; // Inside dungeon or crypt instance — skip scan
            }

            Collider[] hits = Physics.OverlapSphere(playerPosition, _config.ScanRadius.Value, ~0, QueryTriggerInteraction.Collide);

            var trollCavesThisScan = new List<Vector3>();
            var deferredProxies = new List<(Vector3, Transform)>();

            foreach (var hit in hits)
            {
                GameObject root = hit.transform.root.gameObject;
                if (root == null) continue;

                _prefabTracker.LogPrefabHierarchy(root);
                string rootName = root.name.ToLowerInvariant();
                Vector3 pinPos = root.transform.position;

                if (rootName.Contains("locationproxy"))
                {
                    deferredProxies.Add((pinPos, root.transform));
                    continue;
                }

                if (TryMatchPrefab(rootName, out string label, out Minimap.PinType icon))
                {
                    if (label == "TROLLCAVE") trollCavesThisScan.Add(pinPos);
                    if (label == "CRYPT" && trollCavesThisScan.Any(tc => Vector3.Distance(tc, pinPos) < 40f))
                        continue;

                    _pinManager.TryAddPin(pinPos, label, icon);
                }
            }

            foreach (var (pinPos, transform) in deferredProxies)
            {
                foreach (Transform child in transform)
                {
                    string childName = child.name.ToLowerInvariant();
                    if (childName.Contains("runestone")) continue;

                    if (TryMatchPrefab(childName, out string label, out Minimap.PinType icon))
                    {
                        if (label == "TROLLCAVE")
                        {
                            trollCavesThisScan.Add(pinPos);
                            _pinManager.TryAddPin(pinPos, label, icon);
                        }
                        break;
                    }
                }
            }

            foreach (var (pinPos, transform) in deferredProxies)
            {
                foreach (Transform child in transform)
                {
                    string childName = child.name.ToLowerInvariant();
                    if (childName.Contains("runestone")) continue;

                    if (TryMatchPrefab(childName, out string label, out Minimap.PinType icon))
                    {
                        if (label == "TROLLCAVE") break;
                        if (label == "CRYPT" && trollCavesThisScan.Any(tc => Vector3.Distance(tc, pinPos) < 40f))
                            break;

                        _pinManager.TryAddPin(pinPos, label, icon);
                        break;
                    }
                }
            }
        }

        private string CleanLabel(string rawName)
        {
            string noClone = rawName.Replace("(Clone)", "");
            string noDigits = System.Text.RegularExpressions.Regex.Replace(noClone, @"\d", "");
            return noDigits.ToUpperInvariant();
        }

        private bool TryMatchPrefab(string prefabName, out string matchedLabel, out Minimap.PinType icon)
        {
            matchedLabel = null;
            icon = Minimap.PinType.Icon3;

            foreach (var kvp in PinLabelMap.LabelToSettingInfo)
            {
                if (prefabName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string settingKey = kvp.Value.SettingKey;
                var field = typeof(ModConfig).GetField(settingKey, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (field == null || field.FieldType != typeof(ConfigEntry<bool>)) continue;

                var entry = (ConfigEntry<bool>)field.GetValue(_config);
                if (entry != null && entry.Value)
                {
                    matchedLabel = settingKey.ToUpperInvariant();
                    icon = kvp.Value.Icon;
                    return true;
                }
            }

            if (_config.ShowUnmappedPrefabs.Value)
            {
                string[] fallbackExcludes = new[]
                {
                "beech", "berry", "birch", "boar", "branch", "bush", "dandelion", "deer", "feathers",
                "fish", "firtree", "fur", "goblin", "goblin_archer", "goblin_banner", "goblin_bed", "goblin_fence",
                "goblin_pole", "goblin_roof", "goblin_roof_cap", "goblin_shaman", "goblin_stepladder",
                "greyling", "greydwarf", "mushroom", "neck", "oak1", "pickable_dolmentreasure",
                "pickable_forestcryptremains", "pickable_flint", "pickable_stone", "player", "pine",
                "piece_sharpstakes", "piece_workbench", "berries", "resin", "seagal", "shrub", "stone", "stone_wall", "stubbe",
                "vines", "wood", "zone", "pillar", "carrot", "turnip", "onion", "thistle", "minerock_tin",
                "crow", "odin", "pickable_tar", "tarliquid", "skeleton", "deathsquito", "forestcrypt",
                "swamptree", "wraith", "statue", "leech", "table", "draugr", "blob", "oak", "leather",
                "log", "meat", "seed", "bonefragments", "honey", "forge", "entrails", "bonepile",
                "castlekit", "trophy", "sunken", "root", "guck", "swamp", "grave", "piece"
            };

                foreach (var exclude in fallbackExcludes)
                {
                    if (prefabName.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }

                matchedLabel = prefabName.Replace("(clone)", "");
                icon = Minimap.PinType.Icon2;
                return true;
            }

            return false;
        }

        public static class PinLabelMap
        {
            public static readonly Dictionary<string, (string SettingKey, Minimap.PinType Icon)> LabelToSettingInfo =
                new Dictionary<string, (string, Minimap.PinType)>(StringComparer.OrdinalIgnoreCase)
                {
            // Dungeons and Spawners
            { "abomination", ("Abomination", Minimap.PinType.Icon3) },
            { "crypt2", ("Crypt", Minimap.PinType.Icon3) },
            { "crypt3", ("Crypt", Minimap.PinType.Icon3) },
            { "crypt4", ("Crypt", Minimap.PinType.Icon3) },
            { "trollcave", ("TrollCave", Minimap.PinType.Icon3) },
            { "dwarfspawner", ("DwarfSpawner", Minimap.PinType.Icon2) },
            { "draugrspawner", ("DraugrSpawner", Minimap.PinType.Icon2) },
            { "totem", ("Totem", Minimap.PinType.Icon2) },
            { "skel", ("Skeleton", Minimap.PinType.Icon3) },
            { "seekerbrute", ("SeekerBrute", Minimap.PinType.Icon3) },
            { "bonemawserpent", ("BonemawSerpent", Minimap.PinType.Icon3) },

            // Resources
            { "rock4_copper", ("Copper", Minimap.PinType.Icon3) },
            { "iron_deposit", ("Iron", Minimap.PinType.Icon3) },
            { "mudpile", ("Mudpile", Minimap.PinType.Icon3) },
            { "obsidian", ("Obsidian", Minimap.PinType.Icon3) },
            { "rock3_silver", ("Silver", Minimap.PinType.Icon3) },
            { "silvervein", ("Silver", Minimap.PinType.Icon3) },
            { "meteor", ("Meteorite", Minimap.PinType.Icon3) },
            { "pickable_tar", ("Tar", Minimap.PinType.Icon2) },
            { "pickable_flax_wild", ("Flax", Minimap.PinType.Icon3) },

            // Plants & Food
            { "flax", ("Flax", Minimap.PinType.Icon3) },
            { "barley", ("Barley", Minimap.PinType.Icon3) },
            { "blueberry", ("BlueBerries", Minimap.PinType.Icon3) },
            { "raspberry", ("RaspBerries", Minimap.PinType.Icon3) },
            { "shroom", ("Mushrooms", Minimap.PinType.Icon3) },
            { "cloud", ("CloudBerries", Minimap.PinType.Icon3) },
            { "thistle", ("Thistle", Minimap.PinType.Icon3) },
            { "carrot", ("Seeds", Minimap.PinType.Icon3) },
            { "onion", ("Seeds", Minimap.PinType.Icon3) },
            { "turnip", ("Seeds", Minimap.PinType.Icon3) },
            { "magecap", ("MageCaps", Minimap.PinType.Icon3) },
            { "y-pcone", ("YPCones", Minimap.PinType.Icon3) },
            { "j-puff", ("JotunPuffs", Minimap.PinType.Icon3) },
            { "v-egg", ("VoltureEgg", Minimap.PinType.Icon3) },
            { "dragonegg", ("DragonEgg", Minimap.PinType.Icon3) },
            { "pickable_smokepuff", ("SmokePuffs", Minimap.PinType.Icon3) },

            // Structures / Misc Points of Interest
            { "thing", ("DvergerThings", Minimap.PinType.Icon3) },
            { "giant_sword1", ("GiantSword", Minimap.PinType.Icon3) },
            { "giant_ribs", ("GiantRibs", Minimap.PinType.Icon3) },
            { "giant_skull", ("GiantSkull", Minimap.PinType.Icon3) },
            { "giant_brain", ("GiantBrain", Minimap.PinType.Icon3) },
            { "marker", ("Marker", Minimap.PinType.Icon2) },
            { "campfire", ("Fire", Minimap.PinType.Icon0) },
            { "firepit", ("Fire", Minimap.PinType.Icon0) },

            // Loot / Containers
            { "treasurechest", ("Treasure", Minimap.PinType.Icon3) },
            { "shipwreck_karve_chest", ("ShipwreckChest", Minimap.PinType.Icon3) },

            // Mistlands & Ashlands Decor
            { "pickable_dvergrlantern", ("Lantern", Minimap.PinType.Icon3) },
            { "mistlands_excavation2", ("Excavation", Minimap.PinType.Icon3) },
            { "ashland_pot1_green", ("AshlandPotGreen", Minimap.PinType.Icon3) },
            { "ashland_pot2_red", ("AshlandPotRed", Minimap.PinType.Icon3) },
            { "ashland_pot3_red", ("AshlandPotRed", Minimap.PinType.Icon3) },
                };
        }

    }

}