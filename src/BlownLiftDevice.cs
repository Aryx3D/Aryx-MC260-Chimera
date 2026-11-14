using Rewired;
using UnityEngine;

namespace Aryx_MC260_Chimera
{
    public class BlownLiftDevice : MonoBehaviour
    {
        [SerializeField]
        private string partName;

        [Header("Aerodynamics")]
        [SerializeField]
        private AeroPart aeroPart;

        [SerializeField]
        private AeroPart[] criticalParts;

        [SerializeField]
        private SwingWingController swingWingController;

        [SerializeField]
        private float speedDeployed;

        [SerializeField]
        private float speedRetracted;

        [SerializeField]
        private float alphaMin;

        [SerializeField]
        private float alphaFactor;

        [SerializeField]
        private float deployedArea;

        [Header("Moving Parts")]
        [SerializeField]
        private BlownLiftMovingPart[] movingParts;

        [Tooltip("Fraction of full travel moved per second.")]
        [SerializeField]
        private float deploymentRate = 1f;

        [Header("Custom Axis 1")]
        [SerializeField]
        private float axisRemapLower;

        [SerializeField]
        private float axisRemapUpper = 1f;

        [Tooltip("Manual mode returns to auto after this long without a meaningful Custom Axis 1 change.")]
        [SerializeField]
        private float autoReturnDelay = 30f;

        [Tooltip("minimum customaxis1 delta to reset the return delay timer")]
        [SerializeField]
        private float axisChangeEpsilon = 0.002f;

        [SerializeField]
        private bool showHUDStatus = true;

        private BlownLiftMode mode = BlownLiftMode.Auto;
        private Aircraft aircraft;
        private ControlInputs inputs;
        private Player player;

        private float partAreaDeployed;
        private float partAreaRetracted;
        private float position;
        private float customAxis1Prev;
        private float lastModeToggleTime;
        private float lastManualAxisChangeTime;
        private bool initialized;
        private BlownWingController controller;
        private bool isFunctional;
        public float Position => position;
        public BlownLiftMode Mode => mode;
        public bool IsManual => mode == BlownLiftMode.Manual;

        private void Start()
        {
            TryInitialize();
        }

        public void SetController(BlownWingController Controller)
        {
            controller = Controller;
        }
        private bool TryInitialize()
        {
            if (initialized)
            {
                return true;
            }

            if (aeroPart == null)
            {
                Debug.LogError($"{nameof(BlownLiftDevice)} on '{name}' has no AeroPart assigned.", this);
                enabled = false;
                return false;
            }

            aircraft = aeroPart.parentUnit as Aircraft;
            if (aircraft == null)
            {
                return false;
            }

            inputs = aircraft.GetInputs();
            player = ReInput.players.GetPlayer(0);

            partAreaRetracted = aeroPart.GetWingArea();
            partAreaDeployed = partAreaRetracted + deployedArea;

            mode = BlownLiftMode.Auto;
            position = 0f;
            customAxis1Prev = inputs.customAxis1;
            lastModeToggleTime = Time.timeSinceLevelLoad;
            lastManualAxisChangeTime = lastModeToggleTime;

            ApplyPosition(position);

            foreach (var part in criticalParts)
            {
                part.onPartDetached += CriticalPartDetached;
            }

            isFunctional = true;
            initialized = true;
            return true;
        }

        private void FixedUpdate()
        {
            if (!TryInitialize())
            {
                return;
            }

            if (!isFunctional)
                return;

            CheckForManualInput();

            float targetPosition;
            if (mode == BlownLiftMode.Auto && aircraft.LocalSim)
            {
                targetPosition = CalculateAutomaticPosition();
                inputs.customAxis1 = PositionToAxis(targetPosition);
                customAxis1Prev = inputs.customAxis1;
            }
            else
            {
                targetPosition = AxisToPosition(inputs.customAxis1);
            }

            float newPosition = Mathf.MoveTowards(position,targetPosition, Mathf.Max(0f, deploymentRate) * Time.fixedDeltaTime);

            if (Mathf.Abs(newPosition - position) > 0.001f)
            {
                position = newPosition;
                ApplyPosition(position);
            }
        }

        private void CheckForManualInput()
        {
            if (aircraft.Player == null)
            {
                if (mode != BlownLiftMode.Auto)
                {
                    SetMode(BlownLiftMode.Auto);
                }

                return;
            }

            if (!GameManager.IsLocalAircraft(aircraft) || player == null)
            {
                return;
            }

            float rawAxis = Mathf.Clamp(player.GetAxisRaw("Custom Axis 1"), -1f, 1f);
            float previousRawAxis = Mathf.Clamp(player.GetAxisRawPrev("Custom Axis 1"), -1f, 1f);
            float rawDelta = Mathf.Abs(rawAxis - previousRawAxis);

            bool modifiedThrottleAxis = player.GetButton("Axis Modifier") && player.GetAxisRaw("Throttle") != 0f;

            bool deliberateAxisInput = (rawDelta > 0f && rawDelta < 0.5f) || Mathf.Abs(rawAxis) > 0.5f || modifiedThrottleAxis;

            float axis = inputs.customAxis1;
            bool axisChanged = Mathf.Abs(axis - customAxis1Prev) > axisChangeEpsilon;
            customAxis1Prev = axis;

            float now = Time.timeSinceLevelLoad;

            if (mode == BlownLiftMode.Auto)
            {
                if (now - lastModeToggleTime >= 1f && deliberateAxisInput && axisChanged)
                {
                    SetMode(BlownLiftMode.Manual);
                }

                return;
            }

            if (axisChanged)
            {
                lastManualAxisChangeTime = now;
            }

            bool toggleDelayElapsed = now - lastModeToggleTime >= autoReturnDelay;
            bool axisUnchangedLongEnough = now - lastManualAxisChangeTime >= autoReturnDelay;
            bool deploySpeedGateMet = aircraft.speed <= speedDeployed;

            if (toggleDelayElapsed && axisUnchangedLongEnough && deploySpeedGateMet)
            {
                SetMode(BlownLiftMode.Auto);
            }
        }

        private void SetMode(BlownLiftMode newMode)
        {
            if (mode == newMode)
            {
                return;
            }

            mode = newMode;
            float now = Time.timeSinceLevelLoad;
            lastModeToggleTime = now;

            if (newMode == BlownLiftMode.Manual)
            {
                lastManualAxisChangeTime = now;
                ReportManual();
                return;
            }

            float automaticPosition = CalculateAutomaticPosition();
            inputs.customAxis1 = PositionToAxis(automaticPosition);
            customAxis1Prev = inputs.customAxis1;
            ReportAuto();
        }

        private void ReportManual()
        {
            if (!showHUDStatus || controller == null || !GameManager.IsLocalAircraft(aircraft))
            {
                return;
            }
            controller.SetMode(BlownLiftMode.Manual);
        }
        private void ReportAuto()
        {
            if (!showHUDStatus || controller == null || !GameManager.IsLocalAircraft(aircraft))
            {
                return;
            }
            controller.SetMode(BlownLiftMode.Auto);
        }

        private void CriticalPartDetached(UnitPart part)
        {
            if (!isFunctional)
                return;

            isFunctional = false;
            ApplyPosition(0);

            foreach (var critPart in criticalParts)
            {
                critPart.onPartDetached -= CriticalPartDetached;
            }
        }
        private float CalculateAutomaticPosition()
        {
            float speedRange = speedRetracted - speedDeployed;
            float target;

            if (Mathf.Abs(speedRange) < 0.001f)
            {
                target = aircraft.speed <= speedDeployed ? 1f : 0f;
            }
            else
            {
                target = Mathf.Clamp01(1f - Mathf.Max(aircraft.speed - speedDeployed, 0f) / speedRange);
            }

            if (swingWingController != null && swingWingController.GetSwingPosition() > 0f)
            {
                target = 0f;
            }

            if (alphaFactor > 0f)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(aircraft.rb.velocity);
                float alpha = Mathf.Atan2(localVelocity.y, localVelocity.z) * -Mathf.Rad2Deg;
                target += Mathf.Clamp01(Mathf.Abs(alpha) - alphaMin) * alphaFactor;
            }

            return Mathf.Clamp01(target);
        }

        private float AxisToPosition(float axis)
        {
            if (Mathf.Abs(axisRemapUpper - axisRemapLower) < 0.001f)
            {
                return axis >= axisRemapUpper ? 1f : 0f;
            }

            return Mathf.InverseLerp(axisRemapLower, axisRemapUpper, axis);
        }

        private float PositionToAxis(float deployedAmount)
        {
            return Mathf.Lerp(axisRemapLower, axisRemapUpper, Mathf.Clamp01(deployedAmount));
        }

        private void ApplyPosition(float deployedAmount)
        {
            deployedAmount = Mathf.Clamp01(deployedAmount);
            aeroPart.SetWingArea(Mathf.Lerp(partAreaRetracted, partAreaDeployed, deployedAmount));

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

        public BlownLiftMovingPart GetMovingPart(int index)
        {
            if (movingParts == null || index < 0 || index >= movingParts.Length)
            {
                return null;
            }

            return movingParts[index];
        }

        public bool TryGetMovingPartTransform(int index, out Transform movingTransform)
        {
            BlownLiftMovingPart movingPart = GetMovingPart(index);
            movingTransform = movingPart != null ? movingPart.AnimatedTransform : null;
            return movingTransform != null;
        }
    }
}