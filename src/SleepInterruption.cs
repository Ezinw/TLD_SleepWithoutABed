using Il2Cpp;
using HarmonyLib;
using UnityEngine;

namespace SleepWithoutABed
{
    [HarmonyPatch(typeof(Rest), nameof(Rest.RollForRestInterruption))]
    public static class SWaB_PredatorInterruptionChance
    {
        static void Prefix(Rest __instance)
        {
            if (__instance == null)
            {
                return;
            }

            // Settings for predator rest interruptions
            __instance.m_ChancePredatorInterruptionInsideSpawnRegion = Settings.settings.predatorRestInterruption;
            __instance.m_ChancePredatorInterruptionInsideSpawnRegionWhenInSnowShelter = Settings.settings.predatorRestInterruptionShelter;
        }
    }


    // Low health sleep interruption
    [HarmonyPatch(typeof(Rest), nameof(Rest.UpdateWhenSleeping))]
    public static class SWaB_SleepInterruption
    {
        private static float lastInterruptionTime = -1f; // Indicates no interruptions yet
        private static bool lastInterruptionWasConditionBased = false; // Tracks if the last interruption was due to low health
        private static float interruptionCooldown = Settings.settings.interruptionCooldown; // Cooldown period in seconds

        static void Postfix(Rest __instance)
        {
            if (__instance == null || GameManager.GetPlayerManagerComponent().PlayerIsDead())
                return;

            bool applyInterruptToBeds = Settings.settings.applyInterruptToBeds;
            bool isClonedBedroll = SWaB._clonedBedroll != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>();
            bool isRealBed = __instance.m_Bed != null && !isClonedBedroll;

            if (isRealBed && !applyInterruptToBeds)
                return;

            var conditionComponent = GameManager.GetConditionComponent();
            var freezingComponent = GameManager.GetFreezingComponent();

            if (conditionComponent == null || freezingComponent == null)
                return;

            // Calculate effective max condition
            float effectiveMaxCondition = GetEffectiveMaxCondition.CalculateMaxCondition();
            float currentConditionPercentage = conditionComponent.m_CurrentHP / effectiveMaxCondition;
            float conditionThreshold = Settings.settings.lowHealthSleepInterruption;

            // Check if the player is freezing
            bool isFreezing = freezingComponent.IsFreezing();

            float currentTime = Time.time;

            // Only apply cooldown if the last interruption was condition-based
            if (lastInterruptionWasConditionBased && lastInterruptionTime >= 0 && currentTime - lastInterruptionTime < interruptionCooldown)
                return;

            // Only interrupt sleep if the player's condition is below the threshold and the player is freezing.
            if (currentConditionPercentage < conditionThreshold && isFreezing)
            {
                __instance.EndSleeping(true); // Interrupt sleep

                // Record interruption time and mark it as condition-based
                lastInterruptionTime = currentTime;
                lastInterruptionWasConditionBased = true;

                // Optional HUD message
                if (Settings.settings.hudMessage)
                {
                    HUDMessage.AddMessage(Localization.Get("You are about to fade into the long dark. Seek shelter and warmth!"), 5f, false, false);
                }

                // Fade in after interruption
                CameraFade.FadeIn(0.5f, 0f, null);
            }
            else
            {
                // If interrupted for another reason(WakeUpCall), do NOT apply cooldown
                lastInterruptionWasConditionBased = false;
            }
        }
    }


    // Low health passing time interruption
    [HarmonyPatch(typeof(PassTime), nameof(PassTime.UpdatePassingTime))]
    public static class SWaB_PassTimeInterruption
    {
        private static float lastInterruptionTime = -1f; // Indicates no interruptions yet
        private static bool lastInterruptionWasConditionBased = false; // Tracks if the last interruption was due to low health
        private static float interruptionCooldown = Settings.settings.interruptionCooldown; // Cooldown period in seconds

        static void Postfix(PassTime __instance)
        {
            if (__instance == null || GameManager.GetPlayerManagerComponent().PlayerIsDead())
                return;

            bool applyInterruptToBeds = Settings.settings.applyInterruptToBeds;
            bool isClonedBedroll = SWaB._clonedBedroll != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>();
            bool isRealBed = __instance.m_Bed != null && !isClonedBedroll;

            if (isRealBed && !applyInterruptToBeds)
                return;

            var conditionComponent = GameManager.GetConditionComponent();
            var freezingComponent = GameManager.GetFreezingComponent();

            if (conditionComponent == null || freezingComponent == null)
                return;

            // Calculate effective max condition
            float effectiveMaxCondition = GetEffectiveMaxCondition.CalculateMaxCondition();
            float currentConditionPercentage = conditionComponent.m_CurrentHP / effectiveMaxCondition;
            float conditionThreshold = Settings.settings.lowHealthSleepInterruption;

            // Check if the player is freezing
            bool isFreezing = freezingComponent.IsFreezing();

            float currentTime = Time.time;

            // Only apply cooldown if the last interruption was condition-based
            if (lastInterruptionWasConditionBased && lastInterruptionTime >= 0 && currentTime - lastInterruptionTime < interruptionCooldown)
                return;

            // Only interrupt passing time if the player's condition is below the threshold and the player is freezing.
            if (currentConditionPercentage < conditionThreshold && isFreezing)
            {
                __instance.End(); // Interrupt passing time

                // Record interruption time and mark it as condition-based
                lastInterruptionTime = currentTime;
                lastInterruptionWasConditionBased = true;

                if (Settings.settings.hudMessage)
                {
                    HUDMessage.AddMessage(Localization.Get("You are about to fade into the long dark. Seek shelter and warmth!"), 5f, false, false);
                }
            }
            else
            {
                // If interrupted for another reason(WakeUpCall), do NOT apply cooldown
                lastInterruptionWasConditionBased = false;
            }
        }
    }
}

