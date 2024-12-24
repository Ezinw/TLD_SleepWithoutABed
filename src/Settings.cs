using ModSettings;
using System.Reflection;

namespace SleepWithoutABed
{

    internal class Settings : JsonModSettings
    {
        public enum Choice
        {
            Default, ThreeQuarters, Half, Quarter, Eighth
        }

        [Name("         - Fatigue Recovery Penalty")]
        [Description("Adjust the penalty to fatigue recovery when sleeping with no bed/bedroll. (Mod default = 1/2)")]
        [Choice("Full Recovery", "3/4", "1/2", "1/4", "1/8")]
        public Choice fatigueRecoveryPenalty = Choice.Half;

        [Section(" ")]

        /*
        [Name("         - Temperature Sensitivity")]
        [Description("Control the sensitivity of cold exposure effects based on how far the players total calculated body temperature falls below freezing (0°C). (Mod default = 1.00) - Increase for a harsher experience, decrease for a less severe effect.)")]
        [Slider(0.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float temperatureSensitivity = 1.00f;

        [Name("         - Sensitivity Scale")]
        [Description("Adjusts the rate at which sensitivity increases as the temperature drops below the threshold. Lower values result in more gradual penalties, while higher values increase penalties more quickly. (Default = 0.002)")]
        [Slider(0.000f, 0.010f, NumberFormat = "{0:0.000}")]
        public float sensitivityScale = 0.001f;


        [Name("         - Degree Scale")]
        [Description("Defines the temperature range (in degrees) for each incremental adjustment to sensitivity. Lower values create more frequent scaling adjustments, while higher values result in larger steps between adjustments. (Default = 1)")]
        [Slider(1, 20)]
        public int degreeSteps = 1;

        [Section(" ")]
        */

        [Name("         - Freezing Rate")]
        [Description("Adjust the rate at which the player freezes when sleeping or passing time without a bed/bedroll. (Default = 1.50)")]
        [Slider(0.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float freezingRate = 1.50f;

        [Name("         - Freezing Health Loss")]
        [Description("Additional health loss from cold exposure when freezing. Increase to amplify health loss if sleeping or passing time without a bed/bedroll. (Mod default = 0.20)")]
        [Slider(0.00f, 1.00f, NumberFormat = "{0:0.00}")]
        public float freezingHealthLoss = 0.20f;

        [Name("         - Hypothermic Health Loss")]
        [Description("Additional health loss from cold exposure when suffering from hypothermia. Increase to amplify health loss if sleeping or passing time without a bed/bedroll. (Mod default = 0.40)")]
        [Slider(0.00f, 1.00f, NumberFormat = "{0:0.00}")]
        public float hypothermicHealthLoss = 0.40f;

        [Name("         - Pass Time Exposure")]
        [Description("Reduce cold exposure effects while passing time or increase setting to match the sleeping cold exposure effect. (Mod default = 0.15)")]
        [Slider(0.00f, 1.00f, NumberFormat = "{0:0.00}")]
        public float passTimeEffectModifier = 0.15f;

        [Section(" ")]

        [Name("         - Health Recovery Scale with no bed")]
        [Description("Adjust the health recovery rate while sleeping without a bed/bedroll if not freezing. (Mod default = 1.65)")]
        [Slider(0.00f, 3.30f, NumberFormat = "{0:0.00}")]
        public float nullBedConditionGainPerHour = 1.65f;

        [Section(" ")]

        [Name("         - Extra Options")]
        [Description("Show or hide extra options")]
        [Choice("+", "-")]
        public bool extraOptions = false;

        [Name("                  - Low Health Sleep Interruption")]
        [Description("Interrupts sleep/passing time if health drops below the threshold, giving a chance of survival. A setting of 0.10 would wake the player at 10% health. Set to 0 to disable. (Mod default = 0.00)")]
        [Slider(0.00f, 0.20f, NumberFormat = "{0:0.00}")]
        public float lowHealthSleepInterruption = 0.00f;

        [Name("                           - Interruption Cooldown")]
        [Description("Control how often the sleep/passtime interruption occurs. It will only reoccur after this amount of time has passed (in seconds). (Default = 30)")]
        [Slider(1, 60)]
        public int interruptionCooldown = 30;

        [Name("                                             - Apply Interruption to Beds?")]
        [Description("Applies the low health sleep interruption to beds/bedrolls and wakes the player up if health drops below the threshold. (Mod default = Disabled)")]
        [Choice("Disabled", "Enabled")]
        public bool applyInterruptToBeds = false;

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == nameof(extraOptions) ||
                field.Name == nameof(lowHealthSleepInterruption) ||
                field.Name == nameof(interruptionCooldown) ||
                field.Name == nameof(applyInterruptToBeds))

            {
                RefreshGUI();
            }
        }

        internal void RefreshGUI()
        {
            SetFieldVisible(nameof(lowHealthSleepInterruption), extraOptions);
            SetFieldVisible(nameof(interruptionCooldown), extraOptions);
            SetFieldVisible(nameof(applyInterruptToBeds), extraOptions);
        }


        internal static Settings settings;
        internal static void OnLoad()
        {
            settings = new Settings();
            settings.AddToModSettings("SleepWithoutABed");
            settings.RefreshGUI();
        }
    }
}