using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))] // ✅ ensure it exists
public class SplineAproacher : MonoBehaviour
{
    public bool inuse;
    
    public SplineAnimate splineAnimate;
    public bool passed = false;
    public float myProgCompletePosition = 1f;

    public SpriteRenderer[] sprites;
    
    public Vector3 resetScale;

    public float prog;

    // --- ADDED ---
    public int lineResolution = 50;
    private SplineContainer splineContainer;
    private LineRenderer lineRenderer;
    // --- END ADDED ---
    
    void OnEnable()
    {
        passed = false;
        if(sprites == null) sprites = new SpriteRenderer[0];

        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if (splineAnimate != null) splineContainer = splineAnimate.Container;

        GenerateLine();
    }
    
    void Update()
    {
        
        if (!inuse) return;

        prog = splineAnimate.NormalizedTime;

        passed = splineAnimate.ElapsedTime >= myProgCompletePosition + (myProgCompletePosition * 0.2f);

        if (passed)
        {
            SetLineAlpha(0f);
            foreach (var sprite in sprites)
            {
                Color c = sprite.color;
                c.a = 1 - prog - (prog * 0.2f); 
                sprite.color = c;
            }
            return;
        }
        else
        {
            GenerateLine();
            
            SetLineAlpha(1f);
            foreach (var sprite in sprites)
            {
                Color c = sprite.color;
                c.a = 1;
                sprite.color = c;
            }
        }

    }

    // --- ADDED ---
    [Button]
    public void GenerateLine()
    {
        if (splineAnimate == null || splineAnimate.Container == null) return;

        splineContainer = splineAnimate.Container;

        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) return;

        lineRenderer.positionCount = lineResolution;

        for (int i = 0; i < lineResolution; i++)
        {
            float t = i / (float)(lineResolution - 1);
            Vector3 pos = splineContainer.EvaluatePosition(t);
            lineRenderer.SetPosition(i, pos);
        }
    }
    // --- END ADDED ---

    void SetLineAlpha(float alpha)
    {
        if (lineRenderer == null) return;

        Color c1 = lineRenderer.startColor;
        Color c2 = lineRenderer.endColor;

        c1.a = alpha;
        c2.a = alpha;

        lineRenderer.startColor = c1;
        lineRenderer.endColor = c2;
    }
    
}