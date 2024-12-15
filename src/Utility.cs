using Il2Cpp;

namespace SleepWithoutABed
{

    public static class PenaltyMultiplier
    {
        public static float CalculatePenaltyMultiplier()
        {
            // Retrieve ambient temperature and clothing warmth
            float ambientTemperature = AmbientTemperature.CalculateAmbientTemperature();
            float clothingWarmth = WarmthFromClothing.CalculateWarmth();

            float thresholdTemperature = 0f; // Below this temperature, cold exposure takes effect

            // Calculate body temperature
            float bodyTemperature = ambientTemperature + clothingWarmth;

            // No cold exposure effects if above or equal to the threshold
            if (bodyTemperature >= thresholdTemperature)
                return 0f;

            // Calculate temperature difference
            float effectiveTemperature = thresholdTemperature - bodyTemperature;

            // Gradual scaling based on temperature difference
            float incrementalSensitivity = 0.001f /*Settings.settings.sensitivityScale*/ * (effectiveTemperature / 1 /*Settings.settings.degreeSteps*/);

            // Adjust sensitivity based on calculated scaling
            float adjustedSensitivity = 1.000f /*Settings.settings.temperatureSensitivity*/ + incrementalSensitivity;

            // Apply the penalty multiplier
            return effectiveTemperature * adjustedSensitivity;
        }
    }


    //
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
}
