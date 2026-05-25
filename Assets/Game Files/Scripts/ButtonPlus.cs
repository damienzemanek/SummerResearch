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
    ProSM<BtnData> fsm;
    Logics<BtnData> enterLogics, exitLogics, clickLogics;
    NativeList<LogicOperation<BtnData>> enterLogicBuffer, exitLogicBuffer, clickLogicBuffer;
    
    BtnData enterStateData;
    BtnData clickStateData;
    BtnData exitStateData;
    public BtnData.SharedBtnState sharedBtnState;

    Predicate isHoveredPredicate;
    Predicate isNotHoveredPredicate;
    Predicate isClickedPredicate;

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
        sharedBtnState.ManagedBtn = BlittableManagedReference<ButtonPlus>.Allocate(this);

        fsm.Initialize(1);
        fsm.InitLayer<BtnStates, BtnData>(0);
        InitValues();
        DynamicallyPackLogics();
        EstablishTransitions();
        fsm.Entry(ref exitStateData);
        
        
        void DynamicallyPackLogics()
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
            
            ref var layer = ref fsm.layers.Get(0);
            layer.states[(int)BtnStates.Hover].OnEnterState = enterLogics;
            layer.states[(int)BtnStates.Default].OnEnterState = exitLogics; 
            layer.states[(int)BtnStates.Pressed].OnEnterState = clickLogics;
        }

        void InitValues()
        {
            if (buttonBtnEvents.HasFlag(Exit)) {
                exitStateData.btnEventType = Exit;
                exitStateData.managedBtn = sharedBtnState.ManagedBtn; }
            if (buttonBtnEvents.HasFlag(Enter)) {
                enterStateData.btnEventType = Enter;
                enterStateData.managedBtn = sharedBtnState.ManagedBtn; }
            if (buttonBtnEvents.HasFlag(Click)) {
                clickStateData.btnEventType = Click;
                clickStateData.managedBtn = sharedBtnState.ManagedBtn; }
            
            isHoveredPredicate = ButtonPredicates.IsHovered();
            isNotHoveredPredicate = ButtonPredicates.IsNotHovered();
            isClickedPredicate = ButtonPredicates.IsClicked();
        }


        void EstablishTransitions()
        {
            fsm.AddDirectTransition(0, BtnStates.Default, BtnStates.Hover, ref isHoveredPredicate);
            fsm.AddDirectTransition(0, BtnStates.Hover, BtnStates.Default, ref isNotHoveredPredicate);
            fsm.AddDirectTransition(0, BtnStates.Hover, BtnStates.Pressed, ref isClickedPredicate);
            fsm.AddDirectTransition(0, BtnStates.Pressed, BtnStates.Default, ref isNotHoveredPredicate);
            
            //fsm.AddDirectTimedTransition(0, BtnStates.Pressed, BtnStates.Default, 0.5f);

        }
    }
    

    public void OnPointerEnter(PointerEventData eventData)
    {
        enterStateData.sharedBtnStateData.isHovered = 1;
        fsm.TryPollTransitions(ref enterStateData);
    }
    
    public void OnPointerExit(PointerEventData eventData)    
    {
        exitStateData.sharedBtnStateData.isHovered = 0;
        clickStateData.sharedBtnStateData.isClicked = 0;
        fsm.TryPollTransitions(ref exitStateData);   
    }
    public void OnPointerClick(PointerEventData eventData)          
    {                                                      
        clickStateData.sharedBtnStateData.isClicked = 1;
        fsm.TryPollTransitions(ref clickStateData);  
        clickStateData.sharedBtnStateData.isClicked = 0;
    }                                                                      
    
    
    void OnDestroy()
    {
        sharedBtnState.ManagedBtn.Free();
        
        fsm.Dispose();

        if (enterLogicBuffer.IsCreated) enterLogicBuffer.Dispose();
        if (exitLogicBuffer.IsCreated) exitLogicBuffer.Dispose();
        if (clickLogicBuffer.IsCreated) clickLogicBuffer.Dispose();
    }
    
}
