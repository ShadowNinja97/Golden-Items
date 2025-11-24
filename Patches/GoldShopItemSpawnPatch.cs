using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace GoldItems;

/// <summary>
/// Filters which items can spawn in a given ItemVolume:
/// - Volumes with GoldShopVolume: only items whose prefab has GoldItemMarker.
/// - Other volumes: never spawn items whose prefab has GoldItemMarker.
/// </summary>
[HarmonyPatch(typeof(PunManager))]
internal static class GoldShopItemSpawnPatch
{
    [HarmonyPatch("SpawnShopItem")]
    [HarmonyPrefix]
    private static bool SpawnShopItem_GoldFilter(
        ItemVolume itemVolume,
        List<Item> itemList,
        ref int spawnCount,
        bool isSecret,
        ref bool __result)
    {
        // Safety checks
        if (itemVolume == null || itemList == null || ShopManager.instance == null)
        {
            __result = false;
            return false; // skip original
        }

        // Is this a gold-only volume?
        bool isGoldVolume = itemVolume.GetComponent<GoldShopVolume>() != null;

        //Spawn logic with checks
        for (int num = itemList.Count - 1; num >= 0; num--)
        {
            Item item = itemList[num];
            if (item == null)
                continue;

            // Determine if this is a gold item (prefab has GoldItemMarker)
            bool isGoldItem = false;
            GameObject prefabGo = null;

            try
            {
                // PrefabRef.Prefab can be null if misconfigured
                prefabGo = item.prefab != null ? item.prefab.Prefab : null;
            }
            catch
            {
                prefabGo = null;
            }

            if (prefabGo != null &&
                prefabGo.GetComponentInChildren<GoldItemMarker>(true) != null)
            {
                isGoldItem = true;
            }

            // Apply gold filtering:
            // - Gold volumes: ONLY gold items.
            // - Non-gold volumes: NEVER gold items.
            if (isGoldVolume)
            {
                if (!isGoldItem)
                    continue; // skip non-gold items in gold rooms
            }
            else
            {
                if (isGoldItem)
                    continue; // don't spawn gold items outside gold rooms
            }

            // Original condition
            if (item.itemVolume != itemVolume.itemVolume)
                continue;

            // Original spawn logic
            // Use ShopManager.instance.itemRotateHelper to get the correct rotation.
            ShopManager shopManager = ShopManager.instance;
            Transform helper = shopManager.itemRotateHelper;

            helper.transform.parent = itemVolume.transform;
            helper.transform.localRotation = item.spawnRotationOffset;
            Quaternion rotation = helper.transform.rotation;
            helper.transform.parent = shopManager.transform;

            // Instantiate item prefab
            if (SemiFunc.IsMultiplayer())
            {
                PhotonNetwork.InstantiateRoomObject(
                    item.prefab.ResourcePath,
                    itemVolume.transform.position,
                    rotation,
                    0
                );
            }
            else
            {
                Object.Instantiate(
                    item.prefab.Prefab,
                    itemVolume.transform.position,
                    rotation
                );
            }

            // Remove the item from its pool so it won't spawn again
            itemList.RemoveAt(num);

            // Secret items don't count towards the standard spawn count
            if (!isSecret)
            {
                spawnCount++;
            }

            __result = true;
            return false; // skip original
        }

        // No matching item found for this volume
        __result = false;
        return false; // skip original
    }
}
