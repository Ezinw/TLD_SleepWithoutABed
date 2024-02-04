using ModSettings;

namespace SleepWithoutABed
{

    internal class Settings : JsonModSettings
    {
        public enum Choice
        {
            Default, ThreeQuarters, Half, Quarter, Eighth
        }

        [Name("Sleeping penalty when not using a bed")]
        [Description("Set the penalty to the amount of fatigue restored while sleeping without a bed - (Mod default = 1/2 of TLD default value)")]
        [Choice("TLD Default", "3/4 of default", "1/2 of default", "1/4 of default", "1/8 of default")]
        public Choice sleepPenalty = Choice.Half;


        internal static Settings settings;
        internal static void OnLoad()
        {
            settings = new Settings();
            settings.AddToModSettings("SleepWithoutABed");
        }
    
    }

}