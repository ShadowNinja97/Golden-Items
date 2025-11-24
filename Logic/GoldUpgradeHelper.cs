using UnityEngine;

namespace GoldItems;

internal static class GoldUpgradeHelper
{
    public static void ApplyGoldenUpgrade(string steamID)
    {
        var plugin = GoldItems.Instance;
        if (plugin == null || PunManager.instance == null)
            return;

        bool applyToAll = GoldItems.ShareGoldenUpgrade != null && GoldItems.ShareGoldenUpgrade.Value;

        if (!applyToAll)
        {
            // Apply only to the user who picked it up
            ApplyGoldenUpgradeToSingle(steamID);
            return;
        }

        // Apply to ALL active players
        if (GameDirector.instance == null || GameDirector.instance.PlayerList == null)
            return;

        foreach (var player in GameDirector.instance.PlayerList)
        {
            if (!player || player.isDisabled)
                continue;

            string id = SemiFunc.PlayerGetSteamID(player);
            if (string.IsNullOrEmpty(id))
                continue;

            ApplyGoldenUpgradeToSingle(id);
        }
    }

    private static void ApplyGoldenUpgradeToSingle(string steamID)
    {
        var pun = PunManager.instance;
        if (pun == null || string.IsNullOrEmpty(steamID))
            return;

        // Health
        pun.UpgradePlayerHealth(steamID);
        pun.UpgradeDeathHeadBattery(steamID);

        // Sprint
        pun.UpgradePlayerEnergy(steamID);
        pun.UpgradePlayerCrouchRest(steamID);
        pun.UpgradePlayerSprintSpeed(steamID);

        //Mobility
        pun.UpgradePlayerExtraJump(steamID);
        pun.UpgradePlayerTumbleLaunch(steamID);
        pun.UpgradePlayerTumbleClimb(steamID);
        pun.UpgradePlayerTumbleWings(steamID);

        //Misc
        pun.UpgradeMapPlayerCount(steamID); //Might want to clamp this at some point

        // Grab
        pun.UpgradePlayerGrabStrength(steamID);
        pun.UpgradePlayerGrabRange(steamID);

    }
}