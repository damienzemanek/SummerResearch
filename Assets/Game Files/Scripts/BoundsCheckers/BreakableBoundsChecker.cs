using System;
using UnityEngine;


public class BreakableBoundsChecker : MonoBehaviour,
    IBoundsCheckMsgReceiver<Collider2D, AttackingBoundsChecker.AttackCtx>
{
    public bool destroyed;
    public SpriteRenderer spriteRenderer;
    public Collider2D boundsCollider;
    public GameObject childEnable;

    void Awake() => childEnable.SetActive(false);

    public void OnEnterBounds(Collider2D collidedWith, BoundsChecker<AttackingBoundsChecker.AttackCtx> sender, AttackingBoundsChecker.AttackCtx ctx) 
        => Break();

    void Break()
    {
        if(destroyed) return;
        boundsCollider.enabled = false;
        spriteRenderer.enabled = false;
        destroyed = true;
        childEnable.SetActive(true);
    }
}
