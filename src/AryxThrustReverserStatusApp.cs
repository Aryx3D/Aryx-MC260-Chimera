using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    public sealed class AryxThrustReverserStatusApp : HUDApp
    {
        [Tooltip("Shit to show when the reverser is available but not necessarily active")]
        [SerializeField]
        private Graphic[] baseElements;
        [Tooltip("Shit to show when the reverser is active")]
        [SerializeField]
        private Graphic[] reversersActiveElements;
        [SerializeField]
        private Graphic disarmedIndicator;

        [SerializeField]
        private float minThrottle;

        [SerializeField]
        private float maxAltitude;

        [SerializeField]
        private float minSpeedToArm;

        private Aircraft appAircraft;
        private bool shown;
        private bool reverserActive;
        private bool shouldShow;
        private bool shouldActivate;
        private bool armed;
        private ControlInputs inputs;

        public override void Initialize(Aircraft aircraft)
        {
            appAircraft = aircraft;
            inputs = aircraft.GetInputs();
            disarmedIndicator.enabled = true;
            for (int i = 0; i < baseElements.Length; i++)
            {
                baseElements[i].enabled = false;
            }
            for (int i = 0; i < reversersActiveElements.Length; i++)
            {
                reversersActiveElements[i].enabled = false;
            }
        }
        public override void Refresh()
        {
           armed = appAircraft.speed > minSpeedToArm;
           disarmedIndicator.enabled = !armed;


            shouldShow = armed && appAircraft.gearDeployed && appAircraft.radarAlt < maxAltitude;
            shouldActivate = shouldShow && inputs.brake > 0.5 && inputs.throttle > minThrottle;

            State(shouldShow);
            ReverserActive(shouldActivate);
        }
        private void State(bool show)
        {
            if (shown == show)
                return;

            shown = show;

            for (int i = 0; i < baseElements.Length; i++)
            {
                baseElements[i].enabled = shown;
            }
        }

        private void ReverserActive(bool active)
        {
            if (reverserActive == active)
                return;

            reverserActive = active;

            for (int i = 0; i < reversersActiveElements.Length; i++)
            {
                reversersActiveElements[i].enabled = reverserActive;
            }
        }
    }
}
