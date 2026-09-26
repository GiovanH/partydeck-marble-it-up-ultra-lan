using HarmonyLib;
using Rewired.Integration.UnityUI;

namespace MIUULan.Patches;

// Shouldn't need this with PartyDeck

// static class UnfocusedInput
// {
//     [HarmonyPatch(typeof(RewiredStandaloneInputModule), "OnApplicationFocus")]
//     static class RewiredStandaloneInputModulePatch
//     {
//         static void Prefix(ref bool hasFocus)
//         {
//             if (Lan.Enabled)
//                 hasFocus = true;
//         }
//     }
// }
