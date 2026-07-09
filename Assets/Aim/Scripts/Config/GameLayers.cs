using UnityEngine;

namespace Aim.Config
{
    public static class GameLayers
    {
        public const string TargetLayerName = "Target";

        public static int Target => LayerMask.NameToLayer(TargetLayerName);

        public static LayerMask TargetMask => LayerMask.GetMask(TargetLayerName);
    }
}
