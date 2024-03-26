using System.Text.Encodings.Web;
using System.Text.Json;

using Colossal;
using Better_Bulldozer;
using Better_Bulldozer.Settings;

var setting = new BetterBulldozerModSettings(new BetterBulldozerMod());

var locale = new LocaleEN(setting);
var e = new Dictionary<string, string>(
    locale.ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()));
var str = JsonSerializer.Serialize(e, new JsonSerializerOptions()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});

File.WriteAllText("C:\\Users\\TJ\\source\\repos\\BetterBulldozer\\BetterBulldozer\\UI\\src\\mods\\lang\\en-US.json", str);


/*
var file = "C:\\Users\\TJ\\source\\repos\\Tree_Controller\\Tree_Controller\\l10n\\attempt 2\\CSL2 Mod_ Tree Controller (translations) (1)\\l10n.csv";
if (File.Exists(file))
{
    var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
    Console.Write("file exists");
    string[] languages = { "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR", "pl-PL", "pt-BR", "ru-RU", "zh-HANS", "zh-HANT" };
    foreach (string lang in languages) {
        var valueColumn = Array.IndexOf(fileLines.First(), lang);
        if (valueColumn > 0)
        {
            var e = new Dictionary<string, string>();
            IDictionary<string, string?> f = fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(valueColumn));
            e = f as Dictionary<string, string>;
            var str = JsonSerializer.Serialize(e, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\Tree_Controller\\Tree_Controller\\lang\\{lang}.json", str);
            Console.Write(lang.ToString());
        }
    }
}*/
