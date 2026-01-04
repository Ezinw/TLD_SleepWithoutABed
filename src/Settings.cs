using ModSettings;
using System.Reflection;

namespace SleepWithoutABed
{

    internal class Settings : JsonModSettings
    {
        [Section(" ")]

        [Name("Fatigue Recovery Penalty")]
        [Description("Adjust the penalty to fatigue recovery when sleeping with no bed or bedroll. \n\nA setting of 1.00 = TLD default. (Mod default = 0.75)")]
        [Slider(0.15f, 1.00f, NumberFormat = "{0:0.00}")]
        public float fatigueRecoveryPenalty = 0.75f;

        [Name("Health Recovery Scale with no bed")]
        [Description("Adjust the health recovery rate while sleeping without a bed or bedroll. (Mod default = 0.50)")]
        [Slider(0.05f, 1.00f, NumberFormat = "{0:0.00}")]
        public float cloneBedConditionGainPerHour = 0.50f;

        [Name("Freezing Rate")]
        [Description("Increases freezing rate per hour per degree below the threshold. (Mod default = 1.75)")]
        [Slider(1.00f, 3.00f, NumberFormat = "{0:0.00}")]
        public float freezingScale = 1.75f;

        [Name("Freezing Health Loss")]
        [Description("Additional health loss from cold exposure when freezing. \n\nIncrease to amplify health loss if sleeping or passing time without a bed or bedroll. \n(Mod default = 1.20)")]
        [Slider(1.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float freezingHealthLoss = 1.20f;

        [Name("Hypothermic Health Loss")]
        [Description("Additional health loss from cold exposure when suffering from hypothermia. \n\nIncrease to amplify health loss if sleeping or passing time without a bed or bedroll. (Mod default = 1.40)")]
        [Slider(1.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float hypothermicHealthLoss = 1.40f;

        [Name("Pass Time Exposure")]
        [Description("Reduce cold exposure effects while passing time or increase setting to match the sleeping cold exposure effect. (Mod default = 0.75)")]
        [Slider(0.25f, 2.00f, NumberFormat = "{0:0.00}")]
        public float passTimeExposurePenalty = 0.75f;

        [Section(" ")]

        [Name("Enable Low Health Sleep Interruption?")]
        [Description("Enable/Disable low health sleep interruption when sleeping without a bed or bedroll.")]
        [Choice("No", "Yes")]
        public bool lowHealthSleepInterruption = true;

        [Name("         - Low Health Sleep Interruption")]
        [Description("Interrupts sleep/passing time if health drops below the threshold, giving a chance of survival. \n\nA setting of 0.10 will wake the player at 10% health. (Mod default = 0.10)")]
        [Slider(0.05f, 0.20f, NumberFormat = "{0:0.00}")]
        public float sleepInterruptionThreshold = 0.10f;

        [Name("         - Interruption Cooldown")]
        [Description("Control how often the sleep/passtime interruption occurs. \n\nInterruption will only reoccur after this amount of time has passed (in seconds). (Mod default = 15)")]
        [Slider(1, 60)]
        public int interruptionCooldown = 15;

        [Name("         - Display HUD message?")]
        [Description("Show or hide the HUD message to the player when sleep is interrupted.")]
        [Choice("No", "Yes")]
        public bool hudMessage = true;

        [Name("         - Apply Interruption To All Beds?")]
        [Description("Applies the low health sleep interruption to all beds and bedrolls and wakes the player up if health drops below the threshold.")]
        [Choice("No", "Yes")]
        public bool applyInterruptToBeds = true;


        // -------------------------------------------------------------------------------------------------------------------------------- //


        [Section(" ")]

        [Name("Extra Options")]
        [Description("Show or hide extra options")]
        [Choice("+", "-")]
        public bool extraOptions = false;

        [Name("Show Cold Exposure Options?")]
        [Description("Show or hide additional exposure-related options. \n\nHere you can fine-tune how temperature affects exposure penalties. \n\nOnly adjust these values if you want to experiment with lesser or more harsh exposure effects")]
        [Choice("+", "-")]
        public bool exposureSettings = false;
        
        [Name("         - Sensitivity Scale")]
        [Description("Determines how much the cold exposure penalty increases as temperature drops below freezing. \n\nHigher values result in a steeper penalty curve, making cold exposure harsher. (Mod default = 0.20)")]
        [Slider(0.01f, 1.00f, NumberFormat = "{0:0.00}")]
        public float sensitivityScale = 0.20f;

        [Name("         - Adjusted Sensitivity")]
        [Description("Controls the baseline impact of cold exposure penalties. Higher values increase the penalty effect, while lower values make it less severe. \n(Mod default = 0.75)")]
        [Slider(0.01f, 2.00f, NumberFormat = "{0:0.00}")]
        public float adjustedSensitivity = 0.75f;


        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == nameof(lowHealthSleepInterruption) ||
                field.Name == nameof(sleepInterruptionThreshold) ||
                field.Name == nameof(interruptionCooldown) ||
                field.Name == nameof(hudMessage) ||
                field.Name == nameof(applyInterruptToBeds) ||

                field.Name == nameof(extraOptions) ||

                field.Name == nameof(exposureSettings) ||

                
                field.Name == nameof(sensitivityScale) ||
                field.Name == nameof(adjustedSensitivity))
                
            {
                Refresh();
            }
        }

        internal void Refresh()
        {
            SetFieldVisible(nameof(sleepInterruptionThreshold), lowHealthSleepInterruption);
            SetFieldVisible(nameof(interruptionCooldown), lowHealthSleepInterruption);
            SetFieldVisible(nameof(hudMessage), lowHealthSleepInterruption);
            SetFieldVisible(nameof(applyInterruptToBeds), lowHealthSleepInterruption);

            SetFieldVisible(nameof(exposureSettings), extraOptions);

            SetFieldVisible(nameof(sensitivityScale), exposureSettings && extraOptions);
            SetFieldVisible(nameof(adjustedSensitivity), exposureSettings && extraOptions);
        }


        internal static Settings settings;
        internal static void OnLoad()
        {
            settings = new Settings();
            settings.AddToModSettings("SleepWithoutABed");
            settings.Refresh();
        }
    }
}