using System;
using System.Collections;
using System.Collections.Generic;
using DesignPatterns.CreationalPatterns;
using EMILtools.Extensions;
using Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EMILtools.Extensions.FadeEX;


public class LoadScene : ReplacerSingleton<LoadScene>
{
    [SerializeField] float minTimeLoading = 1f;
    [SerializeField] FadeSettings fade;

    public int loadScreenIndx;
    [SerializeField] public List<IntList> scenesToLoad = new();

    [Serializable]
    public class IntList
    {
        public string name;
        public List<int> values = new();
    }

    private void OnDisable()
    {
        fade.SetAlpha(0);
        print("a");
    }

    public void LoadSceneFadeScreenToOpaque(int indx)
    {
        if (fade.targ == null) fade.targ = FaderUI.Instance.targ;
        StartCoroutine(C_FadeToOpaque(fade, () => StartCoroutine(Load(indx, true))));
    }

    public GameObject[] LoadingScreenObjects;
    public GameObject[] disables;

    

    IEnumerator Load(int indx, bool unlockMouse = true)
    {
        LoadingScreenObjects.SetAllActive(true);
        disables.SetAllActive(false);
        yield return null;
        StartCoroutine(LoadSceneAsync(indx));
    }
    IEnumerator LoadSceneAsync(int indx, bool unlockMouse = true)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(loadScreenIndx, LoadSceneMode.Additive);
        
        while(!loadOp.isDone)
            yield return null;
        
        
        yield return new WaitForSeconds(minTimeLoading);
        
        List<AsyncOperation> otherSceneOps = new();
        AsyncOperation mainOp = SceneManager.LoadSceneAsync(scenesToLoad[indx].values[0], LoadSceneMode.Single);
        mainOp.allowSceneActivation = false;
        for (int i = 1; i < scenesToLoad[indx].values.Count; i++)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scenesToLoad[indx].values[i], LoadSceneMode.Additive);
            op.allowSceneActivation = false;
            otherSceneOps.Add(op);
        }
        

        while(mainOp.progress < 0.8f && otherSceneOps.TrueForAll(op => op.progress < 0.8f))
            yield return null;
        
        mainOp.allowSceneActivation = true;
        otherSceneOps.ForEach(op => op.allowSceneActivation = true);

        if (unlockMouse)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        fade.SetAlpha(0);
    }
}
