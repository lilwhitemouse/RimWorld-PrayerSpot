using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace LWM.PrayerSpot
{
    [HarmonyPatch]
    public static class Patch_JobDriver_RelaxAlone_RelaxToil
    {
        public static MethodBase TargetMethod()
        {
            // Decompiler showed the hidden inner class is "<MakeNewToils>d__6"... okay, sure.
            var hiddenClass = AccessTools.Inner(typeof(JobDriver_RelaxAlone), "<MakeNewToils>d__8");
            if (hiddenClass == null)
            {
                Log.Error("Couldn't find d__8 - check decompiler to find proper inner class");
                return null;
            }

            MethodBase iteratorMethod = AccessTools.Method(hiddenClass, "MoveNext");
            if (iteratorMethod == null)
            {
                Log.Error("Couldn't find MoveNext");
            }

            return iteratorMethod;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var Toil_AddPreTickAction = AccessTools.Method(typeof(Toil), "AddPreTickAction");
            /*if (Toil_AddPreTickAction == null) {
                Log.Error("Could not find AddPreTickAction");
                foreach (var c in code) {
                    yield return c;
                }
                yield break;
            }*/
            int locationOfLastPreTickAction;
            for (locationOfLastPreTickAction = code.Count - 1;
                locationOfLastPreTickAction >= 0;
                locationOfLastPreTickAction--)
            {
                if (code[locationOfLastPreTickAction].opcode == OpCodes.Callvirt &&
                    (MethodInfo) code[locationOfLastPreTickAction].operand == Toil_AddPreTickAction)
                {
                    break;
                }
            }

            if (locationOfLastPreTickAction == 0)
            {
                // couldn't find it???
                Log.Warning("Could not find AddPreTickAction; prayer spots broken");
                foreach (var c in code)
                {
                    yield return c;
                }

                yield break;
            }

            // Splice in toil.AddFinishAction(delegate () {Utilities.GiveRelaxAloneThought((JobDriver)this)));
            code.InsertRange(locationOfLastPreTickAction + 1, //insert after it
                new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_2), // this toil
                    new CodeInstruction(OpCodes.Ldloc_1), // this JobDriver_RelaxAlone
                    // load the function we want to call as an Action:
                    //   NOTE: GiveRelaxAloneThought is
                    //   void GiveRelaxAloneThought(JobDriver driver)...
                    new CodeInstruction(OpCodes.Ldftn,
                        AccessTools.Method(typeof(Utilities), "GiveRelaxAloneThought")),
                    // Make it an Action:
                    //   NOTE: I have no idea how this constructor works to turn the
                    //   LdFtn above into an Action. The typeof(JobDriver) seems reasonable,
                    //   but the typeof(IntPtr)?  Who knows, but it's needed!
                    new CodeInstruction(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(Action), new[] {typeof(JobDriver), typeof(IntPtr)})),
                    // put that Action into the toil as a FinishAction:
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Toil), "AddFinishAction"))
                });
            // finally, return the new method IL to Harmony:
            foreach (var c in code)
            {
                yield return c;
            }

/*                if (c.opcode==OpCodes.Callvirt &&
                    (MethodInfo)c.operand == Toil_AddPreTickAction) {
                    yield return new CodeInstruction(OpCodes.Ldloc_2); // this toil
                    yield return new CodeInstruction(OpCodes.Ldloc_1); // this JobDriver_RelaxAlone
                    // load the function we want to call as an Action:
                    yield return new CodeInstruction(OpCodes.Ldftn, HarmonyLib.AccessTools.Method(typeof(Utilities), "GivePrayerT"));
                    // Make it an Action:
                    yield return new CodeInstruction(OpCodes.Newobj,
                                                     HarmonyLib.AccessTools.
                                                     Constructor(typeof(System.Action), new Type[] {typeof(JobDriver), typeof(IntPtr)}));
                    //typeof(System.Action).GetConstructor(new Type[] {}));
                    // put that Action into the toil as a FinishAction:
                    yield return new CodeInstruction(OpCodes.Callvirt, HarmonyLib.AccessTools.Method(typeof(Verse.AI.Toil), "AddFinishAction"));
                }
            }*/
        }
    }
}