// SullysAutoPinner.cs - Handles LocationProxies via child names with fallback labeling
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using BepInEx;

[BepInPlugin("sullys.autopinner", "Sullys Auto Pinner", "1.0.0")]
public class SullysAutoPinner : BaseUnityPlugin
{
    private const string SaveFolder = "BepInEx/config/SullysAutoPinnerFiles/";
    private const string PinsFilePath = SaveFolder + "Pins.txt";
    private const string PrefabsFilePath = SaveFolder + "SeenPrefabs.txt";
    private const string ConfigFilePath = SaveFolder + "SullysAutoPinner.cfg";

    private PinSettings _settings;
    private readonly List<Tuple<Vector3, string>> _pinQueue = new List<Tuple<Vector3, string>>();
    private readonly HashSet<string> _pinHashes = new HashSet<string>();
    private readonly HashSet<string> _seenPrefabs = new HashSet<string>();
    private float _lastSaveTime;
    private float _lastScanTime;
    private float _lastPrefabDumpTime;
    private int _unsavedPins;

    private void Awake()
    {
        Directory.CreateDirectory(SaveFolder);

        Logger.LogWarning("SullysAutoPinner Loaded");
        Logger.LogWarning("Loading config from: " + ConfigFilePath);

        _settings = new PinSettings();
        LoadConfig();
        LoadPinsFromFile();
    }

    private void Update()
    {
        if (Player.m_localPlayer == null) return;

        if (Time.time - _lastScanTime > _settings.ScanInterval)
        {
            _lastScanTime = Time.time;
            Logger.LogWarning("Starting Scan");
            RunScanLogic();
        }

        if (_settings.EnablePinSaving && _unsavedPins > 0 && Time.time - _lastSaveTime > _settings.SaveInterval)
        {
            SavePinsToFile();
        }

        if (Time.time - _lastPrefabDumpTime > 300f)
        {
            SaveSeenPrefabs();
        }
    }

    private void OnApplicationQuit()
    {
        if (_settings.EnablePinSaving && _unsavedPins > 0)
        {
            SavePinsToFile();
        }
        SaveSeenPrefabs();
    }

    private static readonly Dictionary<string, Tuple<string, Func<PinSettings, bool>>> StaticPinMappings = new Dictionary<string, Tuple<string, Func<PinSettings, bool>>>
    {
        { "goblin_totempole", Tuple.Create("TOTEM", new Func<PinSettings, bool>(s => s.Totem)) },
        { "spawner_greydwarfnest", Tuple.Create("DwarfSpawner", new Func<PinSettings, bool>(s => s.DwarfSpawner)) },
        { "spawner_draugrpile", Tuple.Create("DRAUGRSPAWNER", new Func<PinSettings, bool>(s => s.DraugrSpawner)) },
        { "treasurechest_meadows", Tuple.Create("TREASURE", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_blackforest", Tuple.Create("TREASURE", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_heath", Tuple.Create("TREASURE", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_mountains", Tuple.Create("TREASURE", new Func<PinSettings, bool>(s => s.Treasure)) },
        { "treasurechest_forestcrypt", Tuple.Create("TREASURE", new Func<PinSettings, bool>(s => s.Treasure)) },
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

    private class PinSettings
    {
        public float ScanRadius = 50f;
        public float ScanInterval = 12f;
        public float SaveInterval = 60f;
        public float PinMergeDistance = 30f;
        public bool EnablePinSaving = true;
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

    private void SaveDefaultConfig()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(ConfigFilePath))
            {
                writer.WriteLine("# SullysAutoPinner config");
                writer.WriteLine("scanradius=500");
                writer.WriteLine("scaninterval=12");
                writer.WriteLine("saveinterval=60");
                writer.WriteLine("pinmergedistance=30");
                writer.WriteLine("mushrooms=false");
                writer.WriteLine("raspberries=false");
                writer.WriteLine("blueberries=false");
                writer.WriteLine("carrots=false");
                writer.WriteLine("thistle=false");
                writer.WriteLine("cloudberries=true");
                writer.WriteLine("copper=true");
                writer.WriteLine("tin=false");
                writer.WriteLine("skeleton=false");
                writer.WriteLine("dwarfspawner=true");
                writer.WriteLine("trollcave=true");
                writer.WriteLine("crypt=true");
                writer.WriteLine("totem=true");
                writer.WriteLine("draugrspawner=true");
                writer.WriteLine("treasure=true");
                writer.WriteLine("barley=true");
                writer.WriteLine("flax=true");
                writer.WriteLine("tar=true");
                writer.WriteLine("dragonegg=true");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Error saving default config: " + ex.Message);
        }
    }

    private void RunScanLogic()
    {
        Vector3 playerPos = Player.m_localPlayer.transform.position;
        Collider[] hits = Physics.OverlapSphere(playerPos, _settings.ScanRadius, ~0, QueryTriggerInteraction.Collide);

        List<GameObject> trollCaves = new List<GameObject>();
        List<KeyValuePair<GameObject, string>> otherPins = new List<KeyValuePair<GameObject, string>>();

        foreach (Collider col in hits)
        {
            GameObject root = col.transform.root.gameObject;
            if (root == null) continue;

            LogPrefabHierarchy(root);

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
                TryAddPin(pos, "TrollCave");
            }
        }

        foreach (var pair in otherPins)
        {
            TryAddPin(pair.Key.transform.position, pair.Value);
        }
    }

    private void LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            SaveDefaultConfig();
            return;
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

                bool flag = value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);


                switch (key)
                {
                    case "scanradius": float.TryParse(value, out _settings.ScanRadius); break;
                    case "scaninterval": float.TryParse(value, out _settings.ScanInterval); break;
                    case "saveinterval": float.TryParse(value, out _settings.SaveInterval); break;
                    case "pinmergedistance": float.TryParse(value, out _settings.PinMergeDistance); break;
                    case "mushrooms": _settings.Mushrooms = flag; break;
                    case "raspberries": _settings.RaspBerries = flag; break;
                    case "blueberries": _settings.BlueBerries = flag; break;
                    case "carrots": _settings.Carrots = flag; break;
                    case "thistle": _settings.Thistle = flag; break;
                    case "cloudberries": _settings.CloudBerries = flag; break;
                    case "copper": _settings.Copper = flag; break;
                    case "tin": _settings.Tin = flag; break;
                    case "skeleton": _settings.Skeleton = flag; break;
                    case "dwarfspawner": _settings.DwarfSpawner = flag; break;
                    case "trollcave": _settings.TrollCave = flag; break;
                    case "crypt": _settings.Crypt = flag; break;
                    case "totem": _settings.Totem = flag; break;
                    case "draugrspawner": _settings.DraugrSpawner = flag; break;
                    case "treasure": _settings.Treasure = flag; break;
                    case "barley": _settings.Barley = flag; break;
                    case "flax": _settings.Flax = flag; break;
                    case "tar": _settings.Tar = flag; break;
                    case "dragonegg": _settings.DragonEgg = flag; break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Error reading config: " + ex.Message);
        }
    }

    private void LoadPinsFromFile()
    {
        try
        {
            if (!File.Exists(PinsFilePath)) return;

            string[] lines = File.ReadAllLines(PinsFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length != 4) continue;

                float x, y, z;
                if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) continue;
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)) continue;
                string label = parts[3];

                Vector3 pos = new Vector3(x, y, z);
                string hash = GetPinHash(pos, label);
                _pinHashes.Add(hash);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Error loading pins: " + ex.Message);
        }
    }

    private void SavePinsToFile()
    {
        try
        {
            using (StreamWriter writer = File.AppendText(PinsFilePath))
            {
                foreach (Tuple<Vector3, string> pin in _pinQueue)
                {
                    string line = string.Format(CultureInfo.InvariantCulture, "{0:F1}|{1:F1}|{2:F1}|{3}",
                        pin.Item1.x, pin.Item1.y, pin.Item1.z, pin.Item2);
                    writer.WriteLine(line);
                }
            }
            Logger.LogWarning("Saved pins to file: " + PinsFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Error saving pins: " + ex.Message);
        }

        _pinQueue.Clear();
        _unsavedPins = 0;
        _lastSaveTime = Time.time;
    }

    private void SaveSeenPrefabs()
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

            Logger.LogWarning("New prefab structures recorded to SeenPrefabs.txt");
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Error saving prefab list: " + ex.Message);
        }
    }

    private void LogPrefabHierarchy(GameObject rootGO)
    {
        if (rootGO == null) return;

        string rootName = rootGO.name;
        List<string> childNames = new List<string>();

        foreach (Transform child in rootGO.transform)
        {
            if (child != null)
                childNames.Add(child.name);
        }

        childNames.Sort();
        string entry = rootName + " | " + string.Join(",", childNames);

        _seenPrefabs.Add(entry);
    }

    private void TryAddPin(Vector3 pos, string label)
    {
        if (Minimap.instance == null) return;

        foreach (Tuple<Vector3, string> existing in _pinQueue)
        {
            if (Vector3.Distance(existing.Item1, pos) < _settings.PinMergeDistance) return;
        }

        string hash = GetPinHash(pos, label);
        if (_pinHashes.Contains(hash)) return;

        Minimap.instance.AddPin(pos, Minimap.PinType.Icon3, label, true, false);

        _pinQueue.Add(new Tuple<Vector3, string>(pos, label));
        _pinHashes.Add(hash);
        _unsavedPins++;

        Logger.LogWarning("Pin Placed: " + label);

        if (_settings.EnablePinSaving && _unsavedPins >= 30)
        {
            SavePinsToFile();
        }
    }

    private void HandleLocationProxyChildren(GameObject root, List<GameObject> trollCaves, List<KeyValuePair<GameObject, string>> otherPins)
    {
        if (trollCaves.Contains(root)) return;

        string fallbackLabel = null;

        foreach (Transform child in root.transform)
        {
            string childName = child.name.ToLowerInvariant();

            if (childName.Contains("runestone")) continue;

            if (childName.Contains("trollcave") && _settings.TrollCave)
            {
                trollCaves.Add(root);
                return;
            }

            if ((childName.Contains("crypt2") || childName.Contains("crypt4")) && _settings.Crypt)
            {
                otherPins.Add(new KeyValuePair<GameObject, string>(root, "CRYPT"));
                return;
            }

            if (fallbackLabel == null) fallbackLabel = child.name;
        }

        if (!string.IsNullOrEmpty(fallbackLabel))
        {
            otherPins.Add(new KeyValuePair<GameObject, string>(root, fallbackLabel));
        }
    }

    private string GetPinHash(Vector3 pos, string label)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:F1},{2:F1},{3:F1}", label, pos.x, pos.y, pos.z);
    }
}
