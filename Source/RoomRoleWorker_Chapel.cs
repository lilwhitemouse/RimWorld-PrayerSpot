using RimWorld;
using Verse;

namespace LWM.PrayerSpot
{
    public class RoomRoleWorker_Chapel : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            var num = 0;
            var containedAndAdjacentThings = room.ContainedAndAdjacentThings;
            foreach (var thing in containedAndAdjacentThings)
            {
                if (thing is Building_Bed)
                {
                    return 0f; // prayer spots in rooms are private *bedroom* spots
                }

                if (thing.def.defName == "LWM_PrayerSpot" || thing.def.defName == "LWM_PrayerSpot_Dir")
                {
                    num++;
                }
                else if (thing.def.defName == "PrayerFocus")
                {
                    num += 6;
                }
            }

            return num * 4f; // So you can have small prayer spots in rooms?
        }
    }
}