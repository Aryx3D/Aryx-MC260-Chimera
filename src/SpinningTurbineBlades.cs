using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Aryx_MC260_Chimera
{
    public class SpinningTurbineBlades : MonoBehaviour
    {
        [SerializeField] private Transform bladeObject;
        [SerializeField] private Turbofan engine;

        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        [SerializeField] private float rpmMultiplier = 1f;
        [SerializeField] private bool invertRotation = false;

        private float currentRotationSpeed;

        void Update()
        {
            if (engine == null || bladeObject == null)
                return;
            float rpmRatio = engine.GetRPMRatio();
            float targetSpeed = rpmRatio * 360f * 8f * rpmMultiplier;

            if (invertRotation)
                targetSpeed = -targetSpeed;

            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetSpeed, 8f * Time.deltaTime);

            bladeObject.Rotate(rotationAxis * currentRotationSpeed * Time.deltaTime, Space.Self);
        }
        public float GetCurrentRPMRatio() => engine != null ? engine.GetRPMRatio() : 0f;
    }
}
