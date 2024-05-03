using System.Text.Encodings.Web;
using System.Text.Json;

using Colossal;
using Better_Bulldozer;
using Better_Bulldozer.Settings;
using Better_Bulldozer.Localization;

/*
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
*/

string[] languages = { "de-DE", "es-ES", "fr-FR", "it-IT", "ko-KR", "pl-PL", "pt-BR", "ru-RU", "zh-HANS" };

foreach (string lang in languages)
{
    Dictionary<string, string>? e;
    var _ = new Dictionary<string, string>();
    IDictionary<string, string?> f = Localization.LoadTranslations(lang);
    e = f as Dictionary<string, string>;
    var str = JsonSerializer.Serialize(e, new JsonSerializerOptions()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\BetterBulldozer\\BetterBulldozer\\lang\\{lang}.json", str);
}