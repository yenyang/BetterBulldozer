// <copyright file="BetterBulldozerMod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer
{
    using System;
    using System.IO;
    using Better_Bulldozer.Systems;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using HarmonyLib;

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class BetterBulldozerMod : IMod
    {
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

        /// <inheritdoc/>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Logger = LogManager.GetLogger("Mods_Yenyang_Better_Bulldozer").SetShowsErrorsInUI(false);
            Logger.Info(nameof(OnLoad));
#if DEBUG
            Logger.effectivenessLevel = Level.Debug;
#elif VERBOSE
            Log.effectivenessLevel = Level.Verbose;
#else
            Log.effectivenessLevel = Level.Info;
#endif

            Logger.Info("ModInstallFolder = " + ModInstallFolder);
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Injecting Harmony Patches.");
            m_Harmony = new Harmony("Mods_Yenyang_Better_Bulldozer");
            m_Harmony.PatchAll();
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Loading localization");
            Localization.Localization.LoadTranslations(Logger);
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Injecting systems.");
            updateSystem.UpdateAt<BetterBulldozerUISystem>(SystemUpdatePhase.UIUpdate);
            Logger.Info($"{nameof(BetterBulldozerMod)}.{nameof(OnLoad)} Complete.");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Logger.Info("Disposing..");
            m_Harmony.UnpatchAll();
        }

    }
}
