using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Erenshor_LowLevelXP
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class LowLevelXP : BaseUnityPlugin
    {
        internal const string ModName = "LowLevelXP";
        internal const string ModVersion = "1.0.1";
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
                var matcher = new CodeMatcher(instructions);

                Label correctLeaveTarget = new Label();
                
                while (matcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SimPlayer), "MyStats")),
                    new CodeMatch(OpCodes.Dup),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Stats), "CurrentExperience")),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Character), "xp")),
                    new CodeMatch(OpCodes.Add),
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Stats), "CurrentExperience")),
                    new CodeMatch(OpCodes.Ldarg_0)).IsValid)
                {
                    if (matcher.MatchStartForward(
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Character), "MyStats")),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Stats), "Charmed")),
                        new CodeMatch(OpCodes.Brfalse)).IsValid)
                    {
                        correctLeaveTarget = matcher.Instruction.labels.FirstOrDefault();
                    }
                }

                matcher.Start();

                if (matcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData), "PlayerStats")),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Stats), "Level")),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Character), "MyStats")),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Stats), "Level")),
                    new CodeMatch(OpCodes.Sub),
                    new CodeMatch(OpCodes.Ldc_I4_4),
                    new CodeMatch(OpCodes.Bgt)).IsValid)
                {
                    var bgtInstruction = matcher.Instruction;
                    var targetInstruction = matcher.Advance(1).Instruction;

                    var newLabel = new Label();
                    targetInstruction.labels.Add(newLabel);
                    bgtInstruction.operand = newLabel;
                }

                matcher.Start();

                if (matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Leave),
                    new CodeMatch(OpCodes.Ldloca_S),
                    new CodeMatch(OpCodes.Constrained),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Endfinally),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Character), "alternateAttacker"))
                    ).IsValid)
                {
                    matcher.SetOperandAndAdvance(correctLeaveTarget);
                    matcher.Insert(new CodeInstruction(OpCodes.Nop));
                }

                return matcher.InstructionEnumeration();
            }
        }
    }
}
