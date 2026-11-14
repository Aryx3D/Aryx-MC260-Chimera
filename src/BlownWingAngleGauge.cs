using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Aryx_MC260_Chimera
{
    public class BlownWingGauge : HUDApp
    {
        [Header("Wing Vis")]
        [SerializeField] private Transform flapPivot;           // Main pivot for flap rotation
        [SerializeField] private TMP_Text angleText;
        [SerializeField] private float angleOffset = 0f;

        [Header("Intensity")]
        [SerializeField] private Transform intensityArrow;
        [SerializeField] private float intensityScaleMin = 0.2f;
        [SerializeField] private float intensityScaleMax = 1.0f;
        [SerializeField] private TMP_Text intensityText;

        [Header("Warning")]
        [SerializeField] private TMP_Text asymmetricWarning;
        [SerializeField] private Color warningColor = Color.red;

        private BlownWingController blownWing;
        private float lastAngle;

        public override void Initialize(Aircraft aircraft)
        {
            blownWing = aircraft.gameObject.GetComponentInChildren<BlownWingController>(true);

            if (blownWing == null)
            {
                Debug.LogError("BlownWingGauge: BlownWingController not found on aircraft!");
                return;
            }

            if (asymmetricWarning != null)
                asymmetricWarning.gameObject.SetActive(true);
        }

        public override void Refresh()
        {
            if (blownWing == null) return;

            float rawAngle = blownWing.Angle;

            float displayAngle = Mathf.Clamp(rawAngle, 0f, 90f);

            flapPivot.localEulerAngles = new Vector3(0f, 0f, rawAngle);
            
            float scale = Mathf.Lerp(intensityScaleMin, intensityScaleMax, blownWing.Intensity);
            intensityArrow.localScale = new Vector3(scale, 1f, 1f);

            float angle = rawAngle % 360f;
            if (angle < 0f) angle += 360f;
            angle = (angle + angleOffset) % 360f;
            if (angle < 0f) angle += 360f;
            if (angle > 180f)
                angle -= 360f;
            angleText.text = $"{angle:F1}°"; //uuuuuuuuuuuuuuooooooooooooooooooooooohhhhhhhhhhhhhhhhhhhhhhhhhhhhhh

            intensityText.text = $"{(blownWing.Intensity * 100f):F1}%";

            asymmetricWarning.gameObject.SetActive(blownWing.Asymmetric);
            if (blownWing.Asymmetric)
                asymmetricWarning.color = warningColor;

            lastAngle = rawAngle;
        }
    }
}
