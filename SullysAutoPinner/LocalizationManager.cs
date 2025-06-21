// FULL VERSION WITH TRANSLATIONS INCLUDED DIRECTLY

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

        LoadTranslations(FallbackLang);
        if (_langCode != FallbackLang)
            LoadTranslations(_langCode);
    }

    private string _currentLanguage = "";

    public void Reload(string newLanguageCode)
    {
        if (_currentLanguage.Equals(newLanguageCode, StringComparison.OrdinalIgnoreCase))
            return;

        _currentLanguage = newLanguageCode;
        LoadTranslations(newLanguageCode);
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
        return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = LocalizationTranslations.EN,
            ["ru"] = LocalizationTranslations.RU,
            ["fr"] = LocalizationTranslations.FR,
            ["de"] = LocalizationTranslations.DE,
            ["es"] = LocalizationTranslations.ES,
            ["pl"] = LocalizationTranslations.PL,
            ["zh"] = LocalizationTranslations.ZH,
            ["pt"] = LocalizationTranslations.PT,
        };
    }
}

// Re-initialize your dictionaries here using proper syntax, e.g.:
public static class LocalizationTranslations
{
    public static readonly Dictionary<string, string> EN = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abomination",
        ["CRYPT"] = "Crypt",
        ["DRAUGRSPAWNER"] = "Draugr Spawner",
        ["DVERGERTHINGS"] = "Dvergr Remains",
        ["DWARFSPAWNER"] = "Greydwarf Nest",
        ["FIRE"] = "Campfire",
        ["FLAX"] = "Flax",
        ["IRON"] = "Iron",
        ["MUDPILE"] = "Mudpile",
        ["METEORITE"] = "Meteorite",
        ["MAGECAPS"] = "Magecaps",
        ["MARKER"] = "Marker",
        ["MUSHROOMS"] = "Mushrooms",
        ["OBSIDIAN"] = "Obsidian",
        ["SEEDS"] = "Seeds",
        ["SILVER"] = "Silver",
        ["SKELETON"] = "Skeleton",
        ["SMOKEPUFFS"] = "Smoke Puffs",
        ["TAR"] = "Tar Pit",
        ["THISTLE"] = "Thistle",
        ["TOTEM"] = "Fuling Totem",
        ["TROLLCAVE"] = "Troll Cave",
        ["TREASURE"] = "Buried Treasure",
        ["VOLTUREEGG"] = "Vulture Egg",
        ["YPCONES"] = "Yggdrasil Pine Cones",
        ["JOTUNNPUFFS"] = "Jotunn Puffs",
        ["RASPBERRIES"] = "Raspberries",
        ["BLUEBERRIES"] = "Blueberries",
        ["CLOUDBERRIES"] = "Cloudberries",
        ["BARLEY"] = "Barley",
        ["CLOTH"] = "Cloth",
        ["SHIPWRECKCHEST"] = "Shipwreck Chest",
        ["GIANTSWORD"] = "Giant Sword",
        ["GIANTRIBS"] = "Giant Ribs",
        ["GIANTSKULL"] = "Giant Skull",
        ["GIANTBRAIN"] = "Giant Brain",
        ["EXCAVATION"] = "Excavation Site",
        ["LANTERN"] = "Lantern",
        ["ASHLANDPOTGREEN"] = "Ashland Pot (Green)",
        ["ASHLANDPOTRED"] = "Ashland Pot (Red)",
        ["SEEKERBRUTE"] = "Seeker Brute",
        ["BONEMAWSERPENT"] = "Bonemaw Serpent",
        ["COPPER"] = "Copper",
        ["DRAGONEGG"] = "Dragon Egg"
    };
    public static readonly Dictionary<string, string> RU = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Мерзость",
        ["CRYPT"] = "Крипта",
        ["DRAUGRSPAWNER"] = "Портал драугров",
        ["DVERGERTHINGS"] = "Останки двергров",
        ["DWARFSPAWNER"] = "Гнездо серого карлика",
        ["FIRE"] = "Костер",
        ["FLAX"] = "Лён",
        ["IRON"] = "Железо",
        ["MUDPILE"] = "Грязевая куча",
        ["METEORITE"] = "Метеорит",
        ["MAGECAPS"] = "Волшебные шляпки",
        ["MARKER"] = "Маркер",
        ["MUSHROOMS"] = "Грибы",
        ["OBSIDIAN"] = "Обсидиан",
        ["SEEDS"] = "Семена",
        ["SILVER"] = "Серебро",
        ["SKELETON"] = "Скелет",
        ["SMOKEPUFFS"] = "Клубы дыма",
        ["TAR"] = "Смоляная яма",
        ["THISTLE"] = "Чертополох",
        ["TOTEM"] = "Тотем фулингов",
        ["TROLLCAVE"] = "Пещера тролля",
        ["TREASURE"] = "Сокровище",
        ["VOLTUREEGG"] = "Яйцо стервятника",
        ["YPCONES"] = "Шишки Иггдрасиля",
        ["JOTUNNPUFFS"] = "Пух Йотуна",
        ["RASPBERRIES"] = "Малина",
        ["BLUEBERRIES"] = "Черника",
        ["CLOUDBERRIES"] = "Морошка",
        ["BARLEY"] = "Ячмень",
        ["CLOTH"] = "Ткань",
        ["SHIPWRECKCHEST"] = "Сундук кораблекрушения",
        ["GIANTSWORD"] = "Меч великана",
        ["GIANTRIBS"] = "Рёбра великана",
        ["GIANTSKULL"] = "Череп великана",
        ["GIANTBRAIN"] = "Мозг великана",
        ["EXCAVATION"] = "Место раскопок",
        ["LANTERN"] = "Фонарь",
        ["ASHLANDPOTGREEN"] = "Пот Зелёный",
        ["ASHLANDPOTRED"] = "Пот Красный",
        ["SEEKERBRUTE"] = "Ищущий Враг",
        ["BONEMAWSERPENT"] = "Костеглот",
        ["COPPER"] = "Медь",
        ["DRAGONEGG"] = "Яйцо дракона"
    };
    public static readonly Dictionary<string, string> FR = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abomination",
        ["CRYPT"] = "Crypte",
        ["DRAUGRSPAWNER"] = "Portail Draugr",
        ["DVERGERTHINGS"] = "Restes de Dvergr",
        ["DWARFSPAWNER"] = "Nid de nain gris",
        ["FIRE"] = "Feu de camp",
        ["FLAX"] = "Lin",
        ["IRON"] = "Fer",
        ["MUDPILE"] = "Tas de boue",
        ["METEORITE"] = "Météorite",
        ["MAGECAPS"] = "Chapeaux de mage",
        ["MARKER"] = "Marqueur",
        ["MUSHROOMS"] = "Champignons",
        ["OBSIDIAN"] = "Obsidienne",
        ["SEEDS"] = "Graines",
        ["SILVER"] = "Argent",
        ["SKELETON"] = "Squelette",
        ["SMOKEPUFFS"] = "Nuages de fumée",
        ["TAR"] = "Fosse à goudron",
        ["THISTLE"] = "Chardon",
        ["TOTEM"] = "Totem de Fuling",
        ["TROLLCAVE"] = "Grotte du troll",
        ["TREASURE"] = "Trésor",
        ["VOLTUREEGG"] = "Œuf de vautour",
        ["YPCONES"] = "Cônes d’Yggdrasil",
        ["JOTUNNPUFFS"] = "Poussières de Jotunn",
        ["RASPBERRIES"] = "Framboises",
        ["BLUEBERRIES"] = "Myrtilles",
        ["CLOUDBERRIES"] = "Plaquebières",
        ["BARLEY"] = "Orge",
        ["CLOTH"] = "Tissu",
        ["SHIPWRECKCHEST"] = "Coffre d'épave",
        ["GIANTSWORD"] = "Épée de géant",
        ["GIANTRIBS"] = "Côtes de géant",
        ["GIANTSKULL"] = "Crâne de géant",
        ["GIANTBRAIN"] = "Cerveau de géant",
        ["EXCAVATION"] = "Site d’excavation",
        ["LANTERN"] = "Lanterne",
        ["ASHLANDPOTGREEN"] = "Pot des cendres (vert)",
        ["ASHLANDPOTRED"] = "Pot des cendres (rouge)",
        ["SEEKERBRUTE"] = "Brute Chercheuse",
        ["BONEMAWSERPENT"] = "Serpent Mâchoires d’Os",
        ["COPPER"] = "Cuivre",
        ["DRAGONEGG"] = "Œuf de dragon"
    };
    public static readonly Dictionary<string, string> DE = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abscheulichkeit",
        ["CRYPT"] = "Gruft",
        ["DRAUGRSPAWNER"] = "Draugr-Portal",
        ["DVERGERTHINGS"] = "Dvergr-Überreste",
        ["DWARFSPAWNER"] = "Grauzwergen-Nest",
        ["FIRE"] = "Lagerfeuer",
        ["FLAX"] = "Flachs",
        ["IRON"] = "Eisen",
        ["MUDPILE"] = "Schlammhaufen",
        ["METEORITE"] = "Meteorit",
        ["MAGECAPS"] = "Magierkappen",
        ["MARKER"] = "Markierung",
        ["MUSHROOMS"] = "Pilze",
        ["OBSIDIAN"] = "Obsidian",
        ["SEEDS"] = "Samen",
        ["SILVER"] = "Silber",
        ["SKELETON"] = "Skelett",
        ["SMOKEPUFFS"] = "Rauchwolken",
        ["TAR"] = "Teergrube",
        ["THISTLE"] = "Distel",
        ["TOTEM"] = "Fuling-Totem",
        ["TROLLCAVE"] = "Trollhöhle",
        ["TREASURE"] = "Schatz",
        ["VOLTUREEGG"] = "Geier-Ei",
        ["YPCONES"] = "Yggdrasil-Zapfen",
        ["JOTUNNPUFFS"] = "Jotunnflusen",
        ["RASPBERRIES"] = "Himbeeren",
        ["BLUEBERRIES"] = "Blaubeeren",
        ["CLOUDBERRIES"] = "Moltebeeren",
        ["BARLEY"] = "Gerste",
        ["CLOTH"] = "Stoff",
        ["SHIPWRECKCHEST"] = "Schiffswracktruhe",
        ["GIANTSWORD"] = "Schwert des Riesen",
        ["GIANTRIBS"] = "Rippen des Riesen",
        ["GIANTSKULL"] = "Schädel des Riesen",
        ["GIANTBRAIN"] = "Gehirn des Riesen",
        ["EXCAVATION"] = "Ausgrabungsstätte",
        ["LANTERN"] = "Laterne",
        ["ASHLANDPOTGREEN"] = "Aschenkrug (Grün)",
        ["ASHLANDPOTRED"] = "Aschenkrug (Rot)",
        ["SEEKERBRUTE"] = "Sucher-Brutalo",
        ["BONEMAWSERPENT"] = "Knochenrachen-Schlange",
        ["COPPER"] = "Kupfer",
        ["DRAGONEGG"] = "Drachenei"
    };
    public static readonly Dictionary<string, string> ES = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abominación",
        ["CRYPT"] = "Cripta",
        ["DRAUGRSPAWNER"] = "Generador de Draugr",
        ["DVERGERTHINGS"] = "Restos de Dvergr",
        ["DWARFSPAWNER"] = "Nido de enano gris",
        ["FIRE"] = "Hoguera",
        ["FLAX"] = "Lino",
        ["IRON"] = "Hierro",
        ["MUDPILE"] = "Montón de barro",
        ["METEORITE"] = "Meteorito",
        ["MAGECAPS"] = "Sombreros de mago",
        ["MARKER"] = "Marcador",
        ["MUSHROOMS"] = "Hongos",
        ["OBSIDIAN"] = "Obsidiana",
        ["SEEDS"] = "Semillas",
        ["SILVER"] = "Plata",
        ["SKELETON"] = "Esqueleto",
        ["SMOKEPUFFS"] = "Nubes de humo",
        ["TAR"] = "Pozo de alquitrán",
        ["THISTLE"] = "Cardo",
        ["TOTEM"] = "Tótem de Fuling",
        ["TROLLCAVE"] = "Cueva del troll",
        ["TREASURE"] = "Tesoro enterrado",
        ["VOLTUREEGG"] = "Huevo de buitre",
        ["YPCONES"] = "Conos de Yggdrasil",
        ["JOTUNNPUFFS"] = "Pelusas de Jotunn",
        ["RASPBERRIES"] = "Frambuesas",
        ["BLUEBERRIES"] = "Arándanos",
        ["CLOUDBERRIES"] = "Moras árticas",
        ["BARLEY"] = "Cebada",
        ["CLOTH"] = "Tela",
        ["SHIPWRECKCHEST"] = "Cofre del naufragio",
        ["GIANTSWORD"] = "Espada gigante",
        ["GIANTRIBS"] = "Costillas de gigante",
        ["GIANTSKULL"] = "Cráneo de gigante",
        ["GIANTBRAIN"] = "Cerebro de gigante",
        ["EXCAVATION"] = "Sitio de excavación",
        ["LANTERN"] = "Linterna",
        ["ASHLANDPOTGREEN"] = "Tintero de ceniza (verde)",
        ["ASHLANDPOTRED"] = "Tintero de ceniza (rojo)",
        ["SEEKERBRUTE"] = "Bruto Buscador",
        ["BONEMAWSERPENT"] = "Serpiente Mandíbulas de Hueso",
        ["COPPER"] = "Cobre",
        ["DRAGONEGG"] = "Huevo de dragón"
    };
    public static readonly Dictionary<string, string> PL = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abominacja",
        ["CRYPT"] = "Krypta",
        ["DRAUGRSPAWNER"] = "Pojawiacz Draugrów",
        ["DVERGERTHINGS"] = "Szczątki Dvergrów",
        ["DWARFSPAWNER"] = "Gniazdo szarego krasnoluda",
        ["FIRE"] = "Ognisko",
        ["FLAX"] = "Len",
        ["IRON"] = "Żelazo",
        ["MUDPILE"] = "Stos błota",
        ["METEORITE"] = "Meteoryt",
        ["MAGECAPS"] = "Kapelusze maga",
        ["MARKER"] = "Znacznik",
        ["MUSHROOMS"] = "Grzyby",
        ["OBSIDIAN"] = "Obsydian",
        ["SEEDS"] = "Nasiona",
        ["SILVER"] = "Srebro",
        ["SKELETON"] = "Szkielet",
        ["SMOKEPUFFS"] = "Kłęby dymu",
        ["TAR"] = "Dół smoły",
        ["THISTLE"] = "Oset",
        ["TOTEM"] = "Totem Fulingów",
        ["TROLLCAVE"] = "Jaskinia trolla",
        ["TREASURE"] = "Zakopany skarb",
        ["VOLTUREEGG"] = "Jajo sępa",
        ["YPCONES"] = "Szyszki Yggdrasila",
        ["JOTUNNPUFFS"] = "Puch Jotunna",
        ["RASPBERRIES"] = "Maliny",
        ["BLUEBERRIES"] = "Jagody",
        ["CLOUDBERRIES"] = "Maliny moroszki",
        ["BARLEY"] = "Jęczmień",
        ["CLOTH"] = "Tkanina",
        ["SHIPWRECKCHEST"] = "Skrzynia z wraku statku",
        ["GIANTSWORD"] = "Miecz olbrzyma",
        ["GIANTRIBS"] = "Żebra olbrzyma",
        ["GIANTSKULL"] = "Czaszka olbrzyma",
        ["GIANTBRAIN"] = "Mózg olbrzyma",
        ["EXCAVATION"] = "Miejsce wykopalisk",
        ["LANTERN"] = "Latarnia",
        ["ASHLANDPOTGREEN"] = "Popielniczka (zielona)",
        ["ASHLANDPOTRED"] = "Popielniczka (czerwona)",
        ["SEEKERBRUTE"] = "Brutalny Poszukiwacz",
        ["BONEMAWSERPENT"] = "Wąż Kościany",
        ["COPPER"] = "Miedź",
        ["DRAGONEGG"] = "Jajo smoka"
    };
    public static readonly Dictionary<string, string> ZH = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "憎恶",
        ["CRYPT"] = "地穴",
        ["DRAUGRSPAWNER"] = "尸鬼刷怪点",
        ["DVERGERTHINGS"] = "矮人遗骸",
        ["DWARFSPAWNER"] = "灰矮人巢穴",
        ["FIRE"] = "篝火",
        ["FLAX"] = "亚麻",
        ["IRON"] = "铁",
        ["MUDPILE"] = "泥堆",
        ["METEORITE"] = "陨石",
        ["MAGECAPS"] = "法师帽",
        ["MARKER"] = "标记",
        ["MUSHROOMS"] = "蘑菇",
        ["OBSIDIAN"] = "黑曜石",
        ["SEEDS"] = "种子",
        ["SILVER"] = "银",
        ["SKELETON"] = "骷髅",
        ["SMOKEPUFFS"] = "烟雾",
        ["TAR"] = "焦油坑",
        ["THISTLE"] = "蓟",
        ["TOTEM"] = "伏林图腾",
        ["TROLLCAVE"] = "巨魔洞穴",
        ["TREASURE"] = "埋藏宝藏",
        ["VOLTUREEGG"] = "秃鹫蛋",
        ["YPCONES"] = "世界树松果",
        ["JOTUNNPUFFS"] = "尤顿之绒",
        ["RASPBERRIES"] = "覆盆子",
        ["BLUEBERRIES"] = "蓝莓",
        ["CLOUDBERRIES"] = "云莓",
        ["BARLEY"] = "大麦",
        ["CLOTH"] = "布料",
        ["SHIPWRECKCHEST"] = "沉船宝箱",
        ["GIANTSWORD"] = "巨人之剑",
        ["GIANTRIBS"] = "巨人肋骨",
        ["GIANTSKULL"] = "巨人头骨",
        ["GIANTBRAIN"] = "巨人脑",
        ["EXCAVATION"] = "挖掘现场",
        ["LANTERN"] = "灯笼",
        ["ASHLANDPOTGREEN"] = "灰烬罐（绿色）",
        ["ASHLANDPOTRED"] = "灰烬罐（红色）",
        ["SEEKERBRUTE"] = "搜寻者蛮兵",
        ["BONEMAWSERPENT"] = "骨颚巨蛇",
        ["COPPER"] = "铜",
        ["DRAGONEGG"] = "龙蛋"
    };
    public static readonly Dictionary<string, string> PT = new Dictionary<string, string>
    {
        ["ABOMINATION"] = "Abominação",
        ["CRYPT"] = "Cripta",
        ["DRAUGRSPAWNER"] = "Gerador de Draugr",
        ["DVERGERTHINGS"] = "Restos de Dvergr",
        ["DWARFSPAWNER"] = "Ninho de Anão Cinzento",
        ["FIRE"] = "Fogueira",
        ["FLAX"] = "Linho",
        ["IRON"] = "Ferro",
        ["MUDPILE"] = "Monturo de lama",
        ["METEORITE"] = "Meteorito",
        ["MAGECAPS"] = "Chapéus de mago",
        ["MARKER"] = "Marcador",
        ["MUSHROOMS"] = "Cogumelos",
        ["OBSIDIAN"] = "Obsidiana",
        ["SEEDS"] = "Sementes",
        ["SILVER"] = "Prata",
        ["SKELETON"] = "Esqueleto",
        ["SMOKEPUFFS"] = "Fumaça",
        ["TAR"] = "Poça de piche",
        ["THISTLE"] = "Cardo",
        ["TOTEM"] = "Totem de Fuling",
        ["TROLLCAVE"] = "Caverna do troll",
        ["TREASURE"] = "Tesouro enterrado",
        ["VOLTUREEGG"] = "Ovo de abutre",
        ["YPCONES"] = "Pinhas de Yggdrasil",
        ["JOTUNNPUFFS"] = "Pufs de Jotunn",
        ["RASPBERRIES"] = "Framboesas",
        ["BLUEBERRIES"] = "Mirtilos",
        ["CLOUDBERRIES"] = "Amoras árticas",
        ["BARLEY"] = "Cevada",
        ["CLOTH"] = "Tecido",
        ["SHIPWRECKCHEST"] = "Baú de naufrágio",
        ["GIANTSWORD"] = "Espada gigante",
        ["GIANTRIBS"] = "Costelas gigantes",
        ["GIANTSKULL"] = "Crânio gigante",
        ["GIANTBRAIN"] = "Cérebro gigante",
        ["EXCAVATION"] = "Local de escavação",
        ["LANTERN"] = "Lanterna",
        ["ASHLANDPOTGREEN"] = "Pote das Cinzas (Verde)",
        ["ASHLANDPOTRED"] = "Pote das Cinzas (Vermelho)",
        ["SEEKERBRUTE"] = "Bruto Buscador",
        ["BONEMAWSERPENT"] = "Serpente Mandíbula Óssea",
        ["COPPER"] = "Cobre",
        ["DRAGONEGG"] = "Ovo de dragão"
    };
}
