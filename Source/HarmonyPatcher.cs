using UnityEngine;

namespace AdvancedPQSTools.Harmony
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HarmonyPatcher : MonoBehaviour
    {
        internal void Start()
        {
            var harmony = new HarmonyLib.Harmony("AdvancedPQSTools.HarmonyPatcher");
            harmony.PatchAll();
        }
    }
}
