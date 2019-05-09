using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using Harmony;
using System.Reflection;
using System.Reflection.Emit; // for OpCodes in Harmony Transpiler


namespace LWM.PrayerSpot {
    /* Patch RimWorld/JobDriver_RelaxAlone's MakeNewToils
     * Change it to specify facing when on a PrayerSpot
     * 
     * The line in the code is something like this:
     *  relax.initAction = delegate()
     *  {
     *    this.faceDir = ((!this.job.def.faceDir.IsValid) ? Rot4.Random : this.job.def.faceDir);
     *  };
     * We want to replace Rot4.Random with LWM.PrayerSpot.Patch_JobDriver_RelaxAlone_Toil_Delegate.faceDir(pawn)
     *
     * Of course, it's in an anonymous delegate function inside an anonymouse Iterator class: 
     *       c__Iterator0's <>m__1 (<>m__2 is the PreTickAction, etc)
     *       Of course.
     *       (Interesting read on how it's handled:  https://blogs.msdn.microsoft.com/oldnewthing/20060802-00/?p=30263/)
     */
    [HarmonyPatch]
    public static class Patch_JobDriver_RelaxAlone_Toil_Delegate {
        public static Type iteratorClass;
        static MethodBase TargetMethod()//The target method is found using the custom logic defined here
        {
            //<>m__1 is the hidden IL class that is created for the delegate() function
            iteratorClass = typeof(RimWorld.JobDriver_RelaxAlone).GetNestedTypes(Harmony.AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__Iterator0"));
            if (iteratorClass == null) {
                Log.Error("LWM.PrayerSpot: Could not find RimWorld.JobDriver_RelaxAlone:c__Iterator0 to patch.");
                Log.Error("  The author should know about this.  Your game is still okay to play.");
                return null;
            }
            // This fails, by the way:  iteratorClass.GetMethod(alreadyFoundMethod.Name).
            //   No idea why.
            // Anyway, now get <>m__1 (the preinit action for the toil)
            // #DeepMagic
            var m = iteratorClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("m__1"));
            if (m == null) {
                Log.Error("LWM.PrayerSpot: Could not find JobDriver_RelaxAlone::c__Iterator0:<>m__1");
            }
            return m;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var callToRandomDirection=Harmony.AccessTools.Method("Verse.Rot4:get_Random");
            // Should never fail.
            if (callToRandomDirection == null) Log.Error("LWM.PrayerSpot: Failed to find Verse.Rot4:get_Random");
            //  RimWorld.JobDriver_RelaxAlone RimWorld.JobDriver_RelaxAlone/'<MakeNewToils>c__Iterator0'::$this
            var loadHiddenClassThis=iteratorClass.GetFields(AccessTools.all)
                .FirstOrDefault(t=>t.Name.Contains("this"));
            if (loadHiddenClassThis==null) Log.Error("LWM.PrayerSpot: Failed to find Iterator's field This");
            if (callToRandomDirection == null || loadHiddenClassThis==null ){
                Log.Error("LWM.PrayerSpot: Failsafe active: you may still play");
                foreach (var x in instructions) {
                    yield return x;
                }
                yield break;
            }
            foreach (var code in instructions) {
                //Log.Message("Code: "+code.opcode+": "+code.operand);
                if (code.opcode==OpCodes.Call && code.operand==callToRandomDirection) {
                    //Log.Message("Patching to remove Rot4.Random!");
                    // Replace with our call:
                    //   first put pawn on stack:
                    var c=new CodeInstruction(OpCodes.Ldarg_0);
                    c.labels=code.labels; // someone jumps there, so we need the labels
                    yield return c;
                    yield return new CodeInstruction(OpCodes.Ldfld, loadHiddenClassThis);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(JobDriver_RelaxAlone).GetField("pawn"));
                    yield return new CodeInstruction(OpCodes.Call,
                          Harmony.AccessTools.Method("LWM.PrayerSpot.Patch_JobDriver_RelaxAlone_Toil_Delegate:faceDir"));
                } else
                    yield return code;
            }
            yield break;
        }

        public static Verse.Rot4 faceDir(Pawn pawn) {
            // pawn is spawned: it just started a job:p
            // pawn has a map: it just started a job:p
            Thing spot=pawn.Map.thingGrid.ThingAt(pawn.Position, PrayerSpotDirectionalDef);
            if (spot==null) return Rot4.Random;
            return spot.Rotation;
        }
        static ThingDef PrayerSpotDirectionalDef=DefDatabase<ThingDef>.GetNamed("LWM_PrayerSpot_Dir");
        
    }
    // Should go in own file, I suppose...
    [StaticConstructorOnStartup]
    public static class LoadHarmony {
        static LoadHarmony()
        {
            HarmonyInstance.Create("net.littlewhitemouse.PrayerSpot").PatchAll(Assembly.GetExecutingAssembly());
        }

    }

    /* Original approach:
        patch xml def of Pray to use:
        public class JobDriver_Spiritual_Simple : JobDriver_RelaxAlone {
          protected override IEnumerable<Toil> MakeNewToils() {
            // grab toils from parent, if it has a preinit action, it's the one that
            // makes facing, so modify it, etc.
          } // end toils
        }
     * But.  This will be more compatible, I think.
     */
}
