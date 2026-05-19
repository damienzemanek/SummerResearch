using System;
using UnityEngine;

public class HookCollisionChecker : CollisionChecker<HookCollisionChecker.HookContext>
{
    [Serializable]
    public struct HookContext
    {
        public bool isHooked;
    }
}
