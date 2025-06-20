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

    private void Awake()
    {

            _config = new ModConfig(this);


            // Ensure translation files are present and load the selected language
            LocalizationManager.EnsureBaseLanguageFiles(Logger);
            _localizationManager = new LocalizationManager(Logger, _config.PreferredLanguage.Value);


            _pinManager = new PinManager(_config, Logger, _localizationManager);
            _prefabTracker = new PrefabTracker(Logger, _config);
            _scanner = new PinScanner(_config, _pinManager, _prefabTracker);


            Logger.LogWarning("SullysAutoPinner Initialized");
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

public class ConfigLoader
{
    private static readonly string SaveFolder = Path.Combine(Paths.ConfigPath, "SullysAutoPinnerFiles");
    private static readonly string ConfigFilePath = Path.Combine(SaveFolder, "SullysAutoPinner.cfg");

    private readonly BepInEx.Logging.ManualLogSource _logger;
    private FileSystemWatcher _watcher;
    private System.Threading.Timer _debounceTimer;
    private Action _onReload;

    public ConfigLoader(BepInEx.Logging.ManualLogSource logger)
    {
        _logger = logger;
        Directory.CreateDirectory(SaveFolder);
    }

    public void EnableHotReload(Action onReload)
    {
        _onReload = onReload;
        _watcher = new FileSystemWatcher(SaveFolder, "SullysAutoPinner.cfg")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += OnConfigChanged;
        _watcher.Renamed += OnConfigChanged;
        _watcher.EnableRaisingEvents = true;
        _logger.LogWarning("[ConfigLoader] Hot config reload enabled.");
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                _logger.LogWarning("[ConfigLoader] Config file updated — reloading.");
                _onReload?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[ConfigLoader] Reload failed: " + ex.Message);
            }
        }, null, 200, Timeout.Infinite);
    }

    public PinSettings Load()
    {
        var settings = new PinSettings();
        var existing = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (File.Exists(ConfigFilePath))
        {
            foreach (var line in File.ReadAllLines(ConfigFilePath))
            {
                var t = line.Trim();
                if (t.StartsWith("#") || !t.Contains("=")) continue;
                var parts = t.Split(new[] { '=' }, 2);
                existing[parts[0].Trim().ToLowerInvariant()] = parts[1].Trim().ToLowerInvariant();
            }

            foreach (var kv in existing)
                ApplyKeyValue(settings, kv.Key, kv.Value);

            var allBoolKeys = typeof(PinSettings)
                .GetFields()
                .Where(f => f.FieldType == typeof(bool))
                .Select(f => f.Name.ToLowerInvariant())
                .ToHashSet();

            var missing = allBoolKeys.Except(existing.Keys).ToList();

            var oneTimeFlags = new[] {
                "clearallmappinsandhistory",
                "clearallunwantedpins"
            };

            foreach (var key in oneTimeFlags)
            {
                if (!existing.ContainsKey(key) && !missing.Contains(key))
                    missing.Add(key);
            }

            if (missing.Any())
                AppendMissingDefaults(settings, missing);
        }
        else
        {
            SaveDefault(settings);
        }

        return settings;
    }

    private void ApplyKeyValue(PinSettings settings, string key, string value)
    {
        bool flag = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        float num;
        switch (key)
        {
            case "scanradius": if (float.TryParse(value, out num)) settings.ScanRadius = num; break;
            case "scaninterval": if (float.TryParse(value, out num)) settings.ScanInterval = num; break;
            case "saveinterval": if (float.TryParse(value, out num)) settings.SaveInterval = num; break;
            case "pinmergedistance": if (float.TryParse(value, out num)) settings.PinMergeDistance = num; break;
            case "languagecode": settings.LanguageCode = value; break;
            default:
                var field = typeof(PinSettings).GetFields().FirstOrDefault(f => f.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(settings, flag);
                }
                break;
        }
    }

    private void AppendMissingDefaults(PinSettings settings, IEnumerable<string> missingKeys)
    {
        try
        {
            using (var w = File.AppendText(ConfigFilePath))
            {
                foreach (var key in missingKeys)
                {
                    var field = typeof(PinSettings)
                        .GetFields()
                        .First(f => f.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));

                    if (field.FieldType == typeof(bool))
                    {
                        bool val = (bool)field.GetValue(settings);
                        w.WriteLine($"{key}={val.ToString().ToLowerInvariant()}");
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        string val = (string)field.GetValue(settings);
                        w.WriteLine($"{key}={val.ToLowerInvariant()}");
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        float val = (float)field.GetValue(settings);
                        w.WriteLine($"{key}={val.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
            }
            _logger.LogWarning("Appended missing config keys to: " + ConfigFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error appending missing defaults: " + ex.Message);
        }
    }

    public void SaveDefault(PinSettings settings)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(ConfigFilePath))
            {
                writer.WriteLine("# SullysAutoPinner config");
                writer.WriteLine("languagecode=" + settings.LanguageCode.ToLowerInvariant());
                writer.WriteLine("scanradius=" + settings.ScanRadius);
                writer.WriteLine("scaninterval=" + settings.ScanInterval);
                writer.WriteLine("saveinterval=" + settings.SaveInterval);
                writer.WriteLine("pinmergedistance=" + settings.PinMergeDistance);

                foreach (var field in typeof(PinSettings).GetFields())
                {
                    if (field.FieldType == typeof(bool))
                    {
                        string key = field.Name.ToLowerInvariant();
                        bool val = (bool)field.GetValue(settings);
                        writer.WriteLine(key + "=" + val.ToString().ToLowerInvariant());
                    }
                }
            }
            _logger.LogWarning("Default config created at: " + ConfigFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error saving default config: " + ex.Message);
        }
    }
}

public class PinSettings
{
    public string LanguageCode = "en";
    public bool ClearNearbyFallbackPinsOnNextScan = false;
    public bool ShowUnmappedPrefabs = false;
    public float ScanRadius = 300f;
    public float ScanInterval = 12f;
    public float SaveInterval = 120f;
    public float PinMergeDistance = 50f;
    public bool EnablePinSaving = true;
    public bool EnablePrefabLogging = false;
    public bool Mushrooms = false;
    public bool RaspBerries = false;
    public bool BlueBerries = false;
    public bool Seeds = false;
    public bool Thistle = false;
    public bool CloudBerries = false;
    public bool Copper = true;
    public bool Tin = false;
    public bool Skeleton = false;
    public bool DwarfSpawner = true;
    public bool TrollCave = true;
    public bool Crypt = true;
    public bool Totem = true;
    public bool Fire = true;
    public bool DraugrSpawner = false;
    public bool Treasure = false;
    public bool Barley = false;
    public bool Flax = false;
    public bool Tar = true;
    public bool DragonEgg = true;
    public bool VoltureEgg = true;
    public bool SmokePuffs = false;
    public bool MageCaps = true;
    public bool YPCones = true;
    public bool JotunPuffs = true;
    public bool Iron = true;
    public bool Mudpile = true;
    public bool Abomination = true;
    public bool Meteorite = true;
    public bool Obsidian = true;
    public bool Silver = true;
    public bool MistlandsGiants = true;
    public bool MistlandsSwords = true;
    public bool DvergerThings = true;
    public bool ClearAllMapPinsAndHistory = false;
    public bool ClearAllUnwantedPins = false;
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
                var field = typeof(ModConfig).GetField(settingKey);
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
                { "abomination", ("Abomination", Minimap.PinType.Icon3) },
                { "crypt2", ("Crypt", Minimap.PinType.Icon3) },
                { "crypt3", ("Crypt", Minimap.PinType.Icon3) },
                { "crypt4", ("Crypt", Minimap.PinType.Icon3) },
                { "trollcave", ("TrollCave", Minimap.PinType.Icon3) },
                { "rock4_copper", ("Copper", Minimap.PinType.Icon3) },
                { "iron_deposit", ("Iron", Minimap.PinType.Icon3) },
                { "mudpile", ("Mudpile", Minimap.PinType.Icon3) },
                { "obsidian", ("Obsidian", Minimap.PinType.Icon3) },
                { "silver", ("Silver", Minimap.PinType.Icon3) },
                { "meteor", ("Meteorite", Minimap.PinType.Icon3) },
                { "flax", ("Flax", Minimap.PinType.Icon3) },
                { "barley", ("Barley", Minimap.PinType.Icon3) },
                { "blueberry", ("BlueBerries", Minimap.PinType.Icon3) },
                { "berries", ("RaspBerries", Minimap.PinType.Icon3) },
                { "shrooms", ("Mushrooms", Minimap.PinType.Icon3) },
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
                { "smokepuff", ("SmokePuffs", Minimap.PinType.Icon3) },
                { "thing", ("DvergerThings", Minimap.PinType.Icon3) },
                { "dwarfspawner", ("DwarfSpawner", Minimap.PinType.Icon2) },
                { "draugrspawner", ("DraugrSpawner", Minimap.PinType.Icon2) },
                { "totem", ("Totem", Minimap.PinType.Icon2) },
                { "campfire", ("Fire", Minimap.PinType.Icon0) },
                { "tar", ("Tar", Minimap.PinType.Icon2) },
                { "skel", ("Skeleton", Minimap.PinType.Icon3) },
                { "treasure", ("Treasure", Minimap.PinType.Icon3) },
                { "swords", ("MistlandsSwords", Minimap.PinType.Icon3) },
                { "marker", ("Marker", Minimap.PinType.Icon2) },
                { "giant", ("MistlandsGiants", Minimap.PinType.Icon3) }
                };
        }
    }

}