using Il2Cpp;
using HarmonyLib;
using System.Runtime.InteropServices;

namespace SleepWithoutABed
{
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.Enable), new Type[] { typeof(bool), typeof(bool) })]
    class EnableSleep
    {
        static void Prefix(Panel_Rest __instance, ref bool enable, ref bool passTimeOnly)
        {
            bool inVehicle = GameManager.GetPlayerInVehicle().IsInside();
            bool inSnowShelter = GameManager.GetSnowShelterManager().PlayerInShelter();
            Rest sleep = GameManager.GetRestComponent();
            Bed bed = __instance.m_Bed;

            if (enable && bed != null)
            {
                sleep.m_ReduceFatiguePerHourRest = 8.333333333333333f;
            }

            if (enable && bed == null && !(inVehicle || inSnowShelter))
            {
                passTimeOnly = false;

                switch (Settings.settings.sleepPenalty)
                {
                    case Settings.Choice.Default:
                        sleep.m_ReduceFatiguePerHourRest = 8.333333333333333f;
                        break;
                    case Settings.Choice.ThreeQuarters:
                        sleep.m_ReduceFatiguePerHourRest = 6.25f;
                        break;
                    case Settings.Choice.Half:
                        sleep.m_ReduceFatiguePerHourRest = 4.166666666666667f;
                        break;
                    case Settings.Choice.Quarter:
                        sleep.m_ReduceFatiguePerHourRest = 2.083333333333333f;
                        break;
                    case Settings.Choice.Eighth:
                         sleep.m_ReduceFatiguePerHourRest = 1.041666666666667f;
                         break;
                }
                
            }

            else if (!enable && bed != null)
            {
                __instance.m_Bed = null;
            }

        }

    }

    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSleeping))]
    class RestPanelX
    {
        static void Postfix(PlayerManager __instance, ref bool __result)
        {
            if (__result)
            {
                CloseRestPanel();
            }

            else if (!__result)
            {
                return;
            }

        }

        
        //Simulate Escape Key Press.
        //Closes the rest panel after clicking the sleep button if accessed through the pass time radial option.
        public static void CloseRestPanel()
        {
            const ushort VK_ESCAPE = 0x1B;
            INPUT[] inputs = new INPUT[2];

            // Press Escape key
            inputs[0] = new INPUT();
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = VK_ESCAPE;

            // Release Escape key
            inputs[1] = new INPUT();
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = VK_ESCAPE;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

    }

}

