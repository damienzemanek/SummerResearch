using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class HealthSlotManager : MonoBehaviour
{
    [ShowInInspector, ReadOnly] public float currHp => entity == null? -1 : entity.health.Value;
    
    public LivingEntity entity;
    
    public List<Image> healthSlots;
    public List<Animator> animators;

    public float dmgAnimOffsetTime = 0.05f;
    public string dmgAnimName = "Dmg";
    public string hardDmgAnimname = "HardDmg";

    private int dmgAnimHash;
    private int hardDmgAnimHash;
    
    int slotCount => healthSlots?.Count ?? 0;
    WaitForSeconds waitTime;

    public Sprite Active;
    public Sprite Inactive;

    private void Awake()
    {
        waitTime = new WaitForSeconds(dmgAnimOffsetTime);
        if(entity == null) Debug.LogError("Entity is null!");
        entity.NewHealthEvent.Add(UpdateHealth);
        hardDmgAnimHash = Animator.StringToHash(hardDmgAnimname);
        dmgAnimHash = Animator.StringToHash(dmgAnimName);
    }


    [Button]
    private void Start()
    {
        healthSlots = new List<Image>(GetComponentsInChildren<Image>(false));
        animators = new List<Animator>(GetComponentsInChildren<Animator>(false));
    }

    [Button]
    public void UpdateHealth(float curr, float max)
    {
        var percentHpRemaining = curr / max;
        var ret = percentHpRemaining * slotCount;

        StopAllCoroutines();
        StartCoroutine(C_Dmg(ret));
    }

    IEnumerator C_Dmg(float ret)
    {
        for (int i = slotCount-1; i >= 0; i--)
        {
            bool isActive = i < ret;
            if (isActive)
            {
                healthSlots[i].sprite = Active;
                animators[i].Play(dmgAnimName, 0, 0f);
            }
            else if (healthSlots[i].sprite == Active)
            {
                healthSlots[i].sprite = Inactive;
                animators[i].Play(hardDmgAnimHash, 0, 0f);
            }
            yield return waitTime;
        }
    }
    
}
