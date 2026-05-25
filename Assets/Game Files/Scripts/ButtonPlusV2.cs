using System;
using DataArchitecture;
using LogicArchitecture;
using ProceduralStateMachine;
using Sirenix.OdinInspector;
using StateArchitecture;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPlusV2 : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler  
{
    
    [Serializable] 
    public struct SetActive
    {
        public bool active;
        public GameObject target;
    }
    [Serializable] 
    public struct DeactiveAllChildrenButKeepOAnective
    {
        public GameObject activeChild;
    }

    public struct SharedBtnState
    {
        public byte isHovered;      public bool IsHovered => isHovered == 1;
        public byte isClicked;      public bool IsClicked => isClicked == 1;
    }
    
    public unsafe struct BtnData
    {
        public SharedBtnState* sharedSharedBtnState;
        public SetActive* SetActiveData;
        public DeactiveAllChildrenButKeepOAnective* DeactiveAllChildrenButKeepOAnectiveData;
    }
    
    [Serializable]
    public struct BtnReferences
    {
        public Callbacks Callbacks;
        public SetActive SetActiveData;
        public DeactiveAllChildrenButKeepOAnective DeactiveAllChildrenButKeepOAnectiveData;
    }
    
    static unsafe class ButtonPredicates
    {   
        // Little verbose, but oh well
        public static Predicate IsHovered() => new(&isHovered);
        static bool isHovered(void* ptr)
        {
            SharedBtnState* data = (SharedBtnState*)ptr;
            return data->IsHovered;
        }
        
        public static Predicate IsNotHovered() => new(&isNotHovered);
        static bool isNotHovered(void* ptr)
        {
            SharedBtnState* data = (SharedBtnState*)ptr;
            return !data->IsHovered;
        }
        
        public static Predicate IsClicked() => new(&isClicked);
        static bool isClicked(void* ptr)
        {
            SharedBtnState* data = (SharedBtnState*)ptr;
            return data->IsClicked;
        }
    }
    
    [Flags]
    public enum Callbacks
    {
        None = 0,
        SetActive = 1 << 0,
        DeactiveAllChildrenButKeepOAnective = 1 << 1,
        ButtonUnityEvent = 1 << 2,
        Animate = 1 << 3,
        AudioPlayFromSoundUser = 1 << 4,
        DisapearFade = 1 << 5,
    }
    
    [Flags]
    public enum Event
    {
        None = 0,
        Enter = 1 << 0,
        Exit = 1 << 1,
        Click = 1 << 2,
    }
    enum States
    {
        Default,
        Hover,
        Pressed
    }
    
    // Privis
    DataSingle<BtnData> enterStateData = new(Allocator.Persistent);
    DataSingle<BtnData> clickStateData = new(Allocator.Persistent);
    DataSingle<BtnData> exitStateData = new(Allocator.Persistent);
    DataSingle<SharedBtnState> sharedBtnState = new(Allocator.Persistent);
    
    DataSingle<Predicate> isHoveredPredicate = new(Allocator.Persistent);
    DataSingle<Predicate> isNotHoveredPredicate = new(Allocator.Persistent);
    DataSingle<Predicate> isClickedPredicate = new(Allocator.Persistent);

    ProSM<BtnData> fsm;
    Logics<BtnData> enterLogics, exitLogics, clickLogics;
    NativeList<LogicOperation<BtnData>> enterLogicBuffer, exitLogicBuffer, clickLogicBuffer;
    
    // Pubbis
    public Event ButtonEvents;
    [ShowIf("@(ButtonEvents & Event.Enter) == Event.Enter")] public BtnReferences enter;
    [ShowIf("@(ButtonEvents & Event.Exit) == Event.Exit")] public BtnReferences exit;
    [ShowIf("@(ButtonEvents & Event.Click) == Event.Click")] public BtnReferences click;
    

    

    // ButtonUnityEvent
    // Animate
    // AudioPlayFromSoundUser
    // DisapearFade

    void Awake()
    {
        fsm.Initialize(1);
        fsm.InitLayer<States, BtnData>(0);
        PackLogics();
        StorePtrs();
        AssignLogics();
        EstablishTransitions();
        fsm.Entry(ref exitStateData.Value);
        
        
        void PackLogics()
        {
            enterLogics = CreateLogics(ButtonEvents, Event.Enter, ref enter.Callbacks, ref enterLogicBuffer);
            exitLogics  = CreateLogics(ButtonEvents, Event.Exit,  ref exit.Callbacks, ref exitLogicBuffer);
            clickLogics = CreateLogics(ButtonEvents, Event.Click, ref click.Callbacks, ref clickLogicBuffer);
            Logics<BtnData> CreateLogics(Event events, Event target, ref Callbacks callbacks, ref NativeList<LogicOperation<BtnData>> buffer)
            {
                if (!events.HasFlag(target) || callbacks == Callbacks.None) return default;
                if (buffer.IsCreated) buffer.Dispose();
                buffer = new NativeList<LogicOperation<BtnData>>(Allocator.Persistent);
    
                if (callbacks.HasFlag(Callbacks.SetActive)) buffer.Add(ButtonPlusOperations.SetActive);
                if (callbacks.HasFlag(Callbacks.DeactiveAllChildrenButKeepOAnective)) buffer.Add(ButtonPlusOperations.DeactiveAllChildrenButKeepOAnective);

                if(buffer.Length == 0) Debug.LogError("No Logic Operations were added to the buffer, but the event was still flagged. Please check your ButtonEvents and Callbacks flags.");
                return new Logics<BtnData>(buffer.AsReadOnlySpan());
            }
        }

        unsafe void StorePtrs()
        {
            if(ButtonEvents.HasFlag(Event.Exit))
            {
                exitStateData.Value.SetActiveData = &exit.SetActiveData;
                exitStateData.Value.DeactiveAllChildrenButKeepOAnectiveData = &exit.DeactiveAllChildrenButKeepOAnectiveData;
                exitStateData.Value.sharedSharedBtnState = sharedBtnState.Ptr;
            }
            if(ButtonEvents.HasFlag(Event.Enter))
            {
                enterStateData.Value.SetActiveData = &enter.SetActiveData;
                enterStateData.Value.DeactiveAllChildrenButKeepOAnectiveData = &enter.DeactiveAllChildrenButKeepOAnectiveData;
                enterStateData.Value.sharedSharedBtnState = sharedBtnState.Ptr;
            }
            if(ButtonEvents.HasFlag(Event.Click))
            {
                clickStateData.Value.SetActiveData = &click.SetActiveData;
                clickStateData.Value.DeactiveAllChildrenButKeepOAnectiveData = &click.DeactiveAllChildrenButKeepOAnectiveData;
                clickStateData.Value.sharedSharedBtnState = sharedBtnState.Ptr;
            }
        }

        void AssignLogics()
        {
            ref var layer = ref fsm.layers.Get(0);
            layer.states[(int)States.Hover].OnEnterState = enterLogics;
            layer.states[(int)States.Default].OnEnterState = exitLogics; 
            layer.states[(int)States.Pressed].OnEnterState = clickLogics;
        }

        void EstablishTransitions()
        {
            isHoveredPredicate.Value = ButtonPredicates.IsHovered();
            isNotHoveredPredicate.Value = ButtonPredicates.IsNotHovered();
            isClickedPredicate.Value = ButtonPredicates.IsClicked();
            
            fsm.AddDirectTransition(0, States.Default, States.Hover, ref isHoveredPredicate.Value);
            fsm.AddDirectTransition(0, States.Hover, States.Default, ref isNotHoveredPredicate.Value);
            fsm.AddDirectTransition(0, States.Hover, States.Pressed, ref isClickedPredicate.Value);
        }
    }
    
    


    public unsafe void OnPointerEnter(PointerEventData eventData)
    {
        enterStateData.Value.sharedSharedBtnState->isHovered = 1;
        fsm.TryPollTransitions(ref enterStateData.Value);
    }
    
    public unsafe void OnPointerExit(PointerEventData eventData)    
    {
        exitStateData.Value.sharedSharedBtnState->isHovered = 0;
        clickStateData.Value.sharedSharedBtnState->isClicked = 0;
        fsm.TryPollTransitions(ref exitStateData.Value);   
    }
    public unsafe void OnPointerClick(PointerEventData eventData)          
    {                                                      
        clickStateData.Value.sharedSharedBtnState->isClicked = 1;
        fsm.TryPollTransitions(ref clickStateData.Value);                  
    }                                                                      


    public unsafe static class ButtonPlusOperations
    {
        public static LogicOperation<BtnData> SetActive = new(&SetActiveRun, &SetActiveShouldRun);
        static void SetActiveRun(BtnData* data) => data->SetActiveData->target.SetActive(data->SetActiveData->active);
        static bool SetActiveShouldRun(BtnData* data) => true;
        
        public static LogicOperation<BtnData> DeactiveAllChildrenButKeepOAnective = new(&DeactiveAllChildrenButKeepOAnectiveRun, &DeactiveAllChildrenButKeepOAnectiveShouldRun);

        static void DeactiveAllChildrenButKeepOAnectiveRun(BtnData* data)
        {
            ref DeactiveAllChildrenButKeepOAnective dataRef = ref *data->DeactiveAllChildrenButKeepOAnectiveData;
            var parent = dataRef.activeChild.transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(child.gameObject == dataRef.activeChild);
            }
        }
        static bool DeactiveAllChildrenButKeepOAnectiveShouldRun(BtnData* data) => true;
    }
    
    void OnDestroy()
    {
        sharedBtnState.Dispose(Allocator.Persistent);
        enterStateData.Dispose(Allocator.Persistent);
        clickStateData.Dispose(Allocator.Persistent);
        if (enterLogicBuffer.IsCreated) enterLogicBuffer.Dispose();
        if (exitLogicBuffer.IsCreated) exitLogicBuffer.Dispose();
        if (clickLogicBuffer.IsCreated) clickLogicBuffer.Dispose();
    }
    
}
