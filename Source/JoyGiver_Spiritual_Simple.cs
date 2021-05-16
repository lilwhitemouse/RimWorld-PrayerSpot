using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace LWM.PrayerSpot
{
    public class JoyGiver_Spiritual_Simple : JoyGiver_InPrivateRoom
    {
        // for random order:
        private static readonly Random rng = new Random();

        private static readonly JoyGiver vanillaPray; // vanilla fallback

        // I THINK I will need a separate JoyGiver for the vanilla Pray - it's
        //   possible I won't, but this is safe enought to do.  I hope.
        static JoyGiver_Spiritual_Simple()
        {
            if (Defs.LWM_PrayerSpot == null)
            {
                Log.Error("Failed to load PrayerSpot Def");
            }

            vanillaPray = (JoyGiver) Activator.CreateInstance(typeof(JoyGiver_InPrivateRoom));
            vanillaPray.def = DefDatabase<JoyGiverDef>.GetNamed("Pray");
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map == null)
            {
                return null;
            }

            var mapPrayerSpots = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Defs.LWM_PrayerSpot).ToList();
            mapPrayerSpots.AddRange(pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Defs.LWM_PrayerSpot_Dir));

            // Add a null prayerspot to represent vanilla praying/meditating in room:
            //   (but only if there's not already a prayerspot in the room!)
            Room r1;
            if ((r1 = pawn.ownership?.OwnedRoom) != null)
            {
                var hasSpotInRoom = false;
                var l = r1.ContainedAndAdjacentThings;
                foreach (var thing in l)
                {
                    if (thing.def != Defs.LWM_PrayerSpot && thing.def != Defs.LWM_PrayerSpot_Dir)
                    {
                        continue;
                    }

                    hasSpotInRoom = true;
                    break;
                }

                if (!hasSpotInRoom)
                {
                    mapPrayerSpots.Add(null);
                }
            }

            // Randomize mapPrayerSpots:
            //   (I got this off the internet)
            /*
            for (int i=mapPrayerSpots.Count-1; i>0; i--) {
                int j = rng.Next(i + 1);
                spot = mapPrayerSpots[j];
                mapPrayerSpots[j] = mapPrayerSpots[i];
                mapPrayerSpots[i] = spot;
            }*/
            for (var i = mapPrayerSpots.Count - 1; i >= 0; i--)
            {
                // >=0 so we try 0th spot
                var j = rng.Next(i + 1);
                var spot = mapPrayerSpots[j];
                if (spot == null)
                {
                    // pawn in own room.
                    var job = vanillaPray.TryGiveJob(pawn);
                    if (job != null)
                    {
                        return job;
                    } // maybe the door was locked because insects are rampaging in the room?

                    continue;
                }

                var room = spot.GetRoom();
                IntVec3 c;
                // Don't pray in other people's rooms, eh?
                if (room != null)
                {
//                    Log.Message("  In a room!");
                    if (room.Role == RoomRoleDefOf.PrisonBarracks || room.Role == RoomRoleDefOf.PrisonCell)
                    {
                        // prison room: Should we allow praying in prison rooms?
                        // Seems kind of rude.  TODO: revisit
                        if (pawn.IsPrisoner)
                        {
                            c = spot.Position;
                            if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                                pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None))
                            {
                                return new Job(def.jobDef, c);
                            }
                        } // /pawn.IsPrisoner
                    }
                    else
                    {
                        // not a prison room
                        var owners = room.Owners;
                        if (owners.Contains(pawn) || !owners.Any())
                        {
                            c = spot.Position;
                            if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                                pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None))
                            {
                                return JobMaker.MakeJob(def.jobDef, c);
                            }
                        }
                    } // /not prison room
                }
                else
                {
                    // not in a room, so anyone can use
//                    Log.Message("  Rando spot outside a room!");
                    c = spot.Position;
                    if (c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                        pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None))
                    {
                        return JobMaker.MakeJob(def.jobDef, c);
                    }
                }

                // carry on to the next random spot:
                if (j < i)
                {
                    mapPrayerSpots[j] = mapPrayerSpots[i];
                }

                //mapPrayerSpots[i] = spot; Meh. Done with it anyway
            }

            // All spots failed or there were no prayer spots.
            // Default to vanilla:
//            Log.Message("Defaulting to Vanilla:"+vanillaPray+" ("+vanillaPray.def+")");
            return vanillaPray.TryGiveJob(pawn);
        }
    }
}