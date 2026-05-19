using System;
using System.Collections;
using System.Collections.Generic;
using EMILtools.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class FinisherEvent : MonoBehaviour
{
    public List<FinisherChoice> currentAvaliableChoices = new();
    public ChoiceList currentChoiceList = null;
    
    public bool active = false;
    public enum Traversal
    {
        Sequential,
    }

    [Serializable]
    public class ChoiceList
    {
        public List<Spawn> spawns;
        public int neededActionsToComplete;
        public int currentNeededActionsToComplete;
        
        public void CalculateNeddedActionsToComplete() =>
            currentNeededActionsToComplete = neededActionsToComplete = spawns.Count;
    }

    [Serializable]
    public struct Spawn
    {
        public GameObject prefab;
        public Vector3 offset;
        public float spawnDelay;
    }
    
    public InterfaceReference<ISignalReceiverTC<(bool, FinisherChoice)>, MonoBehaviour> finisherSignalReceiver;
    public List<ChoiceList> choiceList;
    public Transform spawnPoint;
    public Traversal traversal;
    
    [ReadOnly] public Ref<bool> stopEarly = new Ref<bool>(false);
    [ReadOnly] public int current = 0;
    [ReadOnly] public List<GameObject> currentlySpawned = new();
    
    Coroutine finisherEvent;

    void Awake()
    {
        if (choiceList == null || choiceList.Count <= 0)
            Debug.LogError("Prefab choices cannot be null or empty!");
    }

    [Button]
    public void CalculateNeededActionsToComplete()
    {
        for (var index = 0; index < choiceList.Count; index++)
        {
            var spawns = choiceList[index];
            spawns.CalculateNeddedActionsToComplete();
            choiceList[index] = spawns;
        }
    }
    
    public void StartEvent() => finisherEvent = StartCoroutine(C_StartEvent());
    
    IEnumerator C_StartEvent()
    {
        currentAvaliableChoices.Clear();
        active = true;
        stopEarly.val = false;
        currentlySpawned.Clear();
        CalculateNeededActionsToComplete();
        currentChoiceList = choiceList[current];
        foreach (Spawn spawn in currentChoiceList.spawns)
        {
            if(stopEarly.val) yield break;
            yield return new WaitForSeconds(spawn.spawnDelay);
            if(stopEarly.val) yield break;

            var newObj = Instantiate(spawn.prefab, spawnPoint).SetActiveThen(false);
            newObj.transform.localPosition += spawn.offset;
            var choice = newObj.GetComponent<FinisherChoice>();
            choice.events = this;
            newObj.SetActive(true);
            choice.PlayEvent();
            currentlySpawned.Add(newObj);
        }
        
        current++;
        if(current >= choiceList.Count) current = 0;
    }
    
    public void StopEarly()
    {
        stopEarly.val = true;
        Stop();
    }

    public void Stop()
    {
        foreach (var obj in currentlySpawned) Destroy(obj);
        currentlySpawned.Clear();
        active = false; 
    }


    public void OnExit()
    {
        
    }
}

