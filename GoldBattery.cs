using UnityEngine;

namespace GoldItems;

public class GoldBattery : MonoBehaviour
{
    private ItemBattery battery;
    private ItemAttributes attributes;

    private float lastBatteryLife;
    private float maxBatteryLife;
    private bool initialized;
    private float nextRechargeTime = -1f;

    private void Awake()
    {
        battery = GetComponent<ItemBattery>();
        attributes = GetComponent<ItemAttributes>();

        if (battery == null)
        {
            Debug.LogWarning("[GoldBattery] No ItemBattery found on this object.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (battery == null)
            return;

        // Wait until ItemBattery has finished its own init
        if (!initialized)
        {
            // Match ItemBattery.BatteryInit wait conditions
            if (LevelGenerator.Instance == null || !LevelGenerator.Instance.Generated)
                return;

            if (attributes == null || attributes.instanceName == null)
                return;

            // At this point StatsManager should have set the battery level.
            // Whatever it is now is our max for this item.
            maxBatteryLife = battery.batteryLife;
            lastBatteryLife = maxBatteryLife;
            initialized = true;
            return;
        }

        float current = battery.batteryLife;

        // Detect any real drain since last frame
        bool drainedThisFrame = current < lastBatteryLife - 0.01f;
        if (drainedThisFrame)
        {
            float cooldown = GoldItems.CooldownSeconds.Value;
            nextRechargeTime = Time.time + cooldown;
        }

        lastBatteryLife = current;

        // Already at or effectively at original max -> nothing to do
        if (current >= maxBatteryLife - 0.01f)
            return;

        // Cooldown not started or still ticking -> no recharge yet
        if (nextRechargeTime < 0f || Time.time < nextRechargeTime)
            return;

        // Recharge using global rate from config
        float rate = GoldItems.RechargePercentPerSecond.Value;

        // Recharge toward this item's original max
        float newLife = Mathf.Min(
            current + rate * Time.deltaTime,
            maxBatteryLife
        );

        battery.batteryLife = newLife;
        // ItemBattery.Update() will handle everything else, probably
    }
}
