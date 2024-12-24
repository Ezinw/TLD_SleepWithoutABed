using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SleepWithoutABed
{
    [HarmonyPatch(typeof(Rest), nameof(Rest.UpdateWhenSleeping))]
    public static class SleepInterruption
    {
        // Static variable to track the time of the last interruption
        private static float lastInterruptionTime = -1f; // Initialize to -1 to indicate no interruptions yet
        private static float interruptionCooldown = /*30f*/ Settings.settings.interruptionCooldown; // Cooldown period in seconds

        static void Prefix(Rest __instance)
        {
            if (GameManager.GetPlayerManagerComponent().PlayerIsDead())
                return;

            // Fetch the setting to determine if interruptions should apply to beds
            bool applyInterruptToBeds = Settings.settings.applyInterruptToBeds;

            if (GameManager.GetRestComponent().m_Bed == null || applyInterruptToBeds)
            {
                var conditionComponent = GameManager.GetConditionComponent();
                var freezingComponent = GameManager.GetFreezingComponent();

                if (conditionComponent == null || freezingComponent == null)
                    return;

                // Calculate the current condition percentage
                float effectiveMaxCondition = GetEffectiveMaxCondition.CalculateMaxCondition();
                float currentConditionPercentage = conditionComponent.m_CurrentHP / effectiveMaxCondition;
                float conditionThreshold = Settings.settings.lowHealthSleepInterruption;

                // Check if the player is freezing
                bool isFreezing = freezingComponent.IsFreezing();

                // Interrupt sleep when freezing starts if that custom option is enabled
                var currentExperienceMode = GameManager.GetExperienceModeManagerComponent()?.GetCurrentExperienceMode();

                if (currentExperienceMode != null && currentExperienceMode.m_ShouldInterruptSleepIfFreezing)
                {
                    // Check if the player is sleeping without a bed and should be interrupted due to freezing
                    var restComponent = GameManager.GetRestComponent();
                    if (restComponent != null && restComponent.m_Bed == null)
                    {
                        bool shouldInterruptSleep = restComponent.ShouldInterruptIfFreezingStartsWhileSleeping();

                        if (shouldInterruptSleep)
                        {
                            restComponent.m_ShouldInterruptWhenFreezing = true;

                            // Display a HUDMessage to the player
                            HUDMessage.AddMessage(Localization.Get("You woke up freezing"), 5f, false, false);
                        }
                    }
                }

                // Check if the cooldown has expired
                float currentTime = Time.time;
                if (lastInterruptionTime >= 0 && currentTime - lastInterruptionTime < interruptionCooldown)
                {
                    // Cooldown active, do not interrupt sleep
                    return;
                }

                // Only interrupt sleep if the condition is below the threshold and the player is freezing
                if (currentConditionPercentage <= conditionThreshold && isFreezing)
                {
                    __instance.EndSleeping(true); // Interrupt sleep

                    // Record the time of this interruption
                    lastInterruptionTime = currentTime;

                    HUDMessage.AddMessage(Localization.Get("You are about to fade into the long dark. Seek shelter and warmth!"), 5f, false, false);

                    // Fade in after sleep interruption
                    CameraFade.FadeIn(0.5f, 0f, null);
                }
            }
        }
    }




    [HarmonyPatch(typeof(PassTime), nameof(PassTime.UpdatePassingTime))]
    public static class PassTimeInterruption
    {
        // Static variable to track the time of the last interruption
        private static float lastInterruptionTime = -1f; // Initialize to -1 to indicate no interruptions yet
        private static float interruptionCooldown = /*30f*/ Settings.settings.interruptionCooldown; // Cooldown period in seconds

        static void Prefix(PassTime __instance)
        {
            if (GameManager.GetPlayerManagerComponent().PlayerIsDead())
                return;

            // Fetch the setting to determine if interruptions should apply to beds
            bool applyInterruptToBeds = Settings.settings.applyInterruptToBeds;

            // Check the bed status
            if (__instance.m_Bed != null && !applyInterruptToBeds)
            {
                // If a bed is being used and interruptions are disabled for beds, do nothing
                return;
            }

            var conditionComponent = GameManager.GetConditionComponent();
            var freezingComponent = GameManager.GetFreezingComponent();

            if (conditionComponent == null || freezingComponent == null)
                return;

            // Calculate the effective max condition
            float effectiveMaxCondition = GetEffectiveMaxCondition.CalculateMaxCondition();
            float currentConditionPercentage = conditionComponent.m_CurrentHP / effectiveMaxCondition;
            float conditionThreshold = Settings.settings.lowHealthSleepInterruption;

            // Check if the player is freezing
            bool isFreezing = freezingComponent.IsFreezing();

            // Check if the cooldown has expired
            float currentTime = Time.time;
            if (lastInterruptionTime >= 0 && currentTime - lastInterruptionTime < interruptionCooldown)
            {
                // Cooldown active, do not interrupt passing time
                return;
            }

            // Only interrupt passing time if the condition is below the threshold and the player is freezing
            if (currentConditionPercentage <= conditionThreshold && isFreezing)
            {
                __instance.End(); // Interrupt passing time

                // Record the time of this interruption
                lastInterruptionTime = currentTime;
                
                HUDMessage.AddMessage(Localization.Get("You are about to fade into the long dark. Seek shelther and warmth!"), 5f, false, false);
            }
        }
    }
}
