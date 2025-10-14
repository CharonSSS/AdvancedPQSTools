using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedPQSTools.Harmony
{
    [HarmonyPatch(typeof(PQS))]
    internal class StockPQSPatches
    {
        internal static string Body = "";
        internal static int? minLevel = null;
        internal static int? maxLevel = null;
        internal static double? minDetailDistance = null;

        [HarmonyPatch("StartSphere")] // cant seem to patch Reset(), it seems to be different from other unity methods like Start()
        [HarmonyPrefix]
        internal static void Prefix_StartSphere(PQS __instance, bool force)
        {
            if (Body == "" || __instance.name != Body)
                return;

            if (minLevel.HasValue)
                __instance.minLevel = minLevel.Value;
            if (maxLevel.HasValue)
                __instance.maxLevel = maxLevel.Value;
            if (minDetailDistance.HasValue)
                __instance.minDetailDistance = minDetailDistance.Value;

            if (PQSCache.PresetList != null)
            {
                PQSCache.PQSSpherePreset preset = PQSCache.PresetList.GetPreset(__instance.gameObject.name);
                if (preset != null)
                {
                    preset.minDistance = __instance.minDetailDistance;
                    preset.minSubdivision = __instance.minLevel;
                    preset.maxSubdivision = __instance.maxLevel;
                    // StartSphere uses the presets if the presets aren't null, so we can preset them here
                }
            }

            Debug.Log($"[StockPQSPatches] Prefix_StartSphere applied for {__instance.name}: maxLevel={__instance.maxLevel}, minDetailDistance={__instance.minDetailDistance}");
        }

        internal static void LoadSettings()
        {
            ConfigNode settings = GameDatabase.Instance.GetConfigNodes("ADVANCEDPQSTOOLS").FirstOrDefault();
            if (settings != null)
            {
                settings.TryGetValue("Body", ref Body);
                if (settings.HasValue("minLevel") && int.TryParse(settings.GetValue("minLevel"), out int parsedMin))
                    minLevel = parsedMin;

                if (settings.HasValue("maxLevel") && int.TryParse(settings.GetValue("maxLevel"), out int parsedMax))
                    maxLevel = parsedMax;

                if (settings.HasValue("minDetailDistance") && double.TryParse(settings.GetValue("minDetailDistance"), out double parsedDistance))
                    minDetailDistance = parsedDistance;
            }
        }
    }
}
