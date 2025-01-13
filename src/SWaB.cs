using Il2Cpp;
using HarmonyLib;
using UnityEngine;

namespace SleepWithoutABed
{

    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.Enable), new Type[] { typeof(bool), typeof(bool) })]
    public class EnableSleep
    {
        static void Prefix(Panel_Rest __instance, ref bool enable, ref bool passTimeOnly)
        {
            if (GameManager.m_IsPaused ||
                GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            bool inSnowShelter = GameManager.GetSnowShelterManager().PlayerInShelter();
            bool inVehicle = GameManager.GetPlayerInVehicle().IsInside();
            var restComponent = GameManager.GetRestComponent();
            var bed = __instance.m_Bed;

            if (restComponent == null)
            {
                return;
            }

            if (enable)
            {
                if (bed != null)
                {
                    // Default fatigue recovery while sleeping in a bed/bedroll
                    restComponent.m_ReduceFatiguePerHourRest = 8.333333333333333f;
                }
                else if (bed == null && !inVehicle && !inSnowShelter)
                {

                    passTimeOnly = false;

                    // Fatigue recovery penalties
                    restComponent.m_ReduceFatiguePerHourRest = Settings.settings.fatigueRecoveryPenalty switch
                    {
                        Settings.Choice.Default => 8.333333333333333f,  // Full recovery
                        Settings.Choice.ThreeQuarters => 6.25f,               // 3/4 recovery
                        Settings.Choice.Half => 4.166666666666667f,  // Half recovery
                        Settings.Choice.Quarter => 2.083333333333333f,  // 1/4 recovery
                        Settings.Choice.Eighth => 1.041666666666667f,  // 1/8 recovery
                        _ => 8.333333333333333f
                    };
                }
            }

            else if (!enable && bed != null)
            {
                // Reset bed reference when disabling rest
                __instance.m_Bed = null;
            }
        }
    }


    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSleeping))]
    public class RestPanelX
    {
        static void Postfix(PlayerManager __instance, ref bool __result)
        {
            if (GameManager.m_IsPaused ||
                GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            if (__result)
            {
                CloseRestPanel();
            }
        }

        // Close the rest panel
        public static void CloseRestPanel()
        {
            var restPanel = InterfaceManager.GetPanel<Panel_Rest>();
            if (restPanel != null && restPanel.IsEnabled())
            {
                restPanel.Enable(false);
            }
        }
    }


    [HarmonyPatch(typeof(Panel_Rest), "DoRest")]
    public static class PanelRestDoRestPatch
    {
        static bool Prefix(Panel_Rest __instance, int restAmount, bool wakeUpAtFullRest)
        {
            // Check if resting is blocked
            if (GameManager.m_BlockAbilityToRest)
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Check Cabin Fever
            var cabinFever = GameManager.GetCabinFeverComponent();
            if (cabinFever?.HasCabinFever() == true && GameManager.GetPlayerManagerComponent().InHibernationPreventionIndoorEnvironment())
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Check Anxiety
            var anxiety = GameManager.GetAnxietyComponent();
            if (anxiety?.HasAffliction() == true && !anxiety.CanPassTime())
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Check Fear
            var fear = GameManager.GetFearComponent();
            if (fear?.HasAffliction() == true)
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Check if Rest is needed
            var rest = GameManager.GetRestComponent();
            if (rest == null)
            {
                return false; // Prevent crash
            }

            if (!rest.AllowUnlimitedSleep() && GameManager.GetFatigueComponent().m_CurrentFatigue <= __instance.m_AllowRestFatigueThreshold && !rest.RestNeededForAffliction())
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Check Thin Ice
            var iceCracking = GameManager.GetIceCrackingManager();
            if (iceCracking?.IsInsideTrigger() == true)
            {
                GameAudioManager.PlayGUIError();
                return false;
            }

            // Perform Rest Logic
            rest.BeginSleeping(__instance.m_Bed, restAmount, __instance.m_MaxSleepHours);
            __instance.m_Bed?.PlayOpenAudio();
            __instance.m_Bed = null; // Reset bed reference
            __instance.m_SkipRestoreItemInHandsOnExit = true;
            __instance.Enable(false);
            __instance.m_SkipRestoreItemInHandsOnExit = false;
            rest.m_WakeUpAtFullRest = wakeUpAtFullRest;

            return false; // Skip original method
        }
    }

}



