using NuclearOption.UIStyleSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    public class AryxWeaponStationApp : HUDApp
    {
        private enum DisplayState
        {
            Invalid,
            Empty,
            Loaded,
            Selected
        }
        [Header("Hardpoint Sets")]
        [Tooltip("Hardpoint-set indices represented by this entry. Include both indices for a symmetric pair.")]
        [SerializeField]
        private int[] hardpointSetIndices;

        [Header("Text")]
        [SerializeField]
        private TMP_Text weaponNameText;

        [SerializeField]
        private TMP_Text weaponAmmoCountText;

        [Tooltip("Show loaded / full capacity for magazine-fed weapons. Otherwise show total remaining ammo.")]
        [SerializeField]
        private bool showMagazineReadout;

        [Header("State Colours")]
        [SerializeField]
        private Graphic[] colourTargets;

        [SerializeField]
        private Graphic[] colourTargetsNoAlpha;

        [Tooltip("Also apply the state colour to the weapon name and ammo text.")]
        [SerializeField]
        private bool colourTexts = true;

        [SerializeField]
        private Color loadedColor = Color.green;

        [SerializeField]
        private Color selectedColor = Color.yellow;

        [SerializeField]
        private Color emptyColor = Color.red;

        [SerializeField]
        private Color unavailableColor = new Color(1f, 1f, 1f, 0.2f);

        [Header("Selection Behaviour")]
        [SerializeField]
        private bool highlightNextHardpointGroupOnly = true;

        private Aircraft aircraft;
        private WeaponManager weaponManager;

        private readonly HashSet<Hardpoint> trackedHardpoints = new HashSet<Hardpoint>();
        private readonly List<Weapon> trackedWeapons = new List<Weapon>();
        private readonly List<WeaponStation> trackedStations = new List<WeaponStation>();

        private bool hardpointGroupValid;
        private string hardpointGroupWeaponName = string.Empty;

        private string displayedWeaponName;
        private string displayedAmmo;
        private AryxAmmoReadoutCache ammoCache;
        private int displayedState = int.MinValue;

        private static readonly FieldInfo WeaponHardpointField = typeof(Weapon).GetField("hardpoint", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo StationWeaponIndexField = typeof(WeaponStation).GetField("weaponIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Func<Weapon, Hardpoint> WeaponHardpoint = WeaponHardpointField != null ? AryxFieldAccess.CreateGetter<Weapon, Hardpoint>(WeaponHardpointField) : null;
        private static readonly Func<WeaponStation, int> StationWeaponIndex = StationWeaponIndexField != null ? AryxFieldAccess.CreateGetter<WeaponStation, int>(StationWeaponIndexField) : null;

        public override void Initialize(Aircraft aircraft)
        {
            RefreshSettings();
            Unsubscribe();

            this.aircraft = aircraft;
            weaponManager = aircraft != null ? aircraft.weaponManager : null;

            if (weaponManager != null)
            {
                weaponManager.OnWeaponsLoaded += WeaponsLoaded;
            }

            RebuildTracking();
            Refresh();
        }

        public override void Refresh()
        {
            string weaponName;
            string ammoReadout;
            DisplayState state;

            bool valid = TryReadHardpointGroup(out weaponName, out ammoReadout, out state);

            if (!valid)
            {
                weaponName = string.Empty;
                ammoReadout = string.Empty;
                state = DisplayState.Invalid;
            }

            ApplyReadout(weaponName, ammoReadout, state);
        }

        public override void RefreshSettings()
        {
            base.RefreshSettings();

            float appliedFontSize = fontSize * fontSizeMultiplier;
            ApplyTextSettings(weaponNameText, appliedFontSize);
            ApplyTextSettings(weaponAmmoCountText, appliedFontSize);

            loadedColor = ThemeManager.Active.ColorTheme.AllClear;
            selectedColor = ThemeManager.Active.ColorTheme.Warning;
            emptyColor = ThemeManager.Active.ColorTheme.Alert;

            displayedState = int.MinValue;
        }

        private bool TryReadHardpointGroup(out string weaponName, out string ammoReadout, out DisplayState state)
        {
            weaponName = hardpointGroupWeaponName;
            ammoReadout = string.Empty;
            state = DisplayState.Invalid;

            if (!hardpointGroupValid)
            {
                return false;
            }

            int loaded = 0;
            int total = 0;
            int full = 0;

            for (int i = 0; i < trackedWeapons.Count; i++)
            {
                Weapon weapon = trackedWeapons[i];
                if (weapon == null)
                {
                    continue;
                }

                total += Mathf.Max(weapon.GetAmmoTotal(), 0);
                if (showMagazineReadout)
                {
                    loaded += Mathf.Max(weapon.GetAmmoLoaded(), 0);
                    full += Mathf.Max(weapon.GetFullAmmo(), 0);
                }
            }

            ammoReadout = ammoCache.Get(showMagazineReadout ? loaded : total, full, showMagazineReadout);

            if (total <= 0)
            {
                state = DisplayState.Empty;
            }
            else if (IsHardpointGroupSelected())
            {
                state = DisplayState.Selected;
            }
            else
            {
                state = DisplayState.Loaded;
            }

            return !string.IsNullOrEmpty(weaponName);
        }

        private bool IsHardpointGroupSelected()
        {
            if (weaponManager == null || weaponManager.currentWeaponStation == null)
            {
                return false;
            }

            WeaponStation selectedStation = weaponManager.currentWeaponStation;
            if (!trackedStations.Contains(selectedStation))
            {
                return false;
            }

            if (!highlightNextHardpointGroupOnly || selectedStation.WeaponInfo == null || selectedStation.WeaponInfo.gun || selectedStation.WeaponInfo.fireInterval == 0f || StationWeaponIndexField == null)
            {
                return true;
            }

            int nextWeaponIndex = StationWeaponIndex(selectedStation);
            if (nextWeaponIndex < 0 || nextWeaponIndex >= selectedStation.Weapons.Count)
            {
                return false;
            }

            Weapon nextWeapon = selectedStation.Weapons[nextWeaponIndex];
            Hardpoint nextHardpoint = GetWeaponHardpoint(nextWeapon);
            return nextHardpoint != null && trackedHardpoints.Contains(nextHardpoint);
        }

        private void WeaponsLoaded()
        {
            RebuildTracking();
            Refresh();
        }

        private void RebuildTracking()
        {
            trackedHardpoints.Clear();
            trackedWeapons.Clear();
            trackedStations.Clear();
            hardpointGroupValid = false;
            hardpointGroupWeaponName = string.Empty;
            InvalidateDisplayedValues();

            if (aircraft == null || weaponManager == null || weaponManager.hardpointSets == null || hardpointSetIndices == null || hardpointSetIndices.Length == 0)
                return;

            List<string> weaponNames = new List<string>();
            bool foundValidSet = false;

            for (int i = 0; i < hardpointSetIndices.Length; i++)
            {
                int setIndex = hardpointSetIndices[i];
                if (setIndex < 0 || setIndex >= weaponManager.hardpointSets.Length)
                    continue;

                HardpointSet set = weaponManager.hardpointSets[setIndex];
                if (set == null)
                    continue;

                foundValidSet = true;

                if (set.hardpoints != null)
                {
                    for (int hardpointIndex = 0; hardpointIndex < set.hardpoints.Count; hardpointIndex++)
                    {
                        Hardpoint hardpoint = set.hardpoints[hardpointIndex];
                        if (hardpoint != null)
                        {
                            trackedHardpoints.Add(hardpoint);
                        }
                    }
                }

                if (set.weaponMount != null && set.weaponMount.info != null)
                    AddUniqueWeaponName(weaponNames, set.weaponMount.info.shortName);

            }

            if (!foundValidSet || WeaponHardpointField == null)
                return;

            for (int stationIndex = 0; stationIndex < aircraft.weaponStations.Count; stationIndex++)
            {
                WeaponStation station = aircraft.weaponStations[stationIndex];
                if (station == null || station.Weapons == null)
                    continue;

                bool stationUsed = false;

                for (int weaponIndex = 0; weaponIndex < station.Weapons.Count; weaponIndex++)
                {
                    Weapon weapon = station.Weapons[weaponIndex];
                    Hardpoint hardpoint = GetWeaponHardpoint(weapon);

                    if (weapon == null || hardpoint == null || !trackedHardpoints.Contains(hardpoint))
                        continue;

                    trackedWeapons.Add(weapon);
                    stationUsed = true;

                    if (weapon.info != null)
                        AddUniqueWeaponName(weaponNames, weapon.info.shortName);
                }

                if (stationUsed && !trackedStations.Contains(station))
                {
                    trackedStations.Add(station);
                }
            }

            hardpointGroupWeaponName = JoinWeaponNames(weaponNames);
            hardpointGroupValid = trackedHardpoints.Count > 0 && !string.IsNullOrEmpty(hardpointGroupWeaponName);
        }

        private static void AddUniqueWeaponName(List<string> names, string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName) || names.Contains(weaponName))
                return;

            names.Add(weaponName);
        }

        private static string JoinWeaponNames(List<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;

            if (names.Count == 1)
                return names[0];

            return string.Join(" / ", names.ToArray());
        }

        private static Hardpoint GetWeaponHardpoint(Weapon weapon)
        {
            if (weapon == null || WeaponHardpointField == null)
                return null;

            return WeaponHardpoint(weapon);
        }

        private void ApplyReadout(string weaponName, string ammoReadout, DisplayState state)
        {
            if (weaponName != displayedWeaponName)
            {
                displayedWeaponName = weaponName;

                if (weaponNameText != null)
                {
                    weaponNameText.text = weaponName;
                }
            }

            if (ammoReadout != displayedAmmo)
            {
                displayedAmmo = ammoReadout;

                if (weaponAmmoCountText != null)
                {
                    weaponAmmoCountText.text = ammoReadout;
                }
            }

            int stateValue = (int)state;
            if (displayedState == stateValue)
            {
                return;
            }

            displayedState = stateValue;
            Color stateColor = GetStateColor(state);
            Color stateColorNoAlpha = new Color(stateColor.r, stateColor.g, stateColor.b, 0.1f);

            if (colourTargets != null)
            {
                for (int i = 0; i < colourTargets.Length; i++)
                {
                    if (colourTargets[i] != null)
                    {
                        colourTargets[i].color = stateColor;
                    }
                }
            }
            if (colourTargetsNoAlpha != null)
            {
                for (int i = 0; i < colourTargetsNoAlpha.Length; i++)
                {
                    if (colourTargetsNoAlpha[i] != null)
                    {
                        colourTargetsNoAlpha[i].color = stateColorNoAlpha;
                    }
                }
            }

            if (colourTexts)
            {
                if (weaponNameText != null)
                {
                    weaponNameText.color = stateColor;
                }

                if (weaponAmmoCountText != null)
                {
                    weaponAmmoCountText.color = stateColor;
                }
            }
        }

        private Color GetStateColor(DisplayState state)
        {
            switch (state)
            {
                case DisplayState.Selected:
                    return selectedColor;

                case DisplayState.Loaded:
                    return loadedColor;

                case DisplayState.Empty:
                    return emptyColor;

                default:
                    return unavailableColor;
            }
        }

        private void ApplyTextSettings(TMP_Text text, float appliedFontSize)
        {
            if (text == null)
                return;

            text.fontSize = appliedFontSize;

            if (!colourTexts && type != AppType.MFD)
                text.color = fontColor;

        }

        private void InvalidateDisplayedValues()
        {
            displayedWeaponName = null;
            displayedAmmo = null;
            displayedState = int.MinValue;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (weaponManager != null)
                weaponManager.OnWeaponsLoaded -= WeaponsLoaded;

        }
    }
}
