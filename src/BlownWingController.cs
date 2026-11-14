using UnityEngine;
using static Aryx_MC260_Chimera.BlownLiftDevice;

namespace Aryx_MC260_Chimera
{
    //dumb bullshit manual flaps emord asked for with pleading eyes so it was mandatory.
    public class BlownWingController : MonoBehaviour
    {
        [Header("References")]
        public BlownLiftDevice[] BlownLiftDevices;

        public Aircraft aircraft;

        [Tooltip("Moving-part index inside the first BlownLiftDevice.")]
        public int HLDIndex;

        [Header("Asymmetry")]
        public AeroPart[] LeftLiftProviders;
        public AeroPart[] RightLiftProviders;

        [Header("Blown lift intensity")]
        public Turbofan[] Engines;

        [Tooltip("Current flap deflection angle in degrees.")]
        public float Angle;

        [Tooltip("0-1 blown-wing intensity (average engine thrust ratio).")]
        public float Intensity;

        public bool Asymmetric;

        private Transform cachedFlapTransform;
        private BlownLiftMode mode = BlownLiftMode.Auto;
        private void Awake()
        {
            if (aircraft == null)
            {
                aircraft = GetComponentInParent<Aircraft>();
            }

            if (BlownLiftDevices != null)
            {
                for (int i = 0; i < BlownLiftDevices.Length; i++)
                {
                    if (BlownLiftDevices[i] != null)
                    {
                        BlownLiftDevices[i].SetController(this);
                    }
                }
            }

            CacheFlapTransform();
        }

        private void Start()
        {
            if (cachedFlapTransform == null)
            {
                CacheFlapTransform();
            }
        }

        private void CacheFlapTransform()
        {
            cachedFlapTransform = null;

            if (BlownLiftDevices == null || BlownLiftDevices.Length == 0 || HLDIndex < 0)
            {
                return;
            }

            BlownLiftDevice device = BlownLiftDevices[0];
            if (device != null)
            {
                device.TryGetMovingPartTransform(HLDIndex, out cachedFlapTransform);
            }
        }

        private void FixedUpdate()
        {
            UpdateIntensity();
            UpdateAngle();
            UpdateAsymmetry();
        }

        private void UpdateIntensity()
        {
            if (Engines == null || Engines.Length == 0)
            {
                Intensity = 0f;
                return;
            }

            float sum = 0f;
            int validCount = 0;

            for (int i = 0; i < Engines.Length; i++)
            {
                Turbofan engine = Engines[i];
                if (engine == null)
                {
                    continue;
                }

                sum += engine.GetThrustRatio();
                validCount++;
            }

            Intensity = validCount > 0 ? sum / validCount : 0f;
        }

        private void UpdateAngle()
        {
            if (cachedFlapTransform == null)
            {
                CacheFlapTransform();
            }

            Angle = cachedFlapTransform != null
                ? cachedFlapTransform.localEulerAngles.x
                : 0f;
        }

        private void UpdateAsymmetry()
        {
            Asymmetric = false;

            if (LeftLiftProviders == null || RightLiftProviders == null)
            {
                return;
            }

            int count = Mathf.Min(LeftLiftProviders.Length, RightLiftProviders.Length);
            for (int i = 0; i < count; i++)
            {
                AeroPart left = LeftLiftProviders[i];
                AeroPart right = RightLiftProviders[i];

                if (left == null || right == null)
                {
                    continue;
                }

                if (left.IsDetached() != right.IsDetached())
                {
                    Asymmetric = true;
                    return;
                }
            }
        }
        public void SetMode(BlownLiftMode newMode)
        {
            if (mode == newMode)
            {
                return;
            }

            mode = newMode;

            if (mode == BlownLiftMode.Manual)
            {
                ReportManual();
            }
            else
            {
                ReportAuto();
            }
        }
        private void ReportManual()
        {
            if (!GameManager.IsLocalAircraft(aircraft))
            {
                return;
            }

            AircraftActionsReport report = SceneSingleton<AircraftActionsReport>.i;
            if (report != null)
            {
                report.ReportText("Blown lift set to Manual", 4f);
            }
        }
        private void ReportAuto()
        {
            if (!GameManager.IsLocalAircraft(aircraft))
            {
                return;
            }

            AircraftActionsReport report = SceneSingleton<AircraftActionsReport>.i;
            if (report != null)
            {
                report.ReportText("Blown lift set to Auto", 4f);
            }
        }
    }
    public enum BlownLiftMode
    {
        Manual,
        Auto
    }
}