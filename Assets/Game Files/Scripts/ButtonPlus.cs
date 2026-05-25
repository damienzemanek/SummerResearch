using System;
using System.Runtime.InteropServices;
using DataArchitecture;
using LogicArchitecture;
using ProceduralStateMachine;
using Sirenix.OdinInspector;
using StateArchitecture;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using static ButtonPlusDepandancies;
using static ButtonPlusDepandancies.BtnEvent;

public class ButtonPlus : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler  
{
    
    // Privis
    DataSingle<BtnData> enterStateData = new(Allocator.Persistent);
    DataSingle<BtnData> clickStateData = new(Allocator.Persistent);
    DataSingle<BtnData> exitStateData = new(Allocator.Persistent);
    DataSingle<BtnData.SharedBtnState> sharedBtnState = new(Allocator.Persistent);
    
    DataSingle<Predicate> isHoveredPredicate = new(Allocator.Persistent);
    DataSingle<Predicate> isNotHoveredPredicate = new(Allocator.Persistent);
    DataSingle<Predicate> isClickedPredicate = new(Allocator.Persistent);

    ProSM<BtnData> fsm;
    Logics<BtnData> enterLogics, exitLogics, clickLogics;
    NativeList<LogicOperation<BtnData>> enterLogicBuffer, exitLogicBuffer, clickLogicBuffer;
    GCHandle btnHandle;

    bool ShowEnter => (buttonBtnEvents & Enter) != 0;
    bool ShowExit => (buttonBtnEvents & Exit) != 0;
    bool ShowClick => (buttonBtnEvents & Click) != 0;
    
    
    // Pubbis
    [FormerlySerializedAs("ButtonEvents")] public BtnEvent buttonBtnEvents;
    [FormerlySerializedAs("enter")] [ShowIf(nameof(ShowEnter))] public BtnReferences enterRefs;
    [FormerlySerializedAs("exit")] [ShowIf(nameof(ShowExit))] public BtnReferences exitRefs;
    [FormerlySerializedAs("click")] [ShowIf(nameof(ShowClick))] public BtnReferences clickRefs;
    
    

    void Awake()
    {
        btnHandle = GCHandle.Alloc(this);
        sharedBtnState.Value.buttonHandle = GCHandle.ToIntPtr(btnHandle);

        fsm.Initialize(1);
        fsm.InitLayer<States, BtnData>(0);
        PackLogics();
        StorePtrs();
        AssignLogics();
        EstablishTransitions();
        fsm.Entry(ref exitStateData.Value);
        
        
        void PackLogics()
        {
            enterLogics = CreateLogics(buttonBtnEvents, Enter, ref enterRefs.Callbacks, ref enterLogicBuffer);
            exitLogics  = CreateLogics(buttonBtnEvents, Exit,  ref exitRefs.Callbacks, ref exitLogicBuffer);
            clickLogics = CreateLogics(buttonBtnEvents, Click, ref clickRefs.Callbacks, ref clickLogicBuffer);
            Logics<BtnData> CreateLogics(BtnEvent btnEvents, BtnEvent target, ref Callbacks callbacks, ref NativeList<LogicOperation<BtnData>> buffer)
            {
                if (!btnEvents.HasFlag(target) || callbacks == Callbacks.None) return default;
                if (buffer.IsCreated) buffer.Dispose();
                buffer = new NativeList<LogicOperation<BtnData>>(Allocator.Persistent);
    
                if (callbacks.HasFlag(Callbacks.SetActive)) buffer.Add(ButtonPlusOperations.SetActive);
                if (callbacks.HasFlag(Callbacks.ChildsDeactivateKeepSelfActive)) buffer.Add(ButtonPlusOperations.DeactiveAllChildrenButKeepOAnective);
                if (callbacks.HasFlag(Callbacks.Animate)) buffer.Add(ButtonPlusOperations.Animate);
                if (callbacks.HasFlag(Callbacks.ButtonUnityEvent)) buffer.Add(ButtonPlusOperations.BtnUnityEvent);
                if (callbacks.HasFlag(Callbacks.PlaySound)) buffer.Add(ButtonPlusOperations.PlaySound);
                
                if(buffer.Length == 0) Debug.LogError("No Logic Operations were added to the buffer, but the event was still flagged. Please check your buttonBtnEvents and Callbacks flags.");
                return new Logics<BtnData>(buffer.AsReadOnlySpan());
            }
        }

        unsafe void StorePtrs()
        {
            if(buttonBtnEvents.HasFlag(Exit))
            {
                exitStateData.Value.btnEventType = Exit;
                exitStateData.Value.sharedSharedBtnState = sharedBtnState.Ptr;
            }
            if(buttonBtnEvents.HasFlag(Enter))
            {
                enterStateData.Value.btnEventType = Enter;
                enterStateData.Value.sharedSharedBtnState = sharedBtnState.Ptr;
            }
            if(buttonBtnEvents.HasFlag(Click))
            {
                clickStateData.Value.btnEventType = Click;
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
            fsm.AddDirectTransition(0, States.Pressed, States.Hover, ref isHoveredPredicate.Value);
            fsm.AddDirectTransition(0, States.Pressed, States.Default, ref isNotHoveredPredicate.Value);

        }
    }
    
    


    public unsafe void OnPointerEnter(PointerEventData eventData)
    {
        enterStateData.Value.sharedSharedBtnState->isHovered = 1;
        fsm.TryPollTransitions(ref enterStateData.Value);
        Debug.Log("Pointer Entered");
    }
    
    public unsafe void OnPointerExit(PointerEventData eventData)    
    {
        exitStateData.Value.sharedSharedBtnState->isHovered = 0;
        clickStateData.Value.sharedSharedBtnState->isClicked = 0;
        fsm.TryPollTransitions(ref exitStateData.Value);   
        Debug.Log("Pointer Exited");
    }
    public unsafe void OnPointerClick(PointerEventData eventData)          
    {                                                      
        clickStateData.Value.sharedSharedBtnState->isClicked = 1;
        fsm.TryPollTransitions(ref clickStateData.Value);  
        Debug.Log("Pointer Clicked");
        clickStateData.Value.sharedSharedBtnState->isClicked = 0;
    }                                                                      
    
    
    void OnDestroy()
    {
        if (btnHandle.IsAllocated) btnHandle.Free();
        sharedBtnState.Dispose(Allocator.Persistent);
        enterStateData.Dispose(Allocator.Persistent);
        clickStateData.Dispose(Allocator.Persistent);
        exitStateData.Dispose(Allocator.Persistent);
        
        isHoveredPredicate.Dispose(Allocator.Persistent);
        isNotHoveredPredicate.Dispose(Allocator.Persistent);
        isClickedPredicate.Dispose(Allocator.Persistent);
        
        fsm.Dispose();

        if (enterLogicBuffer.IsCreated) enterLogicBuffer.Dispose();
        if (exitLogicBuffer.IsCreated) exitLogicBuffer.Dispose();
        if (clickLogicBuffer.IsCreated) clickLogicBuffer.Dispose();
    }
    
}
