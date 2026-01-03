using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedPQSTools.Harmony
{
    [HarmonyPatch(typeof(PQS))]
    internal class StockPQSPatches
    {
        internal class PQSSettings
        {
            internal string Body = "";
            internal int? minLevel = null;
            internal int? maxLevel = null;
            internal double? minDetailDistance = null;
        }

        internal static List<PQSSettings> Settings = new List<PQSSettings>();
        internal static bool settingsLoaded = false;
        internal static bool settingsPresent = false;

        [HarmonyPatch("StartSphere")] // cant seem to patch Reset(), it seems to be different from other unity methods like Start()
        [HarmonyPrefix]
        internal static void Prefix_StartSphere(PQS __instance, bool force)
        {
            LoadSettings(); // we cant do this at KSPAddon.Startup.Instantly bc GameDatabase doesnt seem to be loaded yet

            if (!settingsPresent)
                return;

            PQSSettings match = Settings.FirstOrDefault(m => m.Body == __instance.name);
            if (match == null || match.Body == "")
                return;

            if (match.minLevel.HasValue)
                __instance.minLevel = match.minLevel.Value;

            if (match.maxLevel.HasValue)
                __instance.maxLevel = match.maxLevel.Value;

            if (match.minDetailDistance.HasValue)
                __instance.minDetailDistance = match.minDetailDistance.Value;

            if (PQSCache.PresetList != null)
            {
                PQSCache.PQSSpherePreset preset = PQSCache.PresetList.GetPreset(match.Body);
                if (preset != null)
                {
                    preset.minDistance = __instance.minDetailDistance;
                    preset.minSubdivision = __instance.minLevel;
                    preset.maxSubdivision = __instance.maxLevel;
                    // StartSphere uses the presets if the presets aren't null, so we can preset them here
                }
            }
            Debug.Log($"[StockPQSPatches] Prefix_StartSphere applied for {match.Body}: minLevel: {__instance.minLevel}, maxLevel: {__instance.maxLevel}, minDetailDistance: {__instance.minDetailDistance}");
        }

        internal static void LoadSettings()
        {
            if (settingsLoaded)
                return;

            settingsLoaded = true;

            Settings.Clear();

            ConfigNode root = GameDatabase.Instance.GetConfigNodes("ADVANCEDPQSTOOLS").FirstOrDefault();
            if (root == null)
            {
                //Debug.LogWarning("[StockPQSPatches] No ADVANCEDPQSTOOLS config found.");
                settingsPresent = false; // this should already be set anyway
                return;
            }
            else             
            {
                settingsPresent = true;
            }

            foreach (ConfigNode node in root.GetNodes("Body"))
            {
                PQSSettings setting = new PQSSettings();

                node.TryGetValue("name", ref setting.Body);

                if (node.HasValue("minLevel") && int.TryParse(node.GetValue("minLevel"), out int parsedMin))
                    setting.minLevel = parsedMin;

                if (node.HasValue("maxLevel") && int.TryParse(node.GetValue("maxLevel"), out int parsedMax))
                    setting.maxLevel = parsedMax;

                if (node.HasValue("minDetailDistance") && double.TryParse(node.GetValue("minDetailDistance"), out double parsedDistance))
                    setting.minDetailDistance = parsedDistance;

                if (!string.IsNullOrEmpty(setting.Body))
                {
                    //Debug.Log($"[StockPQSPatches] Loaded config for body: {setting.Body}");
                    Settings.Add(setting);
                }
                else
                {
                    //Debug.LogWarning($"[StockPQSPatches] Config empty");
                }
            }

            if (Settings.Count == 0)
                Debug.LogWarning("[StockPQSPatches] Config node found, but no bodies found.");
        }
    }
}
