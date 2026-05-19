using System;
using System.Collections;
using EMILtools.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

public class FinisherChoice :
    MonoBehaviour,
    IBoundsCheckMsgReceiver<Collider2D, HookFinisherBoundsChecker.HookFinisherContext>
{
    [Required] public SplineAnimate splineAnimate;
    public FinisherEvent events;
    WaitForSeconds waitTime;
    public bool successfull = false;

    void Awake()
    {
        waitTime = new WaitForSeconds(splineAnimate.Duration);
    }
    
    public void PlayEvent() => StartCoroutine(C_PlayEvent());
    
    IEnumerator C_PlayEvent()
    {
        successfull = false;
        splineAnimate.Restart(autoplay: true);
        yield return waitTime;
        events.OnExit();
    }

    public int Hit()
    {
        Debug.Log("HIT");
        gameObject.SetActive(false);
        successfull = true;
        return --events.currentChoiceList.currentNeededActionsToComplete;
    }

    public void Miss()
    {
        Debug.Log("MISS");
    }

    /// <summary>
    /// When Finisher Spline is in Finisher Bounds
    /// </summary>
    /// <param name="collidedWith"></param>
    /// <param name="sender"></param>
    /// <param name="ctx"></param>
    public void OnEnterBounds(Collider2D collidedWith,
        BoundsChecker<HookFinisherBoundsChecker.HookFinisherContext> sender,
        HookFinisherBoundsChecker.HookFinisherContext ctx)
    {
        if(events == null) return;
        events.currentAvaliableChoices.Add(this);
        events.finisherSignalReceiver.Value.Send("FINISHER", (true, this));
    }

    /// <summary>
    /// When Finisher Spline is out of Finisher Bounds
    /// </summary>
    /// <param name="collidedWith"></param>
    /// <param name="sender"></param>
    /// <param name="ctx"></param>
    public void OnExitBounds(Collider2D collidedWith,
        BoundsChecker<HookFinisherBoundsChecker.HookFinisherContext> sender,
        HookFinisherBoundsChecker.HookFinisherContext ctx)

    {
        if(events == null) return;
        if(events.currentAvaliableChoices.Contains(this))
        {
            events.currentAvaliableChoices.Remove(this);
            events.finisherSignalReceiver.Value.Send("FINISHER", (false, this));
        }
        if(!successfull) Miss();
    }
    

}
