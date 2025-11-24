using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace GoldItems;

[HarmonyPatch(typeof(ItemAttributes))]
internal static class GoldPricePatch
{
    [HarmonyPostfix]
    [HarmonyPatch("GetValue")]
    private static void Post_GetValue(ItemAttributes __instance)
    {
        try
        {
            if (!SemiFunc.RunIsShop())
                return;

            var plugin = GoldItems.Instance;
            if (plugin == null || GoldItems.GoldShopPriceMultiplier == null)
                return;

            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            if (__instance == null || __instance.value <= 0)
                return;

            // Is this item a gold item?
            bool isGoldItem = __instance.GetComponentInChildren<GoldItemMarker>(true) != null;
            if (!isGoldItem)
                return;

            int mult = GoldItems.GoldShopPriceMultiplier.Value;
            if (mult < 1)
                mult = 1;

            int oldVal = __instance.value;
            int newVal = Mathf.Max(1, oldVal * mult);

            __instance.value = newVal;

            GoldItems.Log?.LogInfo(
                $"[GoldenItems] Master price adjusted for gold item '{__instance.name}' from {oldVal} to {newVal}."
            );
        }
        catch (System.Exception ex)
        {
            GoldItems.Log?.LogError("[GoldenItems] Exception in GoldPricePatch.Post_GetValue: " + ex);
        }
    }

    // Runs on non-master clients when they receive price via RPC
    [HarmonyPostfix]
    [HarmonyPatch("GetValueRPC")]
    private static void Post_GetValueRPC(ItemAttributes __instance)
    {
        try
        {
            if (!SemiFunc.RunIsShop())
                return;

            var plugin = GoldItems.Instance;
            if (plugin == null || GoldItems.GoldShopPriceMultiplier == null)
                return;


            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                return; // master already handled in GetValue postfix

            if (__instance == null || __instance.value <= 0)
                return;

            bool isGoldItem = __instance.GetComponentInChildren<GoldItemMarker>(true) != null;
            if (!isGoldItem)
                return;

            int mult = GoldItems.GoldShopPriceMultiplier.Value;
            if (mult < 1)
                mult = 1;

            int oldVal = __instance.value;
            int newVal = Mathf.Max(1, oldVal * mult);

            __instance.value = newVal;


            GoldItems.Log?.LogInfo(
                $"[GoldenItems] Client price adjusted for gold item '{__instance.name}' from {oldVal} to {newVal}."
            );
        }
        catch (System.Exception ex)
        {
            GoldItems.Log?.LogError("[GoldenItems] Exception in GoldPricePatch.Post_GetValueRPC: " + ex);
        }
    }
}
