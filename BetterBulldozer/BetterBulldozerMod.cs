// <copyright file="BetterBulldozerMod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

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
    using Colossal;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using HarmonyLib;
    using Newtonsoft.Json;

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class BetterBulldozerMod : IMod
    {
        /// <summary>
        /// Fake keybind action for apply.
        /// </summary>
        public const string RSEApplyMimicAction = "SEBTApplyMimic";

        /// <summary>
        /// Fake keybind action for apply.
        /// </summary>
        public const string VCAApplyMimicAction = "VCAApplyMimic";

        /// <summary>
        /// A static ID for use with bindings.
        /// </summary>
        public static readonly string Id = "BetterBulldozer";

        /// <summary>
        /// Gets the version of the mod.
        /// </summary>
        internal string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        /// <summary>
        /// Gets the install folder for the mod.
        /// </summary>
        private static string m_modInstallFolder;

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
        /// Gets the Install Folder for the mod as a string.
        /// </summary>
        public static string ModInstallFolder
        {
            get
            {
                if (m_modInstallFolder is null)
                {
                    var thisFullName = Instance.GetType().Assembly.FullName;
                    ExecutableAsset thisInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName)) ?? throw new Exception("This mod info was not found!!!!");
                    m_modInstallFolder = Path.GetDirectoryName(thisInfo.GetMeta().path);
                }

                return m_modInstallFolder;
            }
        }

        /// <summary>
        /// Gets ILog for mod.
        /// </summary>
        internal ILog Logger { get; private set; }

        /// <summary>
        ///  Gets or sets the Mod Settings.
        /// </summary>
        internal BetterBulldozerModSettings Settings { get; set; }

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

            Logger.Info("ModInstallFolder = " + ModInstallFolder);
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
#if DEBUG
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Exporting localization");
            var localeDict = new LocaleEN(Settings).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
            var str = JsonConvert.SerializeObject(localeDict, Formatting.Indented);
            try
            {
                File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\{Id}\\{Id}\\UI\\src\\mods\\lang\\en-US.json", str);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
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

    }
}
