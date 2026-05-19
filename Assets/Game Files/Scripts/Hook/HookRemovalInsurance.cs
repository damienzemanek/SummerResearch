using UnityEngine;

public class HookRemovalInsurance : MonoBehaviour
{
    public void RemoveHook()
    {
        var hook =GetComponentInChildren<Hook>();
        if (hook != null)
            hook.gameObject.transform.SetParent(null);
    }
}
