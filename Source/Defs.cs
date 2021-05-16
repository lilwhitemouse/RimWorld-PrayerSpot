using RimWorld;
using Verse;

namespace LWM.PrayerSpot
{
    // RimWorld does magic to pull the defs from xml to these variables:
    [DefOf]
    public static class Defs
    {
        public static ThingDef LWM_PrayerSpot;
        public static ThingDef LWM_PrayerSpot_Dir;
        public static ThingDef LWM_PrayerFocus;

        public static RoomRoleDef LWM_Chapel;

        public static ThoughtDef LWM_PrayedInChapel;
    }
}