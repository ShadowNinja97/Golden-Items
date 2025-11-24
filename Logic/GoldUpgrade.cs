using UnityEngine;

namespace GoldItems;

public class GoldUpgrade : MonoBehaviour
{
    private ItemToggle itemToggle;

    private void Start()
    {
        itemToggle = GetComponent<ItemToggle>();
    }

    public void Upgrade()
    {
        if (itemToggle == null)
            return;

        var playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
        if (playerAvatar == null)
            return;

        string steamID = SemiFunc.PlayerGetSteamID(playerAvatar);
        GoldUpgradeHelper.ApplyGoldenUpgrade(steamID);
    }
}