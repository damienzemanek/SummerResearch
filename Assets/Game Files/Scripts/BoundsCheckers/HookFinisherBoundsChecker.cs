using System;
using UnityEngine;

public class HookFinisherBoundsChecker : BoundsChecker<HookFinisherBoundsChecker.HookFinisherContext>
{
    [Serializable]
    public struct HookFinisherContext
    {
        public bool inside;
    }
}
