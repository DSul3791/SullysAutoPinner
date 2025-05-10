// SullysAutoPinner v1.2.6
// 
// fixed Pins.txt file location
// adjusted metals detection.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using BepInEx;
using HarmonyLib;


[BepInPlugin("sullys.autopinner", "Sullys Auto Pinner", "1.2.6")]
public class SullysAutoPinner : BaseUnityPlugin
{
    private PinSettings _settings;
    private PinManager _pinManager;
    private ConfigLoader _configLoader;
    private PinScanner _scanner;
    private PrefabTracker _prefabTracker;
    private float _lastScanTime;

    private void Awake()
    {
        _configLoader = new ConfigLoader(Logger);
        _settings = _configLoader.Load();

        _pinManager = new PinManager(_settings, Logger);
        _prefabTracker = new PrefabTracker(Logger, _settings);
        _scanner = new PinScanner(_settings, _pinManager, _prefabTracker);

        _configLoader.EnableHotReload(() =>
        {
            _settings = _configLoader.Load();
            _scanner = new PinScanner(_settings, _pinManager, _prefabTracker);
            _lastScanTime = 0f;

            if (_settings.ClearAllMapPinsAndHistory)
            {
                _pinManager.RemoveAllPins();
                _settings.ClearAllMapPinsAndHistory = false;
                _configLoader.SaveDefault(_settings);
                Logger.LogWarning("[SullysAutoPinner] All map pins cleared and config flag reset.");
            }

            if (_settings.ClearAllUnwantedPins)
            {
                _pinManager.RemoveUnwantedPins(_settings);
                _settings.ClearAllUnwantedPins = false;
                _configLoader.SaveDefault(_settings);
                Logger.LogWarning("[SullysAutoPinner] Unwanted pins cleared and config flag reset.");
            }

            Logger.LogWarning("[SullysAutoPinner] Config hot-reloaded — scanner reset.");
        });




        Logger.LogWarning("SullysAutoPinner v1.2.2 Initialized");
    }


    private void Update()
    {
        if (Player.m_localPlayer == null) return;

        if (Time.time - _lastScanTime > _settings.ScanInterval)
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
                    bool val = (bool)field.GetValue(settings);
                    w.WriteLine($"{key}={val.ToString().ToLowerInvariant()}");
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
    public bool DraugrSpawner = false;
    public bool Treasure = false;
    public bool Barley = false;
    public bool Flax = false;
    public bool Tar = true;
    public bool DragonEgg = true;
    public bool VoltureEgg = true;
    public bool SmokePuffs = false;
    public bool MageCaps = false;
    public bool YPCones = true;
    public bool JotunnPuffs = false;
    public bool Iron = true;
    public bool Meteorite = true;
    public bool Obsidian = true;
    public bool Silver = true;
    public bool MistlandsGiants = true;
    public bool MistlandsSwords = true;
    public bool DvergerThings = true;
    public bool ClearAllMapPinsAndHistory = false;
    public bool ClearAllUnwantedPins = false;
}

// --- PinManager ---
public class PinManager
{
    private const int PinFlushThreshold = 250;
    private readonly List<Tuple<Vector3, string>> _pinQueue = new List<Tuple<Vector3, string>>();
    private readonly HashSet<string> _pinHashes = new HashSet<string>();
    private readonly PinSettings _settings;
    private readonly BepInEx.Logging.ManualLogSource _logger;
    private float _lastSaveTime;

    private static readonly string SaveFolder = Path.Combine(Paths.ConfigPath, "SullysAutoPinnerFiles");
    private static readonly string PinsFilePath = Path.Combine(SaveFolder, "Pins.txt");


    public PinManager(PinSettings settings, BepInEx.Logging.ManualLogSource logger)
    {
        _settings = settings;
        _logger = logger;
        Directory.CreateDirectory(Path.GetDirectoryName(PinsFilePath));
        LoadPinsFromFile();
    }

    public void TryAddPin(Vector3 pos, string label)
    {
        if (Minimap.instance == null) return;

        Vector3 roundedPos = RoundTo0_1(pos);
        string hash = GetPinHash(roundedPos, label);

        if (_pinHashes.Contains(hash)) return;

        foreach (var existing in _pinQueue)
        {
            if (Vector3.Distance(existing.Item1, roundedPos) < _settings.PinMergeDistance &&
                string.Equals(existing.Item2, label, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Minimap.instance.AddPin(roundedPos, Minimap.PinType.Icon3, label, true, false);
        
        _pinQueue.Add(new Tuple<Vector3, string>(roundedPos, label));
        _pinHashes.Add(hash);

        if (_settings.EnablePinSaving && _pinQueue.Count >= PinFlushThreshold)
        {
            SavePinsToFile();
        }
    }

    public void TryPeriodicSave()
    {
        if (!_settings.EnablePinSaving || _pinQueue.Count == 0) return;
        if (Time.time - _lastSaveTime > _settings.SaveInterval)
        {
            SavePinsToFile();
        }
    }

    public void SaveImmediate()
    {
        if (_settings.EnablePinSaving && _pinQueue.Count > 0)
        {
            SavePinsToFile();
        }
    }

    public void RemoveAllPins()
    {
        if (Minimap.instance == null) return;
        _pinHashes.Clear();
        _pinQueue.Clear();

        try
        {
            File.WriteAllText(PinsFilePath, string.Empty); // Wipe the file clean
            _logger.LogWarning("SullysAutoPinner >>> All Map Pins Removed -Pin history file emptied.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SullysAutoPinner >>> All Map Pins Removed - Failed to clear Pins.txt: " + ex.Message);
            return;
        }

        var allPins = AccessTools.Field(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>;
        if (allPins == null) return;

        foreach (var pin in allPins.ToList()) // copy to avoid modifying during iteration
        {
            Minimap.instance.RemovePin(pin);
        }

        _logger.LogWarning($"SullysAutoPinner >>> All Map Pins Removed {allPins.Count} pins from the map.");
    }


    public void RemoveUnwantedPins(PinSettings settings)
    {
        if (Minimap.instance == null) return;



        var allPins = AccessTools.Field(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>;

        var toRemove = new List<Minimap.PinData>();

        foreach (var pin in allPins)
        {
            string label = pin.m_name.ToUpperInvariant();

            if (!IsLabelEnabled(label, settings))
            {
                toRemove.Add(pin);
            }
        }

        foreach (var pin in toRemove)
        {
            Minimap.instance.RemovePin(pin);
        }

        _logger.LogWarning($"Removed {toRemove.Count} unwanted pins from the map.");
    }

    private bool IsLabelEnabled(string label, PinSettings settings)
    {
        switch (label)
        {
            case "CRYPT": return settings.Crypt;
            case "TROLLCAVE": return settings.TrollCave;
            case "COPPER": return settings.Copper;
            case "IRON": return settings.Iron;
            case "TIN": return settings.Tin;
            case "OBSIDIAN": return settings.Obsidian;
            case "SILVER": return settings.Silver;
            case "METEOR": return settings.Meteorite;
            case "TAR": return settings.Tar;
            case "FLAX": return settings.Flax;
            case "BARLEY": return settings.Barley;
            case "BLUE": return settings.BlueBerries;
            case "BERRIES": return settings.RaspBerries;
            case "SHROOMS": return settings.Mushrooms;
            case "CLOUD": return settings.CloudBerries;
            case "THISTLE": return settings.Thistle;
            case "CARROT": case "ONION": case "TURNIP": return settings.Seeds;
            case "MAGECAP": return settings.MageCaps;
            case "Y-PCONE": return settings.YPCones;
            case "J-PUFF": return settings.JotunnPuffs;
            case "V-EGG": return settings.VoltureEgg;
            case "DRAGONEGG": return settings.DragonEgg;
            case "SMOKEPUFF": return settings.SmokePuffs;
            case "THING": return settings.DvergerThings;
            case "DWARFSPAWNER": return settings.DwarfSpawner;
            case "DRAUGRSPAWNER": return settings.DraugrSpawner;
            case "TOTEM": return settings.Totem;
            case "SKEL": return settings.Skeleton;
            case "BOX": return settings.Treasure;
            case "SWORDS": return settings.MistlandsSwords;
            case "GIANT": return settings.MistlandsGiants;
            default: return true;
        }
    }

    private void SavePinsToFile()
    {
        try
        {
            using (StreamWriter writer = File.AppendText(PinsFilePath))
            {
                foreach (var pin in _pinQueue)
                {
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F1}|{1:F1}|{2:F1}|{3}",
                        pin.Item1.x, pin.Item1.y, pin.Item1.z, pin.Item2));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SullyAutoPinner >>> Error saving pins: " + ex.Message);
        }

        _pinQueue.Clear();
        _lastSaveTime = Time.time;
    }

    private void LoadPinsFromFile()
    {
        try
        {
            if (!File.Exists(PinsFilePath)) return;

            string[] lines = File.ReadAllLines(PinsFilePath);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length != 4) continue;

                if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;
                string label = parts[3];

                Vector3 pos = new Vector3(x, y, z);
                string hash = GetPinHash(pos, label);
                _pinHashes.Add(hash);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SullyAutoPinner >>> Error loading pins: " + ex.Message);
        }
    }

    private string GetPinHash(Vector3 pos, string label)
    {
        Vector3 rounded = RoundTo0_1(pos);
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:F1},{2:F1},{3:F1}", label.ToUpperInvariant(), rounded.x, rounded.y, rounded.z);
    }

    private Vector3 RoundTo0_1(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x * 10f) / 10f,
            Mathf.Round(v.y * 10f) / 10f,
            Mathf.Round(v.z * 10f) / 10f
        );
    }
}


// --- PrefabTracker ---
public class PrefabTracker
{
    private readonly HashSet<string> _seenPrefabs = new HashSet<string>();
    private readonly BepInEx.Logging.ManualLogSource _logger;
    private readonly PinSettings _settings;
    private float _lastPrefabDumpTime;
    private const float DumpInterval = 300f;
    private const int PrefabFlushThreshold = 1000;
    private const string PrefabsFilePath = "BepInEx/config/SullysAutoPinnerFiles/SeenPrefabs.txt";

    private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>()
    {
        "tree", "bush", "log", "stone", "branch"
    };

    public PrefabTracker(BepInEx.Logging.ManualLogSource logger, PinSettings settings)
    {
        _logger = logger;
        _settings = settings;
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabsFilePath));
    }

    public void LogPrefabHierarchy(GameObject rootGO)
    {
        if (!_settings.EnablePrefabLogging || rootGO == null) return;

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
        if (!_settings.EnablePrefabLogging) return;
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
    private readonly PinSettings _settings;
    private readonly PinManager _pinManager;
    private readonly PrefabTracker _prefabTracker;

    public PinScanner(PinSettings settings, PinManager pinManager, PrefabTracker prefabTracker)
    {
        _settings = settings;
        _pinManager = pinManager;
        _prefabTracker = prefabTracker;
    }

    public void RunScan(Vector3 playerPosition)
    {
        Collider[] hits = Physics.OverlapSphere(playerPosition, _settings.ScanRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            GameObject root = hit.transform.root.gameObject;
            if (root == null) continue;

            _prefabTracker.LogPrefabHierarchy(root);

            string name = root.name.ToLowerInvariant();
            if (name.Contains("locationproxy"))
            {
                HandleLocationProxyChildren(root);
            }
            else if (TryMatchPrefab(name, out string label))
            {
                _pinManager.TryAddPin(root.transform.position, label);
            }
        }
    }

    private void HandleLocationProxyChildren(GameObject root)
    {
        string[] patterns = { "trollcave", "crypt" };

        foreach (Transform child in root.transform)
        {
            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("runestone")) continue;

            foreach (string pattern in patterns)
            {
                if (childName.Contains(pattern))
                {
                    string label = CleanLabel(child.name);
                    _pinManager.TryAddPin(root.transform.position, label);
                    return;
                }
            }
        }

        // Fallback: use first non-runestone child
        foreach (Transform child in root.transform)
        {
            string childName = child.name.ToLowerInvariant();
            if (!childName.Contains("runestone"))
            {
                string label = CleanLabel(child.name);
                _pinManager.TryAddPin(root.transform.position, label);
                return;
            }
        }
    }

    private string CleanLabel(string rawName)
    {
        string noClone = rawName.Replace("(Clone)", "");
        string noDigits = System.Text.RegularExpressions.Regex.Replace(noClone, @"\d", "");
        return noDigits.ToUpperInvariant();
    }



    private bool TryMatchPrefab(string name, out string label)
    {
        label = null;
        if (name.Contains("copper") && _settings.Copper) label = "COPPER";
        else if (name.Contains("iron") && _settings.Iron) label = "IRON";
        else if (name.Contains("meteorite") && _settings.Meteorite) label = "METEOR";
        else if (name.Contains("obsidian") && _settings.Obsidian) label = "OBS";
        else if (name.Contains("silvervein") && _settings.Silver) label = "SILVER";
        else if (name.Contains("tin") && _settings.Tin) label = "TIN";
        else if (name.Contains("spawner_skeleton") && _settings.Skeleton) label = "SKEL";
        else if (name.Contains("pickable_magecap") && _settings.MageCaps) label = "MAGECAP";
        else if (name.Contains("pickable_tar") && _settings.Tar) label = "TAR";
        else if (name.Contains("pickable_flax") && _settings.Flax) label = "FLAX";
        else if (name.Contains("pickable_barley") && _settings.Barley) label = "BARLEY";
        else if (name.Contains("pickable_thistle") && _settings.Thistle) label = "THISTLE";
        else if (name.Contains("pickable_dvergerthing") && _settings.DvergerThings) label = "THING";
        else if (name.Contains("mistlands_giant") && _settings.MistlandsGiants) label = "GIANT";
        else if (name.Contains("mistlands_swords") && _settings.MistlandsSwords) label = "SWORDS";
        else if (name.Contains("pickable_dragonegg") && _settings.DragonEgg) label = "DRAGONEGG";
        else if (name.Contains("pickable_voltureegg") && _settings.VoltureEgg) label = "V-EGG";
        else if (name.Contains("pickable_smokepuff") && _settings.SmokePuffs) label = "SMOKEPUFF";
        else if (name.Contains("pickable_yggdrasilpinecone") && _settings.YPCones) label = "Y-PCONE";
        else if (name.Contains("pickable_jotunnpuff") && _settings.JotunnPuffs) label = "J-PUFF";
        else if (name.Contains("rasp") && _settings.RaspBerries) label = "BERRIES";
        else if (name.Contains("blue") && _settings.BlueBerries) label = "BLUE";
        else if (name.Contains("mushroom") && _settings.Mushrooms) label = "SHROOMS";
        else if (name.Contains("cloud") && _settings.CloudBerries) label = "CLOUD";
        else if (name.Contains("pickable_seedcarrot") && _settings.Seeds) label = "CARROT";
        else if (name.Contains("pickable_seedonion") && _settings.Seeds) label = "ONION";
        else if (name.Contains("pickable_seedturnip") && _settings.Seeds) label = "TURNIP";
        else if (name.Contains("treasurechest") && _settings.Treasure) label = "BOX";
        else if (name.Contains("spawner_draugrpile") && _settings.DraugrSpawner) label = "DRAUGRSPAWNER";
        else if (name.Contains("spawner_greydwarfnest") && _settings.DwarfSpawner) label = "DwarfSpawner";
        else if (name.Contains("goblin_totempole") && _settings.Totem) label = "TOTEM";
        return label != null;
    }
}
