using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ILDumpUtility;

namespace Erenshor_LowLevelXP
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class LowLevelXP : BaseUnityPlugin
    {
        internal const string ModName = "LowLevelXP";
        internal const string ModVersion = "1.0.0";
        internal const string ModDescription = "Low Level XP";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;

        private readonly Harmony harmony = new Harmony(ModGUID);

        public void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {ModName} is loaded!");
        }

        private void OnDestroy()
        {
            harmony.UnpatchAll(ModGUID);
            Logger.LogInfo($"Plugin {ModName} is unloaded!");
        }

        [HarmonyPatch(typeof(Character))]
        [HarmonyPatch("DoDeath")]
        public static class AlwaysGainXPPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].opcode == OpCodes.Sub &&
                        codes[i + 1].opcode == OpCodes.Ldc_I4_4 &&
                        codes[i + 2].opcode == OpCodes.Bgt)
                    {
                        var targetInstruction = codes[i + 3];

                        if (targetInstruction.labels.Count == 0)
                        {
                            var xpLabel = new Label();
                            targetInstruction.labels.Add(xpLabel);
                            codes[i + 2].operand = xpLabel;
                        }
                        else
                        {
                            codes[i + 2].operand = targetInstruction.labels[0];
                        }

                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
    }
}
