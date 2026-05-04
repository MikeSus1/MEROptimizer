using AdminToys;
using Mirror;
using UnityEngine;

namespace MEROptimizer.Helper;

public class PrefabHelper
{
    public static uint PrimitiveAssetId { get; private set; }
    public static uint LightAssetId { get; private set; }
    public static uint CapybaraAssetId { get; private set; }

    public static void RegisterPrefabs()
    {
        foreach (GameObject prefab in NetworkClient.prefabs.Values)
        {
            if (prefab.TryGetComponent<PrimitiveObjectToy>(out var primitiveObjectToy))
            {
                PrimitiveAssetId = primitiveObjectToy.netIdentity.assetId;
                continue;
            }

            if (prefab.TryGetComponent<LightSourceToy>(out var lightSourceToy))
            {
                LightAssetId = lightSourceToy.netIdentity.assetId;
                continue;
            }
            
            if (prefab.TryGetComponent<CapybaraToy>(out var capybaraToy))
            {
                CapybaraAssetId = capybaraToy.netIdentity.assetId;
                continue;
            }
        }
    }
}