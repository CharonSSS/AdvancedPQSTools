using HarmonyLib;
using UnityEngine;

namespace AdvancedPQSTools.Harmony
{
    [HarmonyPatch(typeof(PQS))]
    internal class Stock_PQS_Patches
    {
        [HarmonyPatch("StartSphere")] // cant seem to patch Reset(), it seems to be different from other unity methods like Start()
        [HarmonyPrefix]
        internal static void Prefix_StartSphere(PQS __instance, bool force)
        {
            if (__instance.name != "Kerbin") // TODO: make this adjustable
                return;

            // TODO: add minLevel
            __instance.maxLevel = 16; // TODO: make this adjustable
            __instance.minDetailDistance = 16f; // TODO: make this adjustable

            if (PQSCache.PresetList != null)
            {
                PQSCache.PQSSpherePreset preset = PQSCache.PresetList.GetPreset(__instance.gameObject.name);
                if (preset != null)
                {
                    preset.minDistance = __instance.minDetailDistance;
                    // TODO: add minSubdivision
                    preset.maxSubdivision = __instance.maxLevel;
                    // StartSphere uses the presets if the presets aren't null, so we can preset them here
                }
            }

            Debug.Log($"[Stock PQS Patches] Prefix_StartSphere applied for {__instance.name}: maxLevel={__instance.maxLevel}, minDetailDistance={__instance.minDetailDistance}");
        }
    }
}
