using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

// for OpCodes in Harmony Transpiler

/***************** Patch two parts of JobDriver_RelaxAlone: ***********************/
/*                   One of the toils, and MakeToils itself                       */
namespace LWM.PrayerSpot
{
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
     * Of course, it's in an anonymous delegate function, but...
     *     it's no longer inside an anonymouse Iterator class! The update to 1.1 made some things easier!
     *     (Interesting read on how it's handled:  https://blogs.msdn.microsoft.com/oldnewthing/20060802-00/?p=30263/)
     */
    // Harmony can directly find the delegate, if we have the IL name:
    [HarmonyPatch(typeof(JobDriver_RelaxAlone), "<MakeNewToils>b__8_1")]
    public static class Patch_JobDriver_RelaxAlone_Toil_Delegate
    {
        private static readonly ThingDef
            PrayerSpotDirectionalDef = DefDatabase<ThingDef>.GetNamed("LWM_PrayerSpot_Dir");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var callToRandomDirection = AccessTools.Method("Verse.Rot4:get_Random");
            // Should never fail.
            if (callToRandomDirection == null)
            {
                Log.Error("LWM.PrayerSpot: Failed to find Verse.Rot4:get_Random");
            }

            //  RimWorld.JobDriver_RelaxAlone RimWorld.JobDriver_RelaxAlone/'<MakeNewToils>c__Iterator0'::$this
            foreach (var code in instructions)
            {
                //Log.Message("Code: "+code.opcode+": "+code.operand);
                if (code.opcode == OpCodes.Call && (MethodInfo) code.operand == callToRandomDirection)
                {
                    //Log.Message("Patching to remove Rot4.Random!");
                    // Replace with our call:
                    //   first put pawn on stack:
                    var c = new CodeInstruction(OpCodes.Ldarg_0) {labels = code.labels}; // the JobDriver_RelaxAlone
                    // someone may want to jump there, so take any labels
                    yield return c;
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(JobDriver_RelaxAlone).GetField("pawn"));
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method("LWM.PrayerSpot.Patch_JobDriver_RelaxAlone_Toil_Delegate:faceDir"));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static Rot4 faceDir(Pawn pawn)
        {
            // pawn is spawned: it just started a job:p
            // pawn has a map: it just started a job:p
            var spot = pawn.Map.thingGrid.ThingAt(pawn.Position, PrayerSpotDirectionalDef);
            if (spot == null)
            {
                return Rot4.Random;
            }

            return spot.Rotation;
        }
    }

    /* Patch Rimworld/JobDriver_RelaxAlone's final Toil:
     *   toil.AddFinishAction(Utilities.GiveRelaxAloneThought((JobDriver)this));
     * #DeepMagic
     */
    /* Technical notes:
     * The toils are returned inside a hidden "inner" iterator class,
     * and are returned inside the MoveNext() method of that class.
     * So to patch the method, we first have to find it, then we
     * use Transpiler to add the AddFinishAction call right after
     * the (last) AddPreTickAction
     */

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