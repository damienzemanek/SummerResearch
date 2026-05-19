using System;
using UnityEngine;
using static CanSeeBoundsChecker;

public class CanSeeBoundsChecker : BoundsChecker<CanSeeContext>
{
    [Serializable]
    public struct CanSeeContext
    {
        public bool canSeeTarget;
    }
}
