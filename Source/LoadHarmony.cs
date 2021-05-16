using System.Reflection;
using HarmonyLib;
using Verse;

namespace LWM.PrayerSpot
{
    [StaticConstructorOnStartup]
    public static class LoadHarmony
    {
        static LoadHarmony()
        {
            new Harmony("net.littlewhitemouse.PrayerSpot").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}