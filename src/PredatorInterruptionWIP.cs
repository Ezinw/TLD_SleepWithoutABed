using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SleepWithoutABed
{
    // It seems that predator attacks during rest are bugged. Sometimes the camera will act strangely or break keeping the screen black (faded out) or fade in and out throughout a struggle
    // This is an attempt to make struggle camera issues less frequent but there may still be issues with the struggle camera animations
    // WIP:
    [HarmonyPatch(typeof(Rest), nameof(Rest.ShouldInterruptWithPredator))]
    public class SWaB_PredatorInterruption
    {
        // Track the current predator engaged in an attack
        public static BaseAi? activePredator = null;

        static void Postfix(Rest __instance, ref bool __result)
        {
            if (__instance == null)
                return;

            if (!__result)
                return;

            if (GameManager.GetVpFPSCamera() == null)
            {
                __result = false;
                return;
            }

            // Prevent multiple predators attacking simultaneously
            if (activePredator != null && activePredator.GetAiMode() == AiMode.Attack)
            {
                __result = false;
                return;
            }

            SpawnRegionManager? spawnManager = GameManager.GetSpawnRegionManager();
            if (spawnManager == null)
            {
                __result = false; // No spawn manager, so no predators to check
                return;
            }

            // Get the closest wolf 
            GameObject? closestWolf = GameManager.GetSpawnRegionManager()?.GetClosestActiveSpawn(GameManager.GetPlayerTransform().position, __instance.m_WolfPrefab?.name ?? "");

            // Get the closest bear
            GameObject? closestBear = GameManager.GetSpawnRegionManager()?.GetClosestActiveSpawn(GameManager.GetPlayerTransform().position, __instance.m_BearPrefab?.name ?? "");

            // Determine which predator is closer
            GameObject? predator = null;
            float playerDistance = Mathf.Infinity;

            if (closestWolf != null)
            {
                float wolfDistance = Vector3.Distance(GameManager.GetPlayerTransform().position, closestWolf.transform.position);
                if (wolfDistance < playerDistance)
                {
                    predator = closestWolf;
                    playerDistance = wolfDistance;
                }
            }

            if (closestBear != null)
            {
                float bearDistance = Vector3.Distance(GameManager.GetPlayerTransform().position, closestBear.transform.position);
                if (bearDistance < playerDistance)
                {
                    predator = closestBear;
                    playerDistance = bearDistance;
                }
            }

            if (predator == null)
            {
                __result = false;
                return;
            }

            BaseAi? ai = predator.GetComponent<BaseAi>();
            if (ai == null)
            {
                __result = false;
                return;
            }

            AiTarget? playerIsTarget = GameManager.GetPlayerObject()?.GetComponent<AiTarget>();
            if (playerIsTarget == null)
            {
                __result = false;
                return;
            }

            if (__result && ai.m_CurrentTarget != playerIsTarget)
            {
                ai.m_CurrentTarget = playerIsTarget;
            }

            // If AI cannot pathfind to the player, stop
            if (!ai.CanPathfindToPosition(GameManager.GetPlayerTransform().position))
            {
                __result = false;
                return;
            }

            // Set a timer to make sure AI is in attack mode before a struggle occurs
            float startTime = Time.realtimeSinceStartup;
            float timeout = 0.1f;
            while (Time.realtimeSinceStartup - startTime < timeout && ai.GetAiMode() != AiMode.Attack)
            {
                Thread.Sleep(1);
            }

            if (ai.GetAiMode() != AiMode.Attack)
            {
                Vector3 attackPosition = GameManager.GetPlayerTransform().position;
                bool attackStarted = ai.EnterAttackModeIfPossible(attackPosition, true);

                if (attackStarted && __result)
                {
                    ai.SetAiMode(AiMode.Attack);
                    __result = true;
                }

                if (ai.Bear)
                {
                    ai.SuppressAttackStartAnimation();
                    ai.AnimSetTrigger(ai.m_AnimParameter_Roar_Trigger);
                }
                ai.SuppressAttackStartAnimation();
            }
        }
    }


    // Reset activePredator when the struggle ends
    [HarmonyPatch(typeof(PlayerStruggle), nameof(PlayerStruggle.GetUpAnimationComplete))]
    public class SWaB_ResetActivePredator
    {
        static void Postfix(PlayerStruggle __instance)
        {
            SWaB_PredatorInterruption.activePredator = null; // Reset active predator
        }
    }
}