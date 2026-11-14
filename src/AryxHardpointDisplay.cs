using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    //since blueprinter can't serialise custom classes we gotta monobehaviour fucking everything
    public class AryxHardpointDisplay : MonoBehaviour
    {
        [Tooltip("Index in Aircraft.weaponManager.hardpointSets / Aircraft.loadout.weapons.")]
        [SerializeField]
        [Min(0)]
        private int hardpointSetIndex;

        [Tooltip("Physical hardpoint index inside the selected hardpoint set.")]
        [SerializeField]
        [Min(0)]
        private int hardpointIndex;

        [Tooltip("Main pylon marker. Loaded, selected and empty state is shown by its colour.")]
        [SerializeField]
        private Image hardpointImage;

        [Tooltip("One image per visible store position.")]
        [SerializeField]
        private Image[] ammoImages;

        [Tooltip("Override for the number of store markers. Zero derives it from the loaded weapons.")]
        [SerializeField]
        [Min(0)]
        private int capacityOverride;
        [Tooltip("Object hidden when this hardpoint has no weapon equipped. Leave empty to use this GameObject.")]
        [SerializeField]
        private GameObject displayRoot;

        public int HardpointSetIndex => hardpointSetIndex;

        public int HardpointIndex => hardpointIndex;

        public Image HardpointImage => hardpointImage;

        public Image[] AmmoImages => ammoImages;

        public int CapacityOverride => capacityOverride;

        public GameObject DisplayRoot => displayRoot != null ? displayRoot : gameObject;

        [NonSerialized]
        internal Hardpoint ResolvedHardpoint;

        [NonSerialized]
        internal readonly List<Weapon> Weapons = new List<Weapon>();

        [NonSerialized]
        internal readonly List<WeaponStation> Stations = new List<WeaponStation>();

        [NonSerialized]
        internal int FullAmmo;

        [NonSerialized]
        internal bool HadWeaponEquipped;

        [NonSerialized]
        internal int LastRemaining = int.MinValue;

        [NonSerialized]
        internal int LastCapacity = int.MinValue;

        [NonSerialized]
        internal int LastState = int.MinValue;

        internal void ResetRuntimeState()
        {
            ResolvedHardpoint = null;
            Weapons.Clear();
            Stations.Clear();
            FullAmmo = 0;
            HadWeaponEquipped = false;
            LastRemaining = int.MinValue;
            LastCapacity = int.MinValue;
            LastState = int.MinValue;
        }
    }
}
