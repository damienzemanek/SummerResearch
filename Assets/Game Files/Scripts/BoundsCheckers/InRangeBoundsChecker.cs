using System;
using UnityEngine;

public class InRangeBoundsChecker : BoundsChecker<InRangeBoundsChecker.InRangeContext>
{
    [Serializable]
    public struct InRangeContext
    {
        public bool inRange;
    }
}
