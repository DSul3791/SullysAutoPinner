﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SullysAutoPinner;
using BepInEx.Configuration;

public class PinManager
{
    private const int PinFlushThreshold = 50;

    private readonly HashSet<string> _pinHashes = new HashSet<string>();
    private readonly ModConfig _config;
    private readonly ManualLogSource _logger;
    private readonly LocalizationManager _localizationManager;
    private readonly List<Tuple<Vector3, string>> _currentPins = new List<Tuple<Vector3, string>>();
    private readonly List<Tuple<Vector3, string>> _newPins = new List<Tuple<Vector3, string>>();

    private float _lastSaveTime;

    private static readonly string SaveFolder = Path.Combine(Paths.ConfigPath, "SullysAutoPinnerFiles");
    private static readonly string PinsFilePath = Path.Combine(SaveFolder, "Pins.txt");

    public PinManager(ModConfig config, ManualLogSource logger, LocalizationManager localizationManager)
    {
        _config = config;
        _logger = logger;
        _localizationManager = localizationManager;
        Directory.CreateDirectory(Path.GetDirectoryName(PinsFilePath));
        LoadPinsFromFile();
    }
    public void ReloadConfig()
    {
        // Reapply any config-driven behavior here if needed
        // Currently empty — included for future support
    }

    public void TryAddPin(Vector3 pos, string label, Minimap.PinType icon)
    {
        if (Minimap.instance == null) return;

        Vector3 roundedPos = RoundTo0_1(pos);
        string labelUpper = label.ToUpperInvariant();
        string hash = GetPinHash(roundedPos, labelUpper);

        if (_pinHashes.Contains(hash)) return;

        foreach (var existing in _currentPins)
        {
            if (Vector3.Distance(existing.Item1, roundedPos) < _config.PinMergeDistance.Value &&
                string.Equals(existing.Item2, labelUpper, StringComparison.OrdinalIgnoreCase))
                return;
        }

        string localizedLabel = _localizationManager.GetLabel(labelUpper);
        Minimap.instance.AddPin(roundedPos, icon, localizedLabel, true, false);

        _newPins.Add(new Tuple<Vector3, string>(roundedPos, labelUpper));
        _currentPins.Add(new Tuple<Vector3, string>(roundedPos, labelUpper));
        _pinHashes.Add(hash);

        if (_config.EnablePinSaving.Value && _newPins.Count >= PinFlushThreshold)
        {
            SavePinsToFile();
        }
    }

    public void TryPeriodicSave()
    {
        if (!_config.EnablePinSaving.Value || _newPins.Count == 0) return;

        if (_newPins.Count >= PinFlushThreshold || Time.time - _lastSaveTime > _config.SaveInterval.Value)
        {
            SavePinsToFile();
        }
    }

    public void SaveImmediate()
    {
        if (_config.EnablePinSaving.Value)
        {
            SavePinsToFile();
        }
    }

    public void RemoveAllPins()
    {
        if (Minimap.instance == null) return;
        _pinHashes.Clear();

        try
        {
            File.WriteAllText(PinsFilePath, string.Empty);
            _logger.LogWarning("SullysAutoPinner >>> All Map Pins Removed - Pin history file emptied.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SullysAutoPinner >>> Failed to clear Pins.txt: " + ex.Message);
            return;
        }

        var allPins = AccessTools.Field(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>;
        if (allPins == null) return;

        foreach (var pin in allPins.ToList())
        {
            Minimap.instance.RemovePin(pin);
        }

        _currentPins.Clear();
        _newPins.Clear();
        _pinHashes.Clear();

        _logger.LogWarning($"SullysAutoPinner >>> All Map Pins Removed: {allPins.Count} pins removed from the map.");
    }

    public void RemoveUnwantedPins(ModConfig config)
    {
        if (Minimap.instance == null) return;

        var allPins = AccessTools.Field(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>;
        if (allPins == null) return;

        // Step 1: Build a set of all valid localized labels based on enabled config entries
        var validLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in PinScanner.PinLabelMap.LabelToSettingInfo)
        {
            string configKey = kvp.Value.SettingKey;
            var field = typeof(ModConfig).GetField(configKey);
            if (field == null || field.FieldType != typeof(ConfigEntry<bool>)) continue;

            var entry = (ConfigEntry<bool>)field.GetValue(config);
            if (entry != null && entry.Value)
            {
                string localizedLabel = _localizationManager.GetLabel(configKey.ToUpperInvariant());
                validLabels.Add(localizedLabel);
            }
        }

        // Step 2: Remove all pins that don't match the current set of valid labels
        var toRemove = new List<Minimap.PinData>();

        foreach (var pin in allPins)
        {
            if (!validLabels.Contains(pin.m_name))
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

    private bool IsLabelEnabled(string label, ModConfig config)
    {
        if (!PinScanner.PinLabelMap.LabelToSettingInfo.TryGetValue(label, out var settingInfo))
            return true;

        var field = typeof(ModConfig).GetField(settingInfo.SettingKey);
        if (field == null || field.FieldType != typeof(ConfigEntry<bool>))
            return true;

        var entry = (ConfigEntry<bool>)field.GetValue(config);
        return entry.Value;
    }

    private void SavePinsToFile()
    {
        try
        {
            using (StreamWriter writer = File.AppendText(PinsFilePath))
            {
                foreach (var pin in _newPins)
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

        _newPins.Clear();
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
                string label = parts[3].Trim().ToUpperInvariant();

                Vector3 pos = RoundTo0_1(new Vector3(x, y, z));
                string hash = GetPinHash(pos, label);

                _pinHashes.Add(hash);
                _currentPins.Add(new Tuple<Vector3, string>(pos, label));
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

    public void RemoveNearbyFallbackPins(Vector3 center, float radius)
    {
        if (Minimap.instance == null) return;

        var allPins = AccessTools.Field(typeof(Minimap), "m_pins").GetValue(Minimap.instance) as List<Minimap.PinData>;
        if (allPins == null) return;

        var toRemove = new List<Minimap.PinData>();

        foreach (var pin in allPins)
        {
            if (Vector3.Distance(center, pin.m_pos) > radius)
                continue;

            string label = pin.m_name;
            if (!PinScanner.PinLabelMap.LabelToSettingInfo.ContainsKey(label))
            {
                toRemove.Add(pin);
            }
        }

        foreach (var pin in toRemove)
        {
            Minimap.instance.RemovePin(pin);
        }

        _logger.LogWarning($"Removed {toRemove.Count} fallback pins within {radius}m.");
    }
}
