using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SleepWithoutABed
{
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.Enable), new Type[] { typeof(bool), typeof(bool) })]
    public class SWaB
    {
        public static GearItem? _tempBedroll = null;

        static void Prefix(Panel_Rest __instance, ref bool enable, ref bool passTimeOnly)
        {
            passTimeOnly = false; // Enable sleep option

            // Rest panel opened
            if (enable)
            {
                if (__instance.m_Bed != null && __instance.m_Bed != _tempBedroll?.GetComponent<Bed>())
                {
                    return; // If a real bed exists, use it
                }

                // If no bed exists, create a temporary bedroll
                if (__instance.m_Bed == null && _tempBedroll == null)
                {
                    GearItem bedrollPrefab = GearItem.LoadGearItemPrefab("GEAR_BedRoll");

                    if (bedrollPrefab != null)
                    {
                        GameObject bedrollObject = GearItem.InstantiateDepletedGearPrefab(bedrollPrefab.gameObject);
                        GearItem tempBedroll = bedrollObject.GetComponent<GearItem>();

                        if (tempBedroll != null)
                        {
                            tempBedroll.m_CurrentHP = Mathf.Max(2f, tempBedroll.m_CurrentHP); // Assign 2% condition to ensure there's no warmth bonus from temporary bedroll
                            tempBedroll.m_WornOut = false; // Temporary bedroll is not worn out
                            tempBedroll.m_InPlayerInventory = false; // Do not place temporary bedroll in inventory
                            tempBedroll.gameObject.transform.position = GameManager.GetPlayerTransform().position;
                            tempBedroll.gameObject.SetActive(true);

                            Bed bedComponent = tempBedroll.GetComponent<Bed>();
                            if (bedComponent != null)
                            {
                                __instance.m_Bed = bedComponent;
                                _tempBedroll = tempBedroll;

                                bedComponent.m_OpenAudio = null; // Disable bedroll opening audio
                                bedComponent.m_CloseAudio = null; // Disable bedroll closing audio
                                bedComponent.SetState(BedRollState.Placed); // Set temporary bedroll as placed
                            }
                        }
                    }
                }
            }

            // Rest panel closed
            else if (!enable)
            {
                if (__instance.m_Bed != null && __instance.m_Bed != _tempBedroll?.GetComponent<Bed>()) // If a real bed exists
                {
                    if (!GameManager.GetRestComponent().IsSleeping()) // If player is not sleeping
                    {
                        __instance.m_Bed = null; // Reset bed reference when rest panel is closed
                    }
                }

                if (__instance.m_Bed != null && __instance.m_Bed == _tempBedroll?.GetComponent<Bed>()) // If the bed is temporary
                {
                    if (!GameManager.GetRestComponent().IsSleeping()) // If player is not sleeping
                    {
                        if (_tempBedroll != null)
                        {
                            GearManager.DestroyGearObject(_tempBedroll.gameObject); // Destroy temporary bedroll when rest panel is closed
                            _tempBedroll = null;

                            __instance.m_Bed = null; // Reset bed reference
                        }
                    }
                }
            }
        }
    }


    // Set the Pass Time UI tab as default when selecting Pass Time through the radial menu
    [HarmonyPatch(typeof(Panel_ActionsRadial), nameof(Panel_ActionsRadial.DoPassTime))]
    public static class SWaB_Radial_DoPassTime
    {
        public static void Postfix()
        {
            Panel_Rest restPanel = InterfaceManager.GetPanel<Panel_Rest>();

            if (restPanel != null)
            {
                restPanel.m_ShowPassTime = true; // Show Pass Time tab
                restPanel.m_ShowPassTimeOnly = false; // Enable the ability to switch to the Sleep tab

                // Force refresh of the UI layout
                restPanel.m_RestOnlyObject.SetActive(false);
                restPanel.m_PassTimeOnlyObject.SetActive(true);
            }
        }
    }


    // Disable pickup button and center sleep button when temporary bedroll is active
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.UpdateButtonLegend))]
    public class SWaB_UpdateButtonLegend
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (SWaB._tempBedroll != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>())
            {
                if (__instance.m_PickUpButton.activeSelf) 
                {
                    Utils.SetActive(__instance.m_PickUpButton, false); // Disable pickup button if bedroll is temporary

                    __instance.m_SleepButton.transform.localPosition = __instance.m_SleepButtonCenteredPos; // Center the sleep button
                }
            }
        }
    }


    // Apply cold exposure penalties when sleeping
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.OnRest))]
    public class SWaB_SleepExposure
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            var restComponent = GameManager.GetRestComponent();
            var freezingComponent = GameManager.GetFreezingComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var hypothermiaComponent = GameManager.GetHypothermiaComponent();

            if (__instance == null || restComponent  == null || freezingComponent == null || conditionComponent == null || hypothermiaComponent == null)
            {
                return;
            }

            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>() || __instance.m_Bed == null)
            {
                __instance.m_Bed = SWaB._tempBedroll?.GetComponent<Bed>();
            }

            // Default values for real beds
            restComponent.m_ReduceFatiguePerHourRest = 8.33f;
            freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = 6f;
            conditionComponent.m_HPDecreasePerDayFromFreezing = 450f;
            hypothermiaComponent.m_HPDrainPerHour = 40f;

            // If bed is temporary
            if (SWaB._tempBedroll != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>())
            {
                float exposurePenalty = ExposurePenalty.ApplyExposurePenalty();
                float freezingScale = 6f * Settings.settings.freezingScale;
                float freezingHealthLoss = 450f * Settings.settings.freezingHealthLoss + exposurePenalty;
                float hypothermicHealthLoss = 40f * Settings.settings.hypothermicHealthLoss + exposurePenalty;

                // Apply cold exposure effects
                restComponent.m_ReduceFatiguePerHourRest = 8.33f * Settings.settings.fatigueRecoveryPenalty;
                freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = freezingScale;
                conditionComponent.m_HPDecreasePerDayFromFreezing = freezingHealthLoss;
                hypothermiaComponent.m_HPDrainPerHour = hypothermicHealthLoss;
            }
        }
    }


    // Condition recovery
    [HarmonyPatch(typeof(Rest), nameof(Rest.UpdateWhenSleeping))]
    public class SWaB_ConditionRecovery
    {
        static void Postfix(Rest __instance)
        {
            if (__instance == null || __instance.m_Bed == null || GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            // Use these values for a real bed
            __instance.m_Bed.m_ConditionPercentGainPerHour = 1f;
            __instance.m_Bed.m_UinterruptedRestPercentGainPerHour = 1f;

            // If bed is temporary
            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>())
            {
                // Apply condition recovery penalty
                __instance.m_Bed.m_ConditionPercentGainPerHour = 1f * Settings.settings.cloneBedConditionGainPerHour;
                __instance.m_Bed.m_UinterruptedRestPercentGainPerHour = 1f * Settings.settings.cloneBedConditionGainPerHour;
            }
        }
    }
    

    // When sleep ends, destroy temporary bedroll if it exists and reset bed reference
    [HarmonyPatch(typeof(Rest), nameof(Rest.EndSleeping), new Type[] { typeof(bool) })]
    public class SWaB_EndSleeping
    {
        static void Postfix(Rest __instance, ref bool interrupted)
        {
            if (__instance == null)
            {
                return;
            }

            // Check if the temporary bedroll exists
            if (interrupted && SWaB._tempBedroll != null)
            {
                GearManager.DestroyGearObject(SWaB._tempBedroll.gameObject); // Destroy temporary bedroll if it does exist
                SWaB._tempBedroll = null;
            }

            __instance.m_Bed = null; // Reset bed reference
        }
    }


    // Apply cold exposure penalties when passing time
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.OnPassTime))]
    public class SWaB_PassTimeExposure
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            var freezingComponent = GameManager.GetFreezingComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var hypothermiaComponent = GameManager.GetHypothermiaComponent();

            if (__instance == null || freezingComponent == null || conditionComponent == null || hypothermiaComponent == null)
            {
                return;
            }

            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>() || __instance.m_Bed == null)
            {
                __instance.m_Bed = SWaB._tempBedroll?.GetComponent<Bed>();
            }


            // Default values for real beds
            freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = 6f;
            conditionComponent.m_HPDecreasePerDayFromFreezing = 450f;
            hypothermiaComponent.m_HPDrainPerHour = 40f;

            // If bed is temporary
            if (SWaB._tempBedroll != null && __instance.m_Bed == SWaB._tempBedroll?.GetComponent<Bed>())
            {
                float exposurePenalty = ExposurePenalty.ApplyExposurePenalty();
                float passTimeExposurePenalty = Settings.settings.passTimeExposurePenalty;
                float freezingScale = 6f * Settings.settings.freezingScale;
                float freezingHealthLoss = 450f * Settings.settings.freezingHealthLoss + exposurePenalty;
                float hypothermicHealthLoss = 40f * Settings.settings.hypothermicHealthLoss + exposurePenalty;

                // Apply cold exposure effects
                freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = freezingScale * passTimeExposurePenalty;
                conditionComponent.m_HPDecreasePerDayFromFreezing = freezingHealthLoss * passTimeExposurePenalty;
                hypothermiaComponent.m_HPDrainPerHour = hypothermicHealthLoss * passTimeExposurePenalty;
            }
        }
    }

    
    // Close rest panel when the player begins sleeping
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSleeping))]
    public class SWaB_RestPanelX
    {
        static void Postfix(PlayerManager __instance, ref bool __result)
        {
            if (__instance == null || GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            if (__result) // If player is sleeping
            {
                var restPanel = InterfaceManager.GetPanel<Panel_Rest>();
                if (restPanel != null && restPanel.IsEnabled()) // If rest panel is enabled
                {
                    restPanel.Enable(false); // Close the rest panel
                }
            }
        }
    }
}
