using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
public class MovingEffect : MonoBehaviour
{
    public bool disableOnFinish;
    public float duration = 1f;
    public float lingerDuration = 0.5f;
    public GameObject target;
    
    public Transform startPoint;
    public Transform endPoint;
    
    Coroutine moveCoroutine;

    [Button]
    public void Move()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (target != null && !target.activeSelf)
            target.SetActive(true);

        moveCoroutine = StartCoroutine(C_Move());
    }

    public IEnumerator C_Move()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            target.transform.position = Vector3.Lerp(
                startPoint.position,
                endPoint.position,
                t
            );

            yield return null;
        }

        target.transform.position = endPoint.position;

        if(lingerDuration > 0f)
            yield return new WaitForSeconds(lingerDuration);
        
        if (disableOnFinish)
            target.SetActive(false);

        moveCoroutine = null; // mark as finished
    }
}
