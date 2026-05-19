using EMILtools.Extensions;
using UnityEngine;

public class FaderUI : MonoBehaviour
{
    public static FaderUI Instance;

    public Object targ;
    public bool FadeInOnStart;
    [SerializeField] FadeSettings fade;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (FadeInOnStart)
        {
            if (fade.targ == null) fade.targ = targ;
            StartCoroutine(FadeEX.C_FadeToTransparent(fade, () =>
            {
                if (TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.blocksRaycasts = false;
                }
            }));
        }
    }
}
