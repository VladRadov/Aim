using System;
using UnityEngine;

namespace Aim.Config
{
    [Serializable]
    public sealed class ShopWeaponEntry
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] GameObject prefab;
        [SerializeField] int price;
        [SerializeField] Sprite icon;
        [SerializeField] bool ownedByDefault;

        public string Id => string.IsNullOrWhiteSpace(id) ? displayName : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? (prefab != null ? prefab.name : "Weapon") : displayName;
        public GameObject Prefab => prefab;
        public int Price => Mathf.Max(0, price);
        public Sprite Icon => icon;
        public bool OwnedByDefault => ownedByDefault;
    }

    [CreateAssetMenu(fileName = "WeaponShopCatalog", menuName = "Aim/Weapon Shop Catalog")]
    public sealed class WeaponShopCatalog : ScriptableObject
    {
        [SerializeField] ShopWeaponEntry[] weapons = Array.Empty<ShopWeaponEntry>();

        public ShopWeaponEntry[] Weapons => weapons;
    }
}
