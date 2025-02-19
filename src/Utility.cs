using Il2Cpp;

namespace SleepWithoutABed
{

    public static class ExposurePenalty
    {
        public static float ApplyExposurePenalty()
        {
            // Retrieve ambient temperature and clothing warmth
            float ambientTemperature = AmbientTemperature.CalculateAmbientTemperature();
            float clothingWarmth = WarmthFromClothing.CalculateWarmth();

            // Below this temperature, cold exposure takes effect
            float thresholdTemperature = 0f;

            // Calculate body temperature
            float bodyTemperature = ambientTemperature + clothingWarmth;

            // No cold exposure effects if above or equal to the threshold
            if (bodyTemperature >= thresholdTemperature)
                return 0f;

            // Calculate temperature difference
            float effectiveTemperature = thresholdTemperature - bodyTemperature;

            // Scale the penalty based on effective temperature and the sensitivity setting
            float incrementalSensitivity = Settings.settings.sensitivityScale * effectiveTemperature;

            // Determine the final sensitivity by adding the base adjusted sensitivity to sensitivity scale
            float adjustedSensitivity = Settings.settings.adjustedSensitivity + incrementalSensitivity;

            // Apply the penalty 
            return effectiveTemperature * adjustedSensitivity; 
        }
    }


    
    public static class WarmthFromClothing
    {
        public static float CalculateWarmth()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();

            if (playerManager == null)
            {
                return 0f;
            }

            // Calculate clothing warmth
            float clothingWarmth = playerManager.m_WarmthBonusFromClothing;

            // Calculate windproofing from clothing
            float windproofBonus = playerManager.m_WindproofBonusFromClothing;

            //
            return clothingWarmth + windproofBonus;
        }
    }



    public static class AmbientTemperature
    {
        public static float CalculateAmbientTemperature()
        {
            var weatherComponent = GameManager.GetWeatherComponent();

            // Return a default value if the weather component is not available
            if (weatherComponent == null)
            {
                return 0f;
            }

            // Ambient temperature with windchill
            return weatherComponent.GetCurrentTemperatureWithWindchill();
        }
    }



    public static class GetEffectiveMaxCondition
    {
        public static float CalculateMaxCondition()
        {
            // Get the condition component from the game manager
            var conditionComponent = GameManager.GetConditionComponent();

            // If the condition component exists, return its max HP value (e.g., accounts for buffs/debuffs).
            // Otherwise, return the default max condition value of 100.
            return conditionComponent != null ? conditionComponent.m_MaxHP : 100f;
        }
    }
}