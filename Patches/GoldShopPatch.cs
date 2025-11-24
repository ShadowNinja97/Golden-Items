using System;
using HarmonyLib;
using UnityEngine;

namespace GoldItems;

[HarmonyPatch(typeof(LevelGenerator))]
public static class GoldShopPatch
{
    private static bool _goldShopSpawnedThisLevel = false;

    // Reset flag
    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    private static void Post_Start(LevelGenerator __instance)
    {
        _goldShopSpawnedThisLevel = false;
    }

    // After each module spawn, check if it's a dead end in the shop
    [HarmonyPostfix]
    [HarmonyPatch("SpawnModule")]
    private static void Post_SpawnModule(
        LevelGenerator __instance,
        int x,
        int y,
        Vector3 position,
        Vector3 rotation,
        Module.Type type)
    {
        try
        {
            // Only dead-end modules
            if (type != Module.Type.DeadEnd)
                return;

            // Only on the shop level
            if (!SemiFunc.IsCurrentLevel(__instance.Level, RunManager.instance.levelShop))
                return;

            // Only one Golden Shop per level
            if (_goldShopSpawnedThisLevel)
                return;

            var plugin = GoldItems.Instance;
            if (plugin == null || plugin.GoldShopChance == null)
                return;

            // Chance roll
            float chance = Mathf.Clamp01(plugin.GoldShopChance.Value);
            float roll = UnityEngine.Random.value;
            if (roll > chance)
                return;

            _goldShopSpawnedThisLevel = true;
            GoldItems.Log?.LogInfo(
                $"[GoldenItems] Converting dead-end at ({x},{y}) into Golden Shop (roll={roll:0.00}, chance={chance:0.00})."
            );

            // Find the module that was just spawned near this position
            Module targetModule = FindModuleAtPosition(__instance, position, 1.0f);
            if (targetModule == null)
            {
                GoldItems.Log?.LogWarning("[GoldenItems] Could not find Module at expected position; aborting Golden Shop conversion.");
                return;
            }

            // Remove old module
            ClearModuleInterior(targetModule);

            // Spawn new module
            if (plugin.GoldShopInteriorPrefab != null)
            {
                GameObject interior = UnityEngine.Object.Instantiate(
                    plugin.GoldShopInteriorPrefab,
                    targetModule.transform
                );

                interior.transform.localPosition = Vector3.zero;
                interior.transform.localRotation = Quaternion.identity;
                interior.transform.localScale = Vector3.one;

                GoldItems.Log?.LogInfo(
                    $"[GoldenItems] Golden Shop interior spawned under module '{targetModule.name}'."
                );
            }
            else
            {
                GoldItems.Log?.LogInfo("[GoldenItems] No Golden Shop interior prefab set; using vanilla layout (only door tint).");
            }

            ApplyGoldDoorMaterial(targetModule);
        }
        catch (Exception ex)
        {
            GoldItems.Log?.LogError("[GoldenItems] Exception in GoldShopPatch.Post_SpawnModule: " + ex);
        }
    }


    // Finds the Module nearest to the given position under LevelParent, within a small radius.
    private static Module FindModuleAtPosition(LevelGenerator generator, Vector3 position, float maxDistance)
    {
        if (generator == null || generator.LevelParent == null)
            return null;

        Transform parent = generator.LevelParent.transform;
        Module best = null;
        float bestDist = maxDistance;

        var modules = parent.GetComponentsInChildren<Module>(includeInactive: false);
        foreach (var mod in modules)
        {
            float d = Vector3.Distance(mod.transform.position, position);
            if (d < bestDist)
            {
                bestDist = d;
                best = mod;
            }
        }

        return best;
    }

    /// <summary>
    /// Remove previous interior and props but keeps whatever is necessary to make things work
    /// </summary>
    private static void ClearModuleInterior(Module module)
    {
        if (module == null)
            return;



        for (int i = module.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = module.transform.GetChild(i);

            string name = child.name;

            // Keep dependencies
            if (name == "---- Dependencies ----")
                continue;

            // Keep Door hierarchy
            if (name == "Shop Door")
                continue;

            //Not sure why but this needs to stay for some textures to look correct
            if (name == "Wall 01 - 1x1 - Door (3)" /*|| name == "Wall 01 - 1x1 - Door (2)"*/)
                continue;


            // Dumb repeat logic because the developers weren't consistent with their prefabs
            if (name == "---- Level ------------")
            {
                Transform walls = child.transform.GetChild(1);
                if (walls.childCount == 0)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                    continue;
                }
                for (int w = walls.transform.childCount - 1; w >= 0; w--)
                {
                    Transform wChild = walls.transform.GetChild(w);

                    string cName = wChild.name;

                    if (cName == "Wall 01 - 1x1 - Door (3)" /*|| cName == "Wall 01 - 1x1 - Door (2)"*/)
                        continue;
                    UnityEngine.Object.Destroy(wChild.gameObject);
                }
                UnityEngine.Object.Destroy(child.transform.GetChild(3));
                UnityEngine.Object.Destroy(child.transform.GetChild(2));
                UnityEngine.Object.Destroy(child.transform.GetChild(0));

                continue;
            }

            // Delete everything out else
            UnityEngine.Object.Destroy(child.gameObject);
        }

        GoldItems.Log?.LogInfo(
            $"[GoldenItems] Cleared all module children except '---- Dependencies ----', 'Shop Door', and 'Wall 01 - 1x1 - Door (3)' in module '{module.name}'."
        );
    }



    /// <summary>
    /// Applies the golden material to the existing shop door meshes inside this module.
    /// </summary>
    private static void ApplyGoldDoorMaterial(Module targetModule)
    {
        var plugin = GoldItems.Instance;
        if (plugin == null || plugin.GoldDoorMaterial == null)
        {
            GoldItems.Log?.LogWarning("[GoldenItems] GoldDoorMaterial is null; cannot tint door.");
            return;
        }

        Transform root = targetModule.transform.Find("Shop Door");
        if (root == null)
        {
            GoldItems.Log?.LogWarning(
                $"[GoldenItems] Could not find 'Shop Door' under module '{targetModule.name}'; door tint skipped."
            );
            return;
        }

        // Usually: Shop Door -> Hinge -> Mesh / Handle / Handle
        Transform hinge = root.Find("Hinge");
        if (hinge == null)
        {
            hinge = root; // fallback: search directly under the door root
        }

        // Tint specific children
        TintRendererByIndex(hinge, 0, plugin.GoldDoorMaterial); //Mesh
        TintRendererByIndex(hinge, 4, plugin.GoldDoorMaterial); //Handle
        TintRendererByIndex(hinge, 5, plugin.GoldDoorMaterial); //Handle

        GoldItems.Log?.LogInfo(
            $"[GoldenItems] Applied gold material to shop door under module '{targetModule.name}'."
        );
    }

    private static void TintRendererByIndex(Transform parent, int index, Material mat)
    {
        if (parent == null || mat == null)
            return;

        if (index < 0 || index >= parent.childCount)
        {
            GoldItems.Log?.LogWarning(
                $"[GoldenItems] Tried to tint child index {index} but hinge has only {parent.childCount} children."
            );
            return;
        }

        Transform t = parent.GetChild(index);
        Renderer rend = t.GetComponent<Renderer>();
        if (rend != null)
            rend.material = mat; // instance material so it doesn't affect all doors globally
    }

}