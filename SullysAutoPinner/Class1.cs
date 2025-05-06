// SullysAutoPinner.cs - Version 1.1.0 Refactor
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using BepInEx;

[BepInPlugin("sullys.autopinner", "Sullys Auto Pinner", "1.1.0")]
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

        Logger.LogWarning("SullysAutoPinner v1.1.0 Initialized");
    }

    private void Update()
    {
        if (Player.m_localPlayer == null) return;

        if (Time.time - _lastScanTime > _settings.ScanInterval)
        {
            _lastScanTime = Time.time;
            Logger.LogWarning("Running Scan");
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

// Additional classes appended below

// [PinSettings, ConfigLoader, PinManager, PrefabTracker, PinScanner]
// are all appended in full and C# 7.3 compliant

// BEGIN PinSettings
public class PinSettings
{
    public float ScanRadius = 300f;
    public float ScanInterval = 12f;
    public float SaveInterval = 60f;
    public float PinMergeDistance = 30f;
    public bool EnablePinSaving = true;
    public bool EnablePrefabLogging = false;
    public bool Mushrooms = false;
    public bool RaspBerries = false;
    public bool BlueBerries = false;
    public bool Carrots = false;
    public bool Thistle = false;
    public bool CloudBerries = true;
    public bool Copper = true;
    public bool Tin = false;
    public bool Skeleton = false;
    public bool DwarfSpawner = true;
    public bool TrollCave = true;
    public bool Crypt = true;
    public bool Totem = true;
    public bool DraugrSpawner = true;
    public bool Treasure = true;
    public bool Barley = true;
    public bool Flax = true;
    public bool Tar = true;
    public bool DragonEgg = true;
}

public class ConfigLoader
{
    private const string SaveFolder = "BepInEx/config/SullysAutoPinnerFiles/";
    private const string ConfigFilePath = SaveFolder + "SullysAutoPinner.cfg";
    private readonly BepInEx.Logging.ManualLogSource _logger;

    public ConfigLoader(BepInEx.Logging.ManualLogSource logger)
    {
        _logger = logger;
        Directory.CreateDirectory(SaveFolder);
    }

    public PinSettings Load()
    {
        var settings = new PinSettings();

        if (!File.Exists(ConfigFilePath))
        {
            SaveDefault(settings);
            return settings;
        }

        try
        {
            string[] lines = File.ReadAllLines(ConfigFilePath);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;

                string[] parts = trimmed.Split(new[] { '=' }, 2);
                string key = parts[0].Trim().ToLowerInvariant();
                string value = parts[1].Trim().ToLowerInvariant();

                bool flag = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                float num;

                switch (key)
                {
                    case "scanradius": if (float.TryParse(value, out num)) settings.ScanRadius = num; break;
                    case "scaninterval": if (float.TryParse(value, out num)) settings.ScanInterval = num; break;
                    case "saveinterval": if (float.TryParse(value, out num)) settings.SaveInterval = num; break;
                    case "pinmergedistance": if (float.TryParse(value, out num)) settings.PinMergeDistance = num; break;
                    case "enablepinsaving": settings.EnablePinSaving = flag; break;
                    case "enableprefablogging": settings.EnablePrefabLogging = flag; break;
                    case "mushrooms": settings.Mushrooms = flag; break;
                    case "raspberries": settings.RaspBerries = flag; break;
                    case "blueberries": settings.BlueBerries = flag; break;
                    case "carrots": settings.Carrots = flag; break;
                    case "thistle": settings.Thistle = flag; break;
                    case "cloudberries": settings.CloudBerries = flag; break;
                    case "copper": settings.Copper = flag; break;
                    case "tin": settings.Tin = flag; break;
                    case "skeleton": settings.Skeleton = flag; break;
                    case "dwarfspawner": settings.DwarfSpawner = flag; break;
                    case "trollcave": settings.TrollCave = flag; break;
                    case "crypt": settings.Crypt = flag; break;
                    case "totem": settings.Totem = flag; break;
                    case "draugrspawner": settings.DraugrSpawner = flag; break;
                    case "treasure": settings.Treasure = flag; break;
                    case "barley": settings.Barley = flag; break;
                    case "flax": settings.Flax = flag; break;
                    case "tar": settings.Tar = flag; break;
                    case "dragonegg": settings.DragonEgg = flag; break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error reading config: " + ex.Message);
        }

        return settings;
    }

    private void SaveDefault(PinSettings settings)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(ConfigFilePath))
            {
                writer.WriteLine("# SullysAutoPinner config");
                writer.WriteLine("scanradius=300");
                writer.WriteLine("scaninterval=12");
                writer.WriteLine("saveinterval=60");
                writer.WriteLine("pinmergedistance=30");

                foreach (var field in typeof(PinSettings).GetFields())
                {
                    if (field.FieldType == typeof(bool))
                    {
                        writer.WriteLine(field.Name.ToLowerInvariant() + "=" + ((bool)field.GetValue(settings)).ToString().ToLower());
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


public class PinManager
{
    private const int PinFlushThreshold = 250;
    private readonly List<Tuple<Vector3, string>> _pinQueue = new List<Tuple<Vector3, string>>();
    private readonly HashSet<string> _pinHashes = new HashSet<string>();
    private readonly PinSettings _settings;
    private readonly BepInEx.Logging.ManualLogSource _logger;
    private float _lastSaveTime;
    private const string PinsFilePath = "BepInEx/config/SullysAutoPinnerFiles/Pins.txt";

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

        foreach (var existing in _pinQueue)
        {
            if (Vector3.Distance(existing.Item1, pos) < _settings.PinMergeDistance) return;
        }

        string hash = GetPinHash(pos, label);
        if (_pinHashes.Contains(hash)) return;

        Minimap.instance.AddPin(pos, Minimap.PinType.Icon3, label, true, false);

        _pinQueue.Add(new Tuple<Vector3, string>(pos, label));
        _pinHashes.Add(hash);

        _logger.LogWarning("Pin Placed: " + label);

        if (_settings.EnablePinSaving && _pinQueue.Count >= PinFlushThreshold)
        {
            SavePinsToFile();
        }
    }

    public void TryPeriodicSave()
    {
        if (!_settings.EnablePinSaving) return;
        if (_pinQueue.Count == 0) return;
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
            _logger.LogWarning("Saved pins to file: " + PinsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error saving pins: " + ex.Message);
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
            _logger.LogWarning("Error loading pins: " + ex.Message);
        }
    }

    private string GetPinHash(Vector3 pos, string label)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:F1},{2:F1},{3:F1}", label, pos.x, pos.y, pos.z);
    }
}


public class PrefabTracker
{
    private readonly HashSet<string> _seenPrefabs = new HashSet<string>();
    private readonly BepInEx.Logging.ManualLogSource _logger;
    private readonly PinSettings _settings;
    private float _lastPrefabDumpTime;
    private const float DumpInterval = 300f; // 5 minutes
    private const int PrefabFlushThreshold = 1000;
    private const string PrefabsFilePath = "BepInEx/config/SullysAutoPinnerFiles/SeenPrefabs.txt";

    private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>
    {
        "rock", "tree", "bush", "log", "stone", "branch"
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

            _logger.LogWarning("New prefab structures recorded to SeenPrefabs.txt");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error saving prefab list: " + ex.Message);
        }
    }
}

// BEGIN PinScanner
public class PinScanner
{
    private readonly PinSettings _settings;
    private readonly PinManager _pinManager;
    private readonly PrefabTracker _prefabTracker;

    private static readonly Dictionary<string, Tuple<string, Func<PinSettings, bool>>> StaticPinMappings = new Dictionary<string, Tuple<string, Func<PinSettings, bool>>>
    {
        { "goblin_totempole", Tuple.Create("TOTEM", new Func<PinSettings, bool>(s => s.Totem)) },
        { "spawner_greydwarfnest", Tuple.Create("DwarfSpawner", new Func<PinSettings, bool>(s => s.DwarfSpawner)) },
        { "spawner_draugrpile", Tuple.Create("DRAUGRSPAWNER", new Func<PinSettings, bool>(s => s.DraugrSpawner)) },
        { "treasurechest_meadows", Tuple.Create("BOX", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_blackforest", Tuple.Create("BOX", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_heath", Tuple.Create("BOX", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_mountains", Tuple.Create("BOX", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_forestcrypt", Tuple.Create("BOX", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "pickable_barley_wild", Tuple.Create("BARLEY", new Func<PinSettings, bool>(s => s.Barley)) },
        { "pickable_flax_wild", Tuple.Create("FLAX", new Func<PinSettings, bool>(s => s.Flax)) },
        { "pickable_tar", Tuple.Create("TAR", new Func<PinSettings, bool>(s => s.Tar)) },
        { "pickable_tarbig", Tuple.Create("TAR", new Func<PinSettings, bool>(s => s.Tar)) },
        { "pickable_dragonegg", Tuple.Create("DRAGONEGG", new Func<PinSettings, bool>(s => s.DragonEgg)) },
        { "rasp", Tuple.Create("BERRIES", new Func<PinSettings, bool>(s => s.RaspBerries)) },
        { "blue", Tuple.Create("BLUE", new Func<PinSettings, bool>(s => s.BlueBerries)) },
        { "mushroom", Tuple.Create("SHROOMS", new Func<PinSettings, bool>(s => s.Mushrooms)) },
        { "cloud", Tuple.Create("CLOUD", new Func<PinSettings, bool>(s => s.CloudBerries)) },
        { "copper", Tuple.Create("COPPER", new Func<PinSettings, bool>(s => s.Copper)) },
        { "_tin", Tuple.Create("TIN", new Func<PinSettings, bool>(s => s.Tin)) },
        { "spawner_skeleton", Tuple.Create("SKEL", new Func<PinSettings, bool>(s => s.Skeleton)) },
        { "pickable_seedcarrot", Tuple.Create("CARROTS", new Func<PinSettings, bool>(s => s.Carrots)) },
        { "pickable_thistle", Tuple.Create("THISTLE", new Func<PinSettings, bool>(s => s.Thistle)) }
    };

    public PinScanner(PinSettings settings, PinManager pinManager, PrefabTracker prefabTracker)
    {
        _settings = settings;
        _pinManager = pinManager;
        _prefabTracker = prefabTracker;
    }

    public void RunScan(Vector3 playerPosition)
    {
        Collider[] hits = Physics.OverlapSphere(playerPosition, _settings.ScanRadius, ~0, QueryTriggerInteraction.Collide);

        List<GameObject> trollCaves = new List<GameObject>();
        List<KeyValuePair<GameObject, string>> otherPins = new List<KeyValuePair<GameObject, string>>();

        foreach (Collider col in hits)
        {
            GameObject root = col.transform.root.gameObject;
            if (root == null) continue;

            _prefabTracker.LogPrefabHierarchy(root);

            string name = root.name.ToLowerInvariant();

            if (name.Contains("locationproxy"))
            {
                HandleLocationProxyChildren(root, trollCaves, otherPins);
                continue;
            }

            string label;
            if (IsPinWorthy(name, out label))
            {
                otherPins.Add(new KeyValuePair<GameObject, string>(root, label));
            }
        }

        HashSet<string> seenHashes = new HashSet<string>();
        foreach (GameObject obj in trollCaves)
        {
            Vector3 pos = obj.transform.position;
            string hash = GetPinHash(pos, "TrollCave");
            if (seenHashes.Add(hash))
            {
                _pinManager.TryAddPin(pos, "TrollCave");
            }
        }

        foreach (var pair in otherPins)
        {
            _pinManager.TryAddPin(pair.Key.transform.position, pair.Value);
        }
    }

    private bool IsPinWorthy(string name, out string label)
    {
        label = null;
        foreach (var kvp in StaticPinMappings)
        {
            if (name.Contains(kvp.Key) && kvp.Value.Item2(_settings))
            {
                label = kvp.Value.Item1;
                return true;
            }
        }
        return false;
    }



    private static readonly HashSet<string> IgnoredLocationProxyChildren = new HashSet<string>
{
    "runestone",
    "eikthyrnir",
    "starttemple"
};


    private void HandleLocationProxyChildren(GameObject root, List<GameObject> trollCaves, List<KeyValuePair<GameObject, string>> otherPins)
    {
        if (trollCaves.Contains(root)) return;

        string fallbackLabel = null;

        foreach (Transform child in root.transform)
        {
            string childName = child.name.ToLowerInvariant();

            foreach (string ignore in IgnoredLocationProxyChildren)
            {
                if (childName.Contains(ignore))
                {
                    // ✅ Immediately skip the entire proxy
                    return;
                }
            }

            if (childName.Contains("trollcave") && _settings.TrollCave)
            {
                trollCaves.Add(root);
                return;
            }

            if ((childName.Contains("crypt2") || childName.Contains("crypt3") || childName.Contains("crypt4")) && _settings.Crypt)
            {
                otherPins.Add(new KeyValuePair<GameObject, string>(root, "CRYPT"));
                return;
            }

            if (fallbackLabel == null)
                fallbackLabel = child.name;
        }

        if (!string.IsNullOrEmpty(fallbackLabel))
        {
            string cleanedLabel = fallbackLabel.Replace("(Clone)", "").Trim();
            otherPins.Add(new KeyValuePair<GameObject, string>(root, cleanedLabel));
        }

    }


    private string GetPinHash(Vector3 pos, string label)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:F1},{2:F1},{3:F1}", label, pos.x, pos.y, pos.z);
    }
}
