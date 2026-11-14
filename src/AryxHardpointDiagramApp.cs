using NuclearOption.UIStyleSystem;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    public class AryxHardpointDiagramApp : HUDApp
    {
        private enum DisplayState
        {
            Invalid,
            Empty,
            Loaded,
            Selected
        }

        [Tooltip("One MonoBehaviour reference per physical pylon shown on the diagram.")]
        [SerializeField]
        private AryxHardpointDisplay[] hardpoints;

        [Header("Colours")]
        [SerializeField]
        private Color loadedColor = Color.white;

        [SerializeField]
        private Color selectedColor = Color.yellow;

        [SerializeField]
        private Color spentColor = Color.red;

        [SerializeField]
        private Color unavailableColor = new Color(1f, 1f, 1f, 0.2f);

        [Header("Behaviour")]
        [Tooltip("Keep used store markers visible in red. Disable to hide them instead.")]
        [SerializeField]
        private bool showSpentAmmo = true;

        [Tooltip("For sequential stores, highlight only the physical pylon containing the next weapon to launch.")]
        [SerializeField]
        private bool highlightNextWeaponOnly = true;

        [Tooltip("Turn ammo images red from the start of the list as stores are released. Disable to spend from the end.")]
        [SerializeField]
        private bool spendAmmoImagesFromStart = true;

        [Tooltip("Hide the entire hardpoint display when no Weapon was equipped on that physical hardpoint. Non-weapon stores such as jammers are thus not shown as spent weapons.")]
        [SerializeField]
        private bool hideWhenNoWeaponEquipped = true;

        private Aircraft aircraft;
        private WeaponManager weaponManager;

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
            if (hardpoints == null)
            {
                return;
            }

            for (int i = 0; i < hardpoints.Length; i++)
            {
                RefreshHardpoint(hardpoints[i]);
            }
        }

        public override void RefreshSettings()
        {
            base.RefreshSettings();
            loadedColor = ThemeManager.Active.ColorTheme.AllClear;
            selectedColor = ThemeManager.Active.ColorTheme.Warning;
            spentColor = ThemeManager.Active.ColorTheme.Alert;

            if (hardpoints != null)
            {
                for (int i = 0; i < hardpoints.Length; i++)
                {
                    if (hardpoints[i] != null)
                    {
                        hardpoints[i].LastState = int.MinValue;
                    }
                }
            }
        }

        private void WeaponsLoaded()
        {
            RebuildTracking();
            Refresh();
        }

        private void RebuildTracking()
        {
            if (hardpoints == null)
            {
                return;
            }

            for (int i = 0; i < hardpoints.Length; i++)
            {
                AryxHardpointDisplay display = hardpoints[i];
                if (display == null)
                {
                    continue;
                }

                display.ResetRuntimeState();

                if (!TryResolveHardpoint(display, out Hardpoint targetHardpoint))
                {
                    continue;
                }

                display.ResolvedHardpoint = targetHardpoint;

                if (aircraft == null || WeaponHardpointField == null)
                {
                    continue;
                }

                for (int stationIndex = 0; stationIndex < aircraft.weaponStations.Count; stationIndex++)
                {
                    WeaponStation station = aircraft.weaponStations[stationIndex];
                    if (station == null || station.Weapons == null)
                    {
                        continue;
                    }

                    for (int weaponIndex = 0; weaponIndex < station.Weapons.Count; weaponIndex++)
                    {
                        Weapon weapon = station.Weapons[weaponIndex];
                        if (weapon == null || !ReferenceEquals(GetWeaponHardpoint(weapon), targetHardpoint))
                        {
                            continue;
                        }

                        display.Weapons.Add(weapon);

                        if (!display.Stations.Contains(station))
                        {
                            display.Stations.Add(station);
                        }

                        display.FullAmmo += Mathf.Max(weapon.GetFullAmmo(), 0);
                    }
                }

                display.HadWeaponEquipped = display.Weapons.Count > 0;

                if (display.HadWeaponEquipped && display.FullAmmo <= 0 && weaponManager != null && display.HardpointSetIndex >= 0 && display.HardpointSetIndex < weaponManager.hardpointSets.Length)
                {
                    HardpointSet set = weaponManager.hardpointSets[display.HardpointSetIndex];
                    if (set != null && set.weaponMount != null)
                    {
                        display.FullAmmo = Mathf.Max(set.weaponMount.ammo, 0);
                    }
                }
            }
        }

        private bool TryResolveHardpoint(AryxHardpointDisplay display, out Hardpoint hardpoint)
        {
            hardpoint = null;

            if (display == null || weaponManager == null || weaponManager.hardpointSets == null || display.HardpointSetIndex < 0 || display.HardpointSetIndex >= weaponManager.hardpointSets.Length)
            {
                return false;
            }

            HardpointSet set = weaponManager.hardpointSets[display.HardpointSetIndex];
            if (set == null || set.hardpoints == null || display.HardpointIndex < 0 || display.HardpointIndex >= set.hardpoints.Count)
            {
                return false;
            }

            hardpoint = set.hardpoints[display.HardpointIndex];
            return hardpoint != null;
        }

        private void RefreshHardpoint(AryxHardpointDisplay display)
        {
            if (display == null)
            {
                return;
            }

            if (display.ResolvedHardpoint == null)
            {
                SetDisplayVisible(display, true);
                ApplyDisplay(display, 0, 0, DisplayState.Invalid);
                return;
            }

            if (hideWhenNoWeaponEquipped && !display.HadWeaponEquipped)
            {
                SetDisplayVisible(display, false);
                return;
            }

            SetDisplayVisible(display, true);

            int remaining = GetRemainingAmmo(display);
            int capacity = display.CapacityOverride > 0 ? display.CapacityOverride : display.FullAmmo;

            Image[] ammoImages = display.AmmoImages;
            if (ammoImages != null)
            {
                capacity = Mathf.Min(capacity, ammoImages.Length);
            }

            remaining = Mathf.Clamp(remaining, 0, capacity);

            DisplayState state;
            if (remaining <= 0)
            {
                state = DisplayState.Empty;
            }
            else if (IsSelected(display))
            {
                state = DisplayState.Selected;
            }
            else
            {
                state = DisplayState.Loaded;
            }

            ApplyDisplay(display, remaining, capacity, state);
        }

        private static void SetDisplayVisible(AryxHardpointDisplay display, bool visible)
        {
            if (display == null)
            {
                return;
            }

            GameObject root = display.DisplayRoot;
            if (root != null && root.activeSelf != visible)
            {
                root.SetActive(visible);
            }
        }

        private static int GetRemainingAmmo(AryxHardpointDisplay display)
        {
            int remaining = 0;

            for (int i = 0; i < display.Weapons.Count; i++)
            {
                Weapon weapon = display.Weapons[i];
                if (weapon != null)
                {
                    remaining += Mathf.Max(weapon.GetAmmoTotal(), 0);
                }
            }

            return remaining;
        }

        private bool IsSelected(AryxHardpointDisplay display)
        {
            if (weaponManager == null || weaponManager.currentWeaponStation == null)
            {
                return false;
            }

            WeaponStation selectedStation = weaponManager.currentWeaponStation;
            if (!display.Stations.Contains(selectedStation))
            {
                return false;
            }

            if (!highlightNextWeaponOnly || selectedStation.WeaponInfo == null || selectedStation.WeaponInfo.gun || selectedStation.WeaponInfo.fireInterval == 0f || StationWeaponIndexField == null)
            {
                return true;
            }

            int nextWeaponIndex = StationWeaponIndex(selectedStation);
            if (nextWeaponIndex < 0 || nextWeaponIndex >= selectedStation.Weapons.Count)
            {
                return false;
            }

            Weapon nextWeapon = selectedStation.Weapons[nextWeaponIndex];
            return nextWeapon != null && ReferenceEquals(GetWeaponHardpoint(nextWeapon), display.ResolvedHardpoint);
        }

        private static Hardpoint GetWeaponHardpoint(Weapon weapon)
        {
            if (weapon == null || WeaponHardpointField == null)
            {
                return null;
            }

            return WeaponHardpoint(weapon);
        }

        private void ApplyDisplay(AryxHardpointDisplay display, int remaining, int capacity, DisplayState state)
        {
            int stateValue = (int)state;
            if (display.LastRemaining == remaining && display.LastCapacity == capacity && display.LastState == stateValue)
                return;

            display.LastRemaining = remaining;
            display.LastCapacity = capacity;
            display.LastState = stateValue;

            Color activeColor = state == DisplayState.Selected ? selectedColor : loadedColor;

            if (display.HardpointImage != null)
                display.HardpointImage.color = GetHardpointColor(state);

            Image[] ammoImages = display.AmmoImages;
            if (ammoImages == null)
                return;

            int spent = capacity - remaining;

            for (int i = 0; i < ammoImages.Length; i++)
            {
                Image image = ammoImages[i];
                if (image == null)
                    continue;

                bool belongsToRack = i < capacity;
                bool loaded = spendAmmoImagesFromStart ? i >= spent && i < capacity : i < remaining;
                bool visible = belongsToRack && (loaded || showSpentAmmo);

                if (image.gameObject.activeSelf != visible)
                    image.gameObject.SetActive(visible);

                if (visible)
                    image.color = loaded ? activeColor : spentColor;

            }
        }

        private Color GetHardpointColor(DisplayState state)
        {
            switch (state)
            {
                case DisplayState.Selected:
                    return selectedColor;

                case DisplayState.Loaded:
                    return loadedColor;

                case DisplayState.Empty:
                    return spentColor;

                default:
                    return unavailableColor;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (weaponManager != null)
            {
                weaponManager.OnWeaponsLoaded -= WeaponsLoaded;
            }
        }
    }
}
