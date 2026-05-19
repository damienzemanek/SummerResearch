using UnityEngine;

public class LoadSceneConnector : MonoBehaviour
{
    public void Load(int sceneIndx)
    {
        Debug.Log("Attempting to load new level: " + sceneIndx);

        LoadScene loader = FindAnyObjectByType<LoadScene>();
        if (loader != null)
        {
            loader.LoadSceneFadeScreenToOpaque(sceneIndx);
        }
        else
        {
            Debug.LogError($"LoadSceneConnector on {name} failed: No LoadScene component found in the scene to handle loading scene index {sceneIndx}.");
        }
    }
}
