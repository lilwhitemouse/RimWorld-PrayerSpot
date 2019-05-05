using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;


namespace LWM.PrayerSpot {
    public class JoyGiver_Prayer_Simple : JoyGiver {
        static JoyGiver_Prayer_Simple() {
            if (PrayerSpotDef == null) {
                Log.Error("Failed to load PrayerSpot Def");
            }
            vanillaPray = (JoyGiver)Activator.CreateInstance(typeof(JoyGiver_InPrivateRoom));
            vanillaPray.def= DefDatabase<JoyGiverDef>.GetNamed("Pray");
        }

        static Random rng = new Random();

        public override Job TryGiveJob(Pawn pawn) {
            if (pawn.Map == null) return null;
            List<Building> mapPrayerSpots = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(PrayerSpotDef).ToList();
            Building spot = null;
            // Randomize mapPrayerSpots:
            //   (I got this off the internet)
            /*
            for (int i=mapPrayerSpots.Count-1; i>0; i--) {
                int j = rng.Next(i + 1);
                spot = mapPrayerSpots[j];
                mapPrayerSpots[j] = mapPrayerSpots[i];
                mapPrayerSpots[i] = spot;
            }*/
            for (int i = mapPrayerSpots.Count - 1; i >= 0; i--) { // >=0 so we try 0th spot
                int j = rng.Next(i + 1);
                spot = mapPrayerSpots[j];
                // See if it works:
                Log.Message("Attempting " + spot + " at " + spot.Position);

                Room room = spot.GetRoom();
                IntVec3 c;
                // Don't pray in other people's rooms, eh?
                if (room!=null) {
                    Log.Message("  In a room!");
                    if (room.Role == RoomRoleDefOf.PrisonBarracks || room.Role == RoomRoleDefOf.PrisonCell) {
                        // prison room: Should we allow praying in prison rooms?
                        // Seems kind of rude.  TODO: revisit
                        if (pawn.IsPrisoner) {
                            c = spot.Position;
                            if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                                pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None, 1, -1, null, false)) {
                                return new Job(this.def.jobDef, c);
                            }
                        } // /pawn.IsPrisoner
                    } else {
                        // not a prison room
                        IEnumerable<Pawn> owners = room.Owners;
                        if (owners.Contains(pawn)|| !owners.Any()) {
                            c = spot.Position;
                            if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                                pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None, 1, -1, null, false)) {
                                return new Job(this.def.jobDef, c);
                            }
                        }
                    } // /not prison room
                } else { // not in a room, so anyone can use
                    Log.Message("  Rando spot outside a room!");
                    c = spot.Position;
                    if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) && 
                        pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None, 1, -1, null, false)) {
                        return new Job(this.def.jobDef, c);
                    }
                }
                // carry on to the next random spot:
                if (j<i) mapPrayerSpots[j] = mapPrayerSpots[i];
                //mapPrayerSpots[i] = spot; Meh. Done with it anyway
            }
            // All spots failed or there were no prayer spots.
            // Default to vanilla:
            Log.Message("Defaulting to Vanilla:"+vanillaPray+" ("+vanillaPray.def+")");
            return vanillaPray.TryGiveJob(pawn);
        }
        static ThingDef PrayerSpotDef=DefDatabase<ThingDef>.GetNamed("LWM_PrayerSpot");
        static JoyGiver vanillaPray; // vanilla fallback
    }
}
