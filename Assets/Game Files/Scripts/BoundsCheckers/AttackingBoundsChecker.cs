using System;
using EMILtools.Systems;
using UnityEngine;
using static AttackingBoundsChecker;

[Serializable]
public class AttackingBoundsChecker : BoundsChecker<AttackCtx>
{
    [Serializable]
    public struct AttackCtx
    {
        public string attackingColliderTag;
        public IEntityCtx attackerEntityCtx;
        public IDamageable.DamageInfo damageInfo;
    }
}



