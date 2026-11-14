using UnityEngine;

namespace Aryx_MC260_Chimera
{
    [DefaultExecutionOrder(100)]
    public sealed class AryxThrustReverser : MonoBehaviour
    {
        [SerializeField]
        private Turbofan turbofan;

        [SerializeField]
        private AeroPart part;

        [SerializeField]
        private Transform forceTransform;

        [SerializeField]
        private AryxThrustReverserMovingPart[] movingParts;

        [Range(0f, 1f)]
        [SerializeField]
        private float brakeThreshold = 0.5f;

        [Range(0f, 1f)]
        [SerializeField]
        private float minimumThrottle = 0.05f;

        [SerializeField]
        private float minimumArmingSpeed = 5f;

        [SerializeField]
        private float maximumRadarAltitude = 0.5f;

        [SerializeField]
        private bool requireGearDown = true;

        [Range(0f, 1f)]
        [SerializeField]
        private float redirectedThrustFraction = 0.8f;

        [Range(0f, 1f)]
        [SerializeField]
        private float reverseEfficiency = 0.75f;

        [Header("Movement")]
        [SerializeField]
        private float deploymentRate = 2f;

        [SerializeField]
        private float retractionRate = 3f;

        private Aircraft aircraft;
        private ControlInputs inputs;

        private float position;
        private bool reverserArmed;
        private bool initialized;
        private Rigidbody forceBody;

        public float Position => position;
        public bool IsDeployed => position > 0.99f;
        public bool IsArmed => reverserArmed;

        private void Start()
        {
            TryInitialize();
        }

        private bool TryInitialize()
        {
            if (initialized)
            {
                return true;
            }
            //utterly and bombastically defensive because I am sick to fucking death of nullreferenceexceptions in this Godforsaken game.
            if (turbofan == null)
            {
                Debug.LogError($"{nameof(AryxThrustReverser)} on '{name}' has no Turbofan.", this);
                enabled = false;
                return false;
            }
            if (part == null)
            {
                Debug.LogError($"{nameof(AryxThrustReverser)} on '{name}' has no AeroPart.", this);
                enabled = false;
                return false;
            }

            aircraft = turbofan.GetUnit() as Aircraft;

            if (aircraft == null)
            {
                return false;
            }

            inputs = aircraft.GetInputs();

            if (forceTransform == null)
            {
                forceTransform = turbofan.transform;
            }

            if (forceBody == null)
            {
                forceBody = part.rb;
            }

            if (forceBody == null)
            {
                Debug.LogError($"{nameof(AryxThrustReverser)} on '{name}' could not find rb.", this);
                enabled = false;
                return false;
            }

            position = 0f;
            reverserArmed = false;

            ApplyPosition(0f);

            initialized = true;
            return true;
        }

        private void FixedUpdate()
        {
            if (!TryInitialize())
            {
                return;
            }

            UpdateArming();

            float targetPosition = reverserArmed ? 1f : 0f;
            float rate = targetPosition > position ? deploymentRate : retractionRate;

            position = Mathf.MoveTowards(position, targetPosition, Mathf.Max(0f, rate) * Time.fixedDeltaTime);

            ApplyPosition(position);

            if (!aircraft.LocalSim || position <= 0.001f)
            {
                return;
            }

            ApplyReverseThrust();
        }

        private void UpdateArming()
        {
            bool brakeHeld = inputs.brake >= brakeThreshold;
            bool throttleApplied = inputs.throttle > minimumThrottle;
            bool gearSafe = !requireGearDown || aircraft.gearDeployed;
            bool altitudeSafe = maximumRadarAltitude < 0f || aircraft.radarAlt <= maximumRadarAltitude;

            bool controlsRequestReverse = brakeHeld && throttleApplied && gearSafe && altitudeSafe && aircraft.speed > 0;

            if (!controlsRequestReverse)
            {
                reverserArmed = false;
                return;
            }

            if (reverserArmed)
            {
                return;
            }

            if (aircraft.speed >= minimumArmingSpeed)
            {
                reverserArmed = true;
            }
        }

        private void ApplyReverseThrust()
        {
            float engineThrust = Mathf.Max(0f, turbofan.GetThrust());

            if (engineThrust <= 0f)
            {
                return;
            }

            float redirectedFraction = Mathf.Clamp01(redirectedThrustFraction) * position;
            float divertedThrust = engineThrust * redirectedFraction;
            float correctiveForce = divertedThrust * (1f + Mathf.Clamp01(reverseEfficiency));

            Vector3 reverseForce = -forceTransform.forward * correctiveForce;

            forceBody.AddForceAtPosition(reverseForce, forceTransform.position, ForceMode.Force);
        }

        private void ApplyPosition(float deployedAmount)
        {
            deployedAmount = Mathf.Clamp01(deployedAmount);

            if (movingParts == null)
            {
                return;
            }

            for (int i = 0; i < movingParts.Length; i++)
            {
                if (movingParts[i] != null)
                {
                    movingParts[i].Animate(deployedAmount);
                }
            }
        }

        public AryxThrustReverserMovingPart GetMovingPart(int index)
        {
            if (movingParts == null || index < 0 || index >= movingParts.Length)
            {
                return null;
            }

            return movingParts[index];
        }
    }
}