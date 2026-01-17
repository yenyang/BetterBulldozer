// <copyright file="BetterBulldozerMod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// # define EXPORT_EN_US
namespace Better_Bulldozer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Better_Bulldozer.Settings;
    using Better_Bulldozer.Systems;
    using Better_Bulldozer.Tools;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using HarmonyLib;
#if DEBUG && EXPORT_EN_US
    using Newtonsoft.Json;
    using Colossal;
    using System.Runtime.CompilerServices;
#endif

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class BetterBulldozerMod : IMod
    {
        /// <summary>
        /// A static ID for use with bindings.
        /// </summary>
        public static readonly string Id = "BetterBulldozer";
        private Harmony m_Harmony;

        /// <summary>
        /// Gets the static reference to the mod instance.
        /// </summary>
        public static BetterBulldozerMod Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the version of the mod.
        /// </summary>
#if !STABLE
        internal string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
#else
        internal string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#endif

        /// <summary>
        /// Gets ILog for mod.
        /// </summary>
        internal ILog Logger { get; private set; }

        /// <summary>
        ///  Gets or sets the Mod Settings.
        /// </summary>
        internal BetterBulldozerModSettings Settings { get; set; }

#if EXPORT_EN_US
        private static string GetThisFilePath([CallerFilePath] string path = null)
        {
            return path;
        }
#endif

        /// <inheritdoc/>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
#if DEBUG || VERBOSE
            Logger = LogManager.GetLogger("Mods_Yenyang_Better_Bulldozer").SetShowsErrorsInUI(true);
#else
            Logger = LogManager.GetLogger("Mods_Yenyang_Better_Bulldozer").SetShowsErrorsInUI(false);
#endif
            Logger.Info(nameof(OnLoad));
#if VERBOSE
            Logger.effectivenessLevel = Level.Verbose;
#elif DEBUG
            Logger.effectivenessLevel = Level.Debug;
#else
            Logger.effectivenessLevel = Level.Info;
#endif

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Logger.Info($"Current mod asset at {asset.path}");
            }

            Settings = new (this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(BetterBulldozerMod), Settings, new BetterBulldozerModSettings(this));
            Logger.Info($"[{nameof(BetterBulldozerMod)}] {nameof(OnLoad)} finished loading settings.");
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Injecting Harmony Patches.");
            m_Harmony = new Harmony("Mods_Yenyang_Better_Bulldozer");
            m_Harmony.PatchAll();
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Loading en-US");
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            Logger.Info($"[{nameof(BetterBulldozerMod)}] {nameof(OnLoad)} Loading localization for other languages.");
            LoadNonEnglishLocalizations();
#if DEBUG && EXPORT_EN_US
            GenerateLanguageFile();
#endif
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Injecting systems.");
            updateSystem.UpdateAt<BetterBulldozerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<SubElementBulldozerTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<SubelementBulldozerWarningTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<HandleDeleteInXFramesSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<SafelyRemoveSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<AutomaticallyRemoveFencesAndHedges>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<AutomaticallyRemoveBrandingObjects>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<RemoveRegeneratedSubelementPrefabsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateBefore<AutomaticallyRemoveManicuredGrassSurfaceSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAt<CleanUpOwnerRecordsSystem>(SystemUpdatePhase.Deserialize);
            updateSystem.UpdateAt<RestoreFencesAndHedgesSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<RestoreBrandingObjects>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<RemoveVehiclesCimsAndAnimalsTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<RemoveExistingOwnedGrassSurfaces>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<HandleUpdateNextFrameSystem>(SystemUpdatePhase.Modification5);
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Complete.");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Logger.Info("Disposing..");
            m_Harmony.UnpatchAll();
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }

        private void LoadNonEnglishLocalizations()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            try
            {
                Logger.Debug($"Reading localizations");

                foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    string resourceName = $"{nameof(Better_Bulldozer)}.l10n.{localeID}.json";
                    if (resourceNames.Contains(resourceName))
                    {
                        Logger.Debug($"Found localization file {resourceName}");
                        try
                        {
                            Logger.Debug($"Reading embedded translation file {resourceName}");

                            // Read embedded file.
                            using StreamReader reader = new (thisAssembly.GetManifestResourceStream(resourceName));
                            {
                                string entireFile = reader.ReadToEnd();
                                Colossal.Json.Variant varient = Colossal.Json.JSON.Load(entireFile);
                                Dictionary<string, string> translations = varient.Make<Dictionary<string, string>>();
                                GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(translations));
                            }
                        }
                        catch (Exception e)
                        {
                            // Don't let a single failure stop us.
                            Logger.Error(e, $"Exception reading localization from embedded file {resourceName}");
                        }
                    }
                    else
                    {
                        Logger.Debug($"Did not find localization file {resourceName}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception reading embedded settings localization files");
            }
        }

#if EXPORT_EN_US
        private void GenerateLanguageFile()
        {
            Logger.Info($"[{Id}] Exporting localization");
            var localeDict = new LocaleEN(Settings).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
            var str = JsonConvert.SerializeObject(localeDict, Formatting.Indented);
            try
            {
                var path = GetThisFilePath();
                var directory = Path.GetDirectoryName(path);

                var exportPath = $@"{directory}\UI\src\mods\lang\en-US.json";
                File.WriteAllText(exportPath, str);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
#endif
    }
}
