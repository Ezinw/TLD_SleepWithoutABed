using ModSettings;
using System.Reflection;

namespace SleepWithoutABed
{

    internal class Settings : JsonModSettings
    {
        [Section(" ")]

        [Name("         - Fatigue Recovery Penalty")]
        [Description("Adjust the penalty to fatigue recovery when sleeping with no bed/bedroll. A setting of 1.00 = TLD default. (Mod default = 0.50)")]
        [Slider(0.15f, 1.00f, NumberFormat = "{0:0.00}")]
        public float fatigueRecoveryPenalty = 0.50f;

        [Name("         - Health Recovery Scale with no bed")]
        [Description("Adjust the health recovery rate while sleeping without a bed/bedroll. (Mod default = 0.50)")]
        [Slider(0.05f, 1.00f, NumberFormat = "{0:0.00}")]
        public float cloneBedConditionGainPerHour = 0.50f;

        [Section(" ")]

        [Name("         - Freezing Rate")]
        [Description("Increases freezing rate per hour per degree below the threshold. (Mod default = 1.75)")]
        [Slider(1.00f, 3.00f, NumberFormat = "{0:0.00}")]
        public float freezingScale = 1.75f;

        [Name("         - Freezing Health Loss")]
        [Description("Additional health loss from cold exposure when freezing. Increase to amplify health loss if sleeping or passing time without a bed/bedroll. (Mod default = 1.20)")]
        [Slider(1.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float freezingHealthLoss = 1.20f;

        [Name("         - Hypothermic Health Loss")]
        [Description("Additional health loss from cold exposure when suffering from hypothermia. Increase to amplify health loss if sleeping or passing time without a bed/bedroll. (Mod default = 1.40)")]
        [Slider(1.00f, 2.00f, NumberFormat = "{0:0.00}")]
        public float hypothermicHealthLoss = 1.40f;

        [Name("         - Pass Time Exposure")]
        [Description("Reduce cold exposure effects while passing time or increase setting to match the sleeping cold exposure effect. (Mod default = 0.75)")]
        [Slider(0.25f, 2.00f, NumberFormat = "{0:0.00}")]
        public float passTimeEffectModifier = 0.75f;

        
        // -------------------------------------------------------------------------------------------------------------------------------- //
        
        
        [Section(" ")]

        [Name("         - Extra Options")]
        [Description("Show or hide extra options")]
        [Choice("+", "-")]
        public bool extraOptions = false;

        [Name("         - Show Cold Exposure Options?")]
        [Description("Show or hide additional exposure-related options. Here you can fine-tune how temperature affects exposure penalties. Only adjust these values if you want to experiment with lesser or more harsh exposure effects")]
        [Choice("+", "-")]
        public bool exposureSettings = false;

        [Name("                  - Sensitivity Scale")]
        [Description("Determines how much the cold exposure penalty increases as temperature drops below freezing. Higher values result in a steeper penalty curve, making cold exposure harsher. (Mod default = 0.20)")]
        [Slider(0.01f, 1.00f, NumberFormat = "{0:0.00}")]
        public float sensitivityScale = 0.20f;

        [Name("                  - Adjusted Sensitivity")]
        [Description("Controls the baseline impact of cold exposure penalties. Higher values increase the penalty effect, while lower values make it less severe. (Mod default = 0.75)")]
        [Slider(0.01f, 2.00f, NumberFormat = "{0:0.00}")]
        public float adjustedSensitivity = 0.75f;

        [Section(" ")]

        [Name("         - Low Health Sleep Interruption")]
        [Description("Interrupts sleep/passing time if health drops below the threshold, giving a chance of survival. A setting of 0.10 would wake the player at 10% health. Set to 0 to disable. (Mod default = 0.10")]
        [Slider(0.00f, 0.20f, NumberFormat = "{0:0.00}")]
        public float lowHealthSleepInterruption = 0.10f;

        [Name("                  - Interruption Cooldown")]
        [Description("Control how often the sleep/passtime interruption occurs. It will only reoccur after this amount of time has passed (in seconds). (Mod default = 15)")]
        [Slider(1, 60)]
        public int interruptionCooldown = 15;

        [Name("                  - Display HUD message?")]
        [Description("Show or hide the HUD message to the player when sleep is interrupted.")]
        [Choice("No", "Yes")]
        public bool hudMessage = true;

        [Name("                  - Apply Interruption to Beds?")]
        [Description("Applies the low health sleep interruption to beds/bedrolls and wakes the player up if health drops below the threshold.")]
        [Choice("No", "Yes")]
        public bool applyInterruptToBeds = true;

        [Section(" ")]

        [Name("         - Predator Interruption Options")]
        [Description("Show or hide options that control how often a predator can interrupt sleep with an attack. WARNING: Due to a bug in the game the struggle camera might act strangely or remain black throughout some struggles. Here you can adjust the probability of an attack or disable it.")]
        [Choice("+", "-")]
        public bool predatorInterruptoinOptions = false;

        [Name("                  - % Attack Chance")]
        [Description("Set the percent chance a predator will interrupt sleeping when outside. (TLD default = 75%)")]
        [Slider(0, 75)]
        public int predatorRestInterruption = 75;

        [Name("                  - % Attack Chance In Shelter")]
        [Description("Set the percent chance a predator will interrupt sleeping when inside a snow shelter. (TLD default = 25%)")]
        [Slider(0, 25)]
        public int predatorRestInterruptionShelter = 25;


        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == nameof(extraOptions) ||

                field.Name == nameof(exposureSettings) ||
                field.Name == nameof(sensitivityScale) ||
                field.Name == nameof(adjustedSensitivity) ||

                field.Name == nameof(lowHealthSleepInterruption) ||
                field.Name == nameof(interruptionCooldown) ||
                field.Name == nameof(hudMessage) ||
                field.Name == nameof(applyInterruptToBeds) ||

                field.Name == nameof(predatorInterruptoinOptions) ||
                field.Name == nameof(predatorRestInterruption) ||
                field.Name == nameof(predatorRestInterruptionShelter))


            {
                Refresh();
            }
        }

        internal void Refresh()
        {
            SetFieldVisible(nameof(lowHealthSleepInterruption), extraOptions);

            SetFieldVisible(nameof(exposureSettings), extraOptions);
            SetFieldVisible(nameof(sensitivityScale), exposureSettings && extraOptions);
            SetFieldVisible(nameof(adjustedSensitivity), exposureSettings && extraOptions);

            SetFieldVisible(nameof(interruptionCooldown), extraOptions);
            SetFieldVisible(nameof(hudMessage), extraOptions);
            SetFieldVisible(nameof(applyInterruptToBeds), extraOptions);

            SetFieldVisible(nameof(predatorInterruptoinOptions), extraOptions);
            SetFieldVisible(nameof(predatorRestInterruption), predatorInterruptoinOptions && extraOptions);
            SetFieldVisible(nameof(predatorRestInterruptionShelter), predatorInterruptoinOptions && extraOptions);
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