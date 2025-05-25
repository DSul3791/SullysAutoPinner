using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using SimpleJSON;

public class LocalizationManager
{
    private readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingKeys = new HashSet<string>();
    private readonly ManualLogSource _logger;
    private readonly string _langCode;
    private readonly string _translationsFolder;
    private const string FallbackLang = "en";

    public LocalizationManager(ManualLogSource logger, string langCode)
    {
        _logger = logger;
        _langCode = string.IsNullOrWhiteSpace(langCode) ? FallbackLang : langCode.ToLowerInvariant();
        _translationsFolder = Path.Combine(Paths.ConfigPath, "SullysAutoPinnerFiles", "Translations");

        LoadTranslations(FallbackLang); // Load fallback first
        if (_langCode != FallbackLang)
            LoadTranslations(_langCode);
    }

    private void LoadTranslations(string lang)
    {
        try
        {
            string filePath = Path.Combine(_translationsFolder, $"{lang}.json");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"[LocalizationManager] Translation file not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            var parsed = JSON.Parse(json);

            if (parsed == null || !parsed.IsObject)
            {
                _logger.LogWarning($"[LocalizationManager] Failed to parse {filePath}");
                return;
            }

            foreach (var kv in parsed.AsObject)
                _translations[kv.Key.ToUpperInvariant()] = kv.Value.Value;

            _logger.LogWarning($"[LocalizationManager] Loaded {parsed.Count} entries from {lang}.json");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[LocalizationManager] Error loading {lang}.json: {ex.Message}");
        }
    }

    public string GetLabel(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;

        if (_translations.TryGetValue(key.ToUpperInvariant(), out var localized))
            return localized;

        if (_missingKeys.Add(key))
            _logger.LogWarning($"[LocalizationManager] Missing translation for label: {key}");

        return key;
    }

    public static void EnsureBaseLanguageFiles(ManualLogSource logger)
    {
        var folder = Path.Combine(Paths.ConfigPath, "SullysAutoPinnerFiles", "Translations");
        Directory.CreateDirectory(folder);

        // Same label-to-language map as before
        var translations = GetDefaultTranslations();

        foreach (var pair in translations)
        {
            string lang = pair.Key;
            string path = Path.Combine(folder, lang + ".json");

            if (File.Exists(path)) continue;

            try
            {
                var root = new JSONObject();
                foreach (var kv in pair.Value)
                    root[kv.Key] = kv.Value;

                File.WriteAllText(path, root.ToString(2));
                logger.LogWarning($"[LocalizationManager] Created default translation file: {lang}.json");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[LocalizationManager] Failed to write {lang}.json: {ex.Message}");
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> GetDefaultTranslations()
    {
        // Paste the full real-language translation dictionary from earlier here.
        // Omitted for brevity — include same structure as earlier post.
        return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Copper",
                ["IRON"] = "Iron",
                ["SILVER"] = "Silver",
                ["OBSIDIAN"] = "Obsidian",
                ["MUDPILE"] = "Mudpile",
                ["METEORITE"] = "Meteorite",
                ["TROLLCAVE"] = "Troll Cave",
                ["CRYPT"] = "Crypt",
                ["ABOMINATION"] = "Abomination",
                ["FLAX"] = "Flax",
                ["BARLEY"] = "Barley",
                ["BLUEBERRIES"] = "Blueberries",
                ["RASPBERRIES"] = "Raspberries",
                ["MUSHROOMS"] = "Mushrooms",
                ["CLOUDBERRIES"] = "Cloudberries",
                ["THISTLE"] = "Thistle",
                ["SEEDS"] = "Seeds",
                ["MAGECAPS"] = "Magecaps",
                ["YPCONES"] = "Yggdrasil Pine Cones",
                ["JOTUNNPUFFS"] = "Jotunn Puffs",
                ["VOLTUREEGG"] = "Vulture Egg",
                ["DRAGONEGG"] = "Dragon Egg",
                ["SMOKEPUFFS"] = "Smoke Puffs",
                ["DVERGERTHINGS"] = "Dvergr Remains",
                ["DWARFSPAWNER"] = "Greydwarf Nest",
                ["DRAUGRSPAWNER"] = "Draugr Spawner",
                ["TOTEM"] = "Fuling Totem",
                ["FIRE"] = "Campfire",
                ["TAR"] = "Tar Pit",
                ["SKELETON"] = "Skeleton",
                ["TREASURE"] = "Buried Treasure",
                ["MISTLANDSSWORDS"] = "Sword in the Stone",
                ["MISTLANDSGIANTS"] = "Giant Remains",
                ["MARKER"] = "Marker"
            },
            ["ru"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Медь",
                ["IRON"] = "Железо",
                ["SILVER"] = "Серебро",
                ["OBSIDIAN"] = "Обсидиан",
                ["MUDPILE"] = "Грязевая куча",
                ["METEORITE"] = "Метеорит",
                ["TROLLCAVE"] = "Пещера тролля",
                ["CRYPT"] = "Крипта",
                ["ABOMINATION"] = "Мерзость",
                ["FLAX"] = "Лён",
                ["BARLEY"] = "Ячмень",
                ["BLUEBERRIES"] = "Черника",
                ["RASPBERRIES"] = "Малина",
                ["MUSHROOMS"] = "Грибы",
                ["CLOUDBERRIES"] = "Морошка",
                ["THISTLE"] = "Чертополох",
                ["SEEDS"] = "Семена",
                ["MAGECAPS"] = "Волшебные шляпки",
                ["YPCONES"] = "Шишки Иггдрасиля",
                ["JOTUNNPUFFS"] = "Пух Йотуна",
                ["VOLTUREEGG"] = "Яйцо стервятника",
                ["DRAGONEGG"] = "Яйцо дракона",
                ["SMOKEPUFFS"] = "Клубы дыма",
                ["DVERGERTHINGS"] = "Останки двергров",
                ["DWARFSPAWNER"] = "Гнездо серого карлика",
                ["DRAUGRSPAWNER"] = "Портал драугров",
                ["TOTEM"] = "Тотем фулингов",
                ["FIRE"] = "Костер",
                ["TAR"] = "Смоляная яма",
                ["SKELETON"] = "Скелет",
                ["TREASURE"] = "Сокровище",
                ["MISTLANDSSWORDS"] = "Меч в камне",
                ["MISTLANDSGIANTS"] = "Останки великанов",
                ["MARKER"] = "Маркер"
            },
            ["fr"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Cuivre",
                ["IRON"] = "Fer",
                ["SILVER"] = "Argent",
                ["OBSIDIAN"] = "Obsidienne",
                ["MUDPILE"] = "Tas de boue",
                ["METEORITE"] = "Météorite",
                ["TROLLCAVE"] = "Grotte du troll",
                ["CRYPT"] = "Crypte",
                ["ABOMINATION"] = "Abomination",
                ["FLAX"] = "Lin",
                ["BARLEY"] = "Orge",
                ["BLUEBERRIES"] = "Myrtilles",
                ["RASPBERRIES"] = "Framboises",
                ["MUSHROOMS"] = "Champignons",
                ["CLOUDBERRIES"] = "Plaquebières",
                ["THISTLE"] = "Chardon",
                ["SEEDS"] = "Graines",
                ["MAGECAPS"] = "Chapeaux de mage",
                ["YPCONES"] = "Cônes d’Yggdrasil",
                ["JOTUNNPUFFS"] = "Poussières de Jotunn",
                ["VOLTUREEGG"] = "Œuf de vautour",
                ["DRAGONEGG"] = "Œuf de dragon",
                ["SMOKEPUFFS"] = "Nuages de fumée",
                ["DVERGERTHINGS"] = "Restes de Dvergr",
                ["DWARFSPAWNER"] = "Nid de nain gris",
                ["DRAUGRSPAWNER"] = "Portail Draugr",
                ["TOTEM"] = "Totem de Fuling",
                ["FIRE"] = "Feu de camp",
                ["TAR"] = "Fosse à goudron",
                ["SKELETON"] = "Squelette",
                ["TREASURE"] = "Trésor",
                ["MISTLANDSSWORDS"] = "Épée dans la pierre",
                ["MISTLANDSGIANTS"] = "Restes de géants",
                ["MARKER"] = "Marqueur"
            },
            ["de"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Kupfer",
                ["IRON"] = "Eisen",
                ["SILVER"] = "Silber",
                ["OBSIDIAN"] = "Obsidian",
                ["MUDPILE"] = "Schlammhaufen",
                ["METEORITE"] = "Meteorit",
                ["TROLLCAVE"] = "Trollhöhle",
                ["CRYPT"] = "Gruft",
                ["ABOMINATION"] = "Abscheulichkeit",
                ["FLAX"] = "Flachs",
                ["BARLEY"] = "Gerste",
                ["BLUEBERRIES"] = "Blaubeeren",
                ["RASPBERRIES"] = "Himbeeren",
                ["MUSHROOMS"] = "Pilze",
                ["CLOUDBERRIES"] = "Moltebeeren",
                ["THISTLE"] = "Distel",
                ["SEEDS"] = "Samen",
                ["MAGECAPS"] = "Magierkappen",
                ["YPCONES"] = "Yggdrasil-Zapfen",
                ["JOTUNNPUFFS"] = "Jotunnflusen",
                ["VOLTUREEGG"] = "Geier-Ei",
                ["DRAGONEGG"] = "Drachenei",
                ["SMOKEPUFFS"] = "Rauchwolken",
                ["DVERGERTHINGS"] = "Dvergr-Überreste",
                ["DWARFSPAWNER"] = "Grauzwergen-Nest",
                ["DRAUGRSPAWNER"] = "Draugr-Portal",
                ["TOTEM"] = "Fuling-Totem",
                ["FIRE"] = "Lagerfeuer",
                ["TAR"] = "Teergrube",
                ["SKELETON"] = "Skelett",
                ["TREASURE"] = "Schatz",
                ["MISTLANDSSWORDS"] = "Schwert im Stein",
                ["MISTLANDSGIANTS"] = "Überreste der Riesen",
                ["MARKER"] = "Markierung"
            },
            ["es"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Cobre",
                ["IRON"] = "Hierro",
                ["SILVER"] = "Plata",
                ["OBSIDIAN"] = "Obsidiana",
                ["MUDPILE"] = "Montón de barro",
                ["METEORITE"] = "Meteorito",
                ["TROLLCAVE"] = "Cueva de trolls",
                ["CRYPT"] = "Cripta",
                ["ABOMINATION"] = "Abominación",
                ["FLAX"] = "Lino",
                ["BARLEY"] = "Cebada",
                ["BLUEBERRIES"] = "Arándanos",
                ["RASPBERRIES"] = "Frambuesas",
                ["MUSHROOMS"] = "Setas",
                ["CLOUDBERRIES"] = "Moras nubes",
                ["THISTLE"] = "Cardo",
                ["SEEDS"] = "Semillas",
                ["MAGECAPS"] = "Sombreros mágicos",
                ["YPCONES"] = "Conos de Yggdrasil",
                ["JOTUNNPUFFS"] = "Pelusas de Jotunn",
                ["VOLTUREEGG"] = "Huevo de buitre",
                ["DRAGONEGG"] = "Huevo de dragón",
                ["SMOKEPUFFS"] = "Nubes de humo",
                ["DVERGERTHINGS"] = "Restos de Dvergr",
                ["DWARFSPAWNER"] = "Nido de enano gris",
                ["DRAUGRSPAWNER"] = "Portal Draugr",
                ["TOTEM"] = "Tótem Fuling",
                ["FIRE"] = "Hoguera",
                ["TAR"] = "Pozo de alquitrán",
                ["SKELETON"] = "Esqueleto",
                ["TREASURE"] = "Tesoro",
                ["MISTLANDSSWORDS"] = "Espada en la piedra",
                ["MISTLANDSGIANTS"] = "Restos de gigantes",
                ["MARKER"] = "Marcador"
            },
            ["pl"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Miedź",
                ["IRON"] = "Żelazo",
                ["SILVER"] = "Srebro",
                ["OBSIDIAN"] = "Obsydian",
                ["MUDPILE"] = "Stos błota",
                ["METEORITE"] = "Meteoryt",
                ["TROLLCAVE"] = "Jaskinia trolla",
                ["CRYPT"] = "Krypta",
                ["ABOMINATION"] = "Potworność",
                ["FLAX"] = "Len",
                ["BARLEY"] = "Jęczmień",
                ["BLUEBERRIES"] = "Jagody",
                ["RASPBERRIES"] = "Maliny",
                ["MUSHROOMS"] = "Grzyby",
                ["CLOUDBERRIES"] = "Malina moroszka",
                ["THISTLE"] = "Oset",
                ["SEEDS"] = "Nasiona",
                ["MAGECAPS"] = "Kapelusze maga",
                ["YPCONES"] = "Szyszki Yggdrasila",
                ["JOTUNNPUFFS"] = "Puch Jotunna",
                ["VOLTUREEGG"] = "Jajo sępa",
                ["DRAGONEGG"] = "Jajo smoka",
                ["SMOKEPUFFS"] = "Kłęby dymu",
                ["DVERGERTHINGS"] = "Pozostałości Dvergrów",
                ["DWARFSPAWNER"] = "Gniazdo krasnoluda",
                ["DRAUGRSPAWNER"] = "Pojawiacz Draugrów",
                ["TOTEM"] = "Totem Fulingów",
                ["FIRE"] = "Ognisko",
                ["TAR"] = "Dół ze smołą",
                ["SKELETON"] = "Szkielet",
                ["TREASURE"] = "Skarb",
                ["MISTLANDSSWORDS"] = "Miecz w kamieniu",
                ["MISTLANDSGIANTS"] = "Szczątki olbrzyma",
                ["MARKER"] = "Znacznik",
            },
            ["zh"] = new Dictionary<string, string>
            {
                ["COPPER"] = "铜矿",
                ["IRON"] = "铁矿",
                ["SILVER"] = "银矿",
                ["OBSIDIAN"] = "黑曜石",
                ["MUDPILE"] = "泥堆",
                ["METEORITE"] = "陨石",
                ["TROLLCAVE"] = "巨魔洞穴",
                ["CRYPT"] = "地穴",
                ["ABOMINATION"] = "憎恶",
                ["FLAX"] = "亚麻",
                ["BARLEY"] = "大麦",
                ["BLUEBERRIES"] = "蓝莓",
                ["RASPBERRIES"] = "覆盆子",
                ["MUSHROOMS"] = "蘑菇",
                ["CLOUDBERRIES"] = "云莓",
                ["THISTLE"] = "蓟",
                ["SEEDS"] = "种子",
                ["MAGECAPS"] = "法师帽",
                ["YPCONES"] = "世界树松果",
                ["JOTUNNPUFFS"] = "尤顿绒毛",
                ["VOLTUREEGG"] = "秃鹰蛋",
                ["DRAGONEGG"] = "龙蛋",
                ["SMOKEPUFFS"] = "烟雾",
                ["DVERGERTHINGS"] = "矮人遗物",
                ["DWARFSPAWNER"] = "灰矮人巢穴",
                ["DRAUGRSPAWNER"] = "尸鬼生成器",
                ["TOTEM"] = "愚林图腾",
                ["FIRE"] = "营火",
                ["TAR"] = "焦油坑",
                ["SKELETON"] = "骷髅",
                ["TREASURE"] = "宝藏",
                ["MISTLANDSSWORDS"] = "石中剑",
                ["MISTLANDSGIANTS"] = "巨人遗骸",
                ["MARKER"] = "标记",
            },
            ["pt"] = new Dictionary<string, string>
            {
                ["COPPER"] = "Cobre",
                ["IRON"] = "Ferro",
                ["SILVER"] = "Prata",
                ["OBSIDIAN"] = "Obsidiana",
                ["MUDPILE"] = "Monturo de lama",
                ["METEORITE"] = "Meteorito",
                ["TROLLCAVE"] = "Caverna do troll",
                ["CRYPT"] = "Cripta",
                ["ABOMINATION"] = "Abominação",
                ["FLAX"] = "Linho",
                ["BARLEY"] = "Cevada",
                ["BLUEBERRIES"] = "Mirtilos",
                ["RASPBERRIES"] = "Framboesas",
                ["MUSHROOMS"] = "Cogumelos",
                ["CLOUDBERRIES"] = "Amoras nubladas",
                ["THISTLE"] = "Cardo",
                ["SEEDS"] = "Sementes",
                ["MAGECAPS"] = "Chapéus mágicos",
                ["YPCONES"] = "Pinhas de Yggdrasil",
                ["JOTUNNPUFFS"] = "Fofos de Jotunn",
                ["VOLTUREEGG"] = "Ovo de abutre",
                ["DRAGONEGG"] = "Ovo de dragão",
                ["SMOKEPUFFS"] = "Nuvens de fumaça",
                ["DVERGERTHINGS"] = "Restos de Dvergr",
                ["DWARFSPAWNER"] = "Ninho de anão cinzento",
                ["DRAUGRSPAWNER"] = "Portal Draugr",
                ["TOTEM"] = "Totem Fuling",
                ["FIRE"] = "Fogueira",
                ["TAR"] = "Poça de alcatrão",
                ["SKELETON"] = "Esqueleto",
                ["TREASURE"] = "Tesouro",
                ["MISTLANDSSWORDS"] = "Espada na pedra",
                ["MISTLANDSGIANTS"] = "Restos de gigantes",
                ["MARKER"] = "Marcador"
            }
        };

    }
}
