using NuclearOption.UIStyleSystem;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    public class AryxCountermeasureStatusApp : HUDApp
    {
        [SerializeField]
        private Image counterImage;

        [SerializeField]
        private Text counterName;

        [SerializeField]
        private Text counterAmmo;

        private Aircraft aircraft;

        public override void Initialize(Aircraft aircraft)
        {
            this.aircraft = aircraft;
            if (aircraft.countermeasureManager.GetActiveCountermeasure() == null)
            {
                base.gameObject.SetActive(value: false);
            }
        }

        public override void RefreshSettings()
        {
            base.RefreshSettings();

            counterName.fontSize = (int)((float)fontSize * fontSizeMultiplier);
            counterAmmo.fontSize = (int)((float)fontSize * fontSizeMultiplier);
            Show(true);
            Refresh();
        }

        public override void Refresh()
        {
            if (aircraft == null)
            {
                return;
            }
            if (aircraft.countermeasureManager.GetActiveCountermeasure() != null)
            {
                counterImage.enabled = true;
                counterImage.color = ThemeManager.Active.ColorTheme.AllClear;
                counterName.color = ThemeManager.Active.ColorTheme.AllClear;
                counterAmmo.color = ThemeManager.Active.ColorTheme.AllClear;
                Countermeasure activeCountermeasure = aircraft.countermeasureManager.GetActiveCountermeasure();
                if (activeCountermeasure is FlareEjector flareEjector)
                {
                    if (counterName.text != "FLARE")
                    {
                        if (!counterImage.enabled)
                        {
                            counterImage.enabled = true;
                        }
                        counterImage.sprite = flareEjector.displayImage;
                        counterName.text = "FLARE";
                    }
                    int ammo = flareEjector.GetAmmo();
                    counterAmmo.text = $"{ammo}";
                    if ((float)ammo == 0f)
                    {
                        counterImage.color = ThemeManager.Active.ColorTheme.Alert;
                        counterName.color = ThemeManager.Active.ColorTheme.Alert;
                        counterAmmo.color = ThemeManager.Active.ColorTheme.Alert;
                    }
                }
                else
                {
                    if (counterName.text != activeCountermeasure.displayName)
                    {
                        counterImage.sprite = activeCountermeasure.displayImage;
                        counterName.text = activeCountermeasure.displayName;
                    }
                    float charge = aircraft.GetPowerSupply().GetCharge();
                    counterAmmo.text = $"{100f * charge:F0}%";
                    if (charge < 0.5f)
                    {
                        counterImage.color = ThemeManager.Active.ColorTheme.Alert;
                        counterName.color = ThemeManager.Active.ColorTheme.Alert;
                        counterAmmo.color = ThemeManager.Active.ColorTheme.Alert;
                    }
                }
            }
            else
            {
                counterImage.enabled = false;
                counterName.text = "";
                counterAmmo.text = "";
            }
        }

        public void Show(bool arg)
        {
            counterImage.enabled = arg;
            counterName.enabled = arg;
            counterAmmo.enabled = arg;
        }
    }

}
