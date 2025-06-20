﻿// Updated ModConfig.cs for SullysAutoPinner v1.4.0

using BepInEx.Configuration;
using BepInEx.Logging;

namespace SullysAutoPinner
{
    public class ModConfig
    {
        private readonly ManualLogSource _logger;

        // Core
        public ConfigEntry<string> PreferredLanguage;
        public ConfigEntry<float> ScanRadius;
        public ConfigEntry<float> ScanInterval;
        public ConfigEntry<float> SaveInterval;
        public ConfigEntry<float> PinMergeDistance;
        public ConfigEntry<bool> EnablePinSaving;
        public ConfigEntry<bool> EnablePrefabLogging;

        // One-time flags
        public ConfigEntry<bool> ClearAllMapPinsAndHistory;
        public ConfigEntry<bool> ClearAllUnwantedPins;
        public ConfigEntry<bool> ClearNearbyFallbackPinsOnNextScan;

        // Detection toggles
        public ConfigEntry<bool> ShowUnmappedPrefabs;

        // Pin toggles
        public ConfigEntry<bool> Abomination;
        public ConfigEntry<bool> AshlandPotGreen;
        public ConfigEntry<bool> AshlandPotRed;
        public ConfigEntry<bool> Barley;
        public ConfigEntry<bool> BlueBerries;
        public ConfigEntry<bool> BonemawSerpent;
        public ConfigEntry<bool> CloudBerries;
        public ConfigEntry<bool> Copper;
        public ConfigEntry<bool> Crypt;
        public ConfigEntry<bool> DragonEgg;
        public ConfigEntry<bool> DraugrSpawner;
        public ConfigEntry<bool> DvergerThings;
        public ConfigEntry<bool> DwarfSpawner;
        public ConfigEntry<bool> Excavation;
        public ConfigEntry<bool> Fire;
        public ConfigEntry<bool> Flax;
        public ConfigEntry<bool> GiantBrain;
        public ConfigEntry<bool> GiantRibs;
        public ConfigEntry<bool> GiantSkull;
        public ConfigEntry<bool> GiantSword;
        public ConfigEntry<bool> Iron;
        public ConfigEntry<bool> JotunPuffs;
        public ConfigEntry<bool> Lantern;
        public ConfigEntry<bool> MageCaps;
        public ConfigEntry<bool> Marker;
        public ConfigEntry<bool> Meteorite;
        public ConfigEntry<bool> MistlandsGiants;
        public ConfigEntry<bool> MistlandsSwords;
        public ConfigEntry<bool> Mudpile;
        public ConfigEntry<bool> Mushrooms;
        public ConfigEntry<bool> Obsidian;
        public ConfigEntry<bool> Onion;
        public ConfigEntry<bool> PickableSmokePuff;
        public ConfigEntry<bool> RaspBerries;
        public ConfigEntry<bool> Seeds;
        public ConfigEntry<bool> SeekerBrute;
        public ConfigEntry<bool> ShipwreckChest;
        public ConfigEntry<bool> Silver;
        public ConfigEntry<bool> Skeleton;
        public ConfigEntry<bool> SmokePuffs;
        public ConfigEntry<bool> Tar;
        public ConfigEntry<bool> Thistle;
        public ConfigEntry<bool> Tin;
        public ConfigEntry<bool> Totem;
        public ConfigEntry<bool> Treasure;
        public ConfigEntry<bool> TrollCave;
        public ConfigEntry<bool> Turnip;
        public ConfigEntry<bool> VoltureEgg;
        public ConfigEntry<bool> YPCones;

        public ModConfig(ConfigFile config, ManualLogSource logger)
        {
            _logger = logger;

            // Core
            PreferredLanguage = config.Bind("Localization", "LanguageCode", "en", "Language code for localization (en, fr, ru, etc).");
            ScanRadius = config.Bind("Scanning", "ScanRadius", 300f, "Radius (in meters) to scan for prefabs.");
            ScanInterval = config.Bind("Scanning", "ScanInterval", 12f, "Time (in seconds) between environment scans.");
            SaveInterval = config.Bind("Persistence", "SaveInterval", 120f, "How often to save pins to disk (in seconds).");
            PinMergeDistance = config.Bind("Pins", "PinMergeDistance", 50f, "Distance (in meters) at which duplicate pins are merged.");
            EnablePinSaving = config.Bind("Persistence", "EnablePinSaving", true, "Enable saving of detected pins.");
            EnablePrefabLogging = config.Bind("Debug", "EnablePrefabLogging", false, "Enable logging of prefab names and hierarchy for debugging.");

            // One-time flags
            ClearAllMapPinsAndHistory = config.Bind("Admin", "ClearAllMapPinsAndHistory", false, "Clear all pins and reset history file on next run.");
            ClearAllUnwantedPins = config.Bind("Admin", "ClearAllUnwantedPins", false, "Clear only pins that do not match active settings.");
            ClearNearbyFallbackPinsOnNextScan = config.Bind("Admin", "ClearNearbyFallbackPinsOnNextScan", false, "Remove fallback pins near the player on next scan.");

            // Behavior
            ShowUnmappedPrefabs = config.Bind("Fallback", "ShowUnmappedPrefabs", false, "Create pins for unknown prefabs using prefab name.");

            // Pins
            Mushrooms = config.Bind("Pins", "Mushrooms", false, "");
            RaspBerries = config.Bind("Pins", "RaspBerries", false, "");
            BlueBerries = config.Bind("Pins", "BlueBerries", false, "");
            Seeds = config.Bind("Pins", "Seeds", false, "");
            Thistle = config.Bind("Pins", "Thistle", false, "");
            CloudBerries = config.Bind("Pins", "CloudBerries", false, "");
            Copper = config.Bind("Pins", "Copper", true, "");
            Tin = config.Bind("Pins", "Tin", false, "");
            Skeleton = config.Bind("Pins", "Skeleton", false, "");
            DwarfSpawner = config.Bind("Pins", "DwarfSpawner", true, "");
            TrollCave = config.Bind("Pins", "TrollCave", true, "");
            Crypt = config.Bind("Pins", "Crypt", true, "");
            Totem = config.Bind("Pins", "Totem", true, "");
            Fire = config.Bind("Pins", "Fire", true, "");
            DraugrSpawner = config.Bind("Pins", "DraugrSpawner", false, "");
            Treasure = config.Bind("Pins", "Treasure", false, "");
            Barley = config.Bind("Pins", "Barley", false, "");
            Flax = config.Bind("Pins", "Flax", false, "");
            Tar = config.Bind("Pins", "Tar", true, "");
            DragonEgg = config.Bind("Pins", "DragonEgg", true, "");
            VoltureEgg = config.Bind("Pins", "VoltureEgg", true, "");
            SmokePuffs = config.Bind("Pins", "SmokePuffs", false, "");
            MageCaps = config.Bind("Pins", "MageCaps", true, "");
            YPCones = config.Bind("Pins", "YPCones", true, "");
            JotunPuffs = config.Bind("Pins", "JotunPuffs", true, "");
            Iron = config.Bind("Pins", "Iron", true, "");
            Mudpile = config.Bind("Pins", "Mudpile", true, "");
            Abomination = config.Bind("Pins", "Abomination", true, "");
            Meteorite = config.Bind("Pins", "Meteorite", true, "");
            Obsidian = config.Bind("Pins", "Obsidian", true, "");
            Silver = config.Bind("Pins", "Silver", true, "");
            MistlandsGiants = config.Bind("Pins", "MistlandsGiants", true, "");
            MistlandsSwords = config.Bind("Pins", "MistlandsSwords", true, "");
            DvergerThings = config.Bind("Pins", "DvergerThings", true, "");
            BonemawSerpent = config.Bind("Pins", "BonemawSerpent", true, "");
            GiantSword = config.Bind("Pins", "GiantSword", true, "");
            GiantRibs = config.Bind("Pins", "GiantRibs", true, "");
            GiantSkull = config.Bind("Pins", "GiantSkull", true, "");
            GiantBrain = config.Bind("Pins", "GiantBrain", true, "");
            Lantern = config.Bind("Pins", "Lantern", true, "");
            Excavation = config.Bind("Pins", "Excavation", true, "");
            AshlandPotGreen = config.Bind("Pins", "AshlandPotGreen", true, "");
            AshlandPotRed = config.Bind("Pins", "AshlandPotRed", true, "");
            ShipwreckChest = config.Bind("Pins", "ShipwreckChest", true, "");
            SeekerBrute = config.Bind("Pins", "SeekerBrute", true, "");
            Marker = config.Bind("Pins", "Marker", true, "");
        }
    }
}
