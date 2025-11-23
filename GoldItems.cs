using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GoldItems;

[BepInPlugin("WorthyOtter.GoldItems", "Golden Items", "1.0.0")]
public class GoldItems : BaseUnityPlugin
{
    public static GoldItems Instance { get; private set; }
    public static ManualLogSource Log { get; private set; }

    public ConfigEntry<float> GoldShopChance;
    public static ConfigEntry<int> CooldownSeconds;
    public static ConfigEntry<int> RechargePercentPerSecond;

    public AssetBundle GoldShopBundle;
    public GameObject GoldShopInteriorPrefab;
    public Material GoldDoorMaterial;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        GoldShopChance = Config.Bind(
            "Gold Shop",
            "GoldShopChance",
            1f,
            new ConfigDescription("Chance (0–1) that a shop dead end is turned into the Golden Shop.", new AcceptableValueRange<float>(0f, 1f))
        );

        CooldownSeconds = Config.Bind(
            "Golden Items",
            "CooldownSeconds",
            60,
            new ConfigDescription("Seconds of inactivity before a golden item starts recharging.", new AcceptableValueRange<int>(0, 300))
        );

        RechargePercentPerSecond = Config.Bind(
            "Golden Items",
            "RechargePercentPerSecond",
            5,
            new ConfigDescription("Percent of max charge restored per second while recharging (0–1).", new AcceptableValueRange<int>(0, 100))
        );

        LoadGoldShopPrefab();

        var harmony = new Harmony("WorthyOtter.GoldItems");
        harmony.PatchAll();
    }

    private void LoadGoldShopPrefab()
    {
        string pluginDir = Path.GetDirectoryName(Info.Location);
        string bundlePath = Path.Combine(pluginDir, "goldenitemsassets");

        GoldShopBundle = AssetBundle.LoadFromFile(bundlePath);
        if (GoldShopBundle == null)
        {
            Log.LogError($"[GoldenItems] Failed to load AssetBundle at '{bundlePath}'.");
            return;
        }
        const string prefabNameInBundle = "assets/goldenitems/module - shop - de - golden shop.prefab";

        GoldShopInteriorPrefab = GoldShopBundle.LoadAsset<GameObject>(prefabNameInBundle);
        if (GoldShopInteriorPrefab == null)
        {
            Log.LogError($"[GoldenItems] Failed to load prefab '{prefabNameInBundle}' from bundle.");
        }
        else
        {
            Log.LogInfo($"[GoldenItems] Loaded GoldenShop prefab '{GoldShopInteriorPrefab.name}' from AssetBundle.");
        }

        const string matNameInBundle = "assets/goldenitems/gold.mat";

        GoldDoorMaterial = GoldShopBundle.LoadAsset<Material>(matNameInBundle);

    }
}
