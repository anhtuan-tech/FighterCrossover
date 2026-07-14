using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    private SpriteRenderer sr;
    private List<Sprite> frames;
    private float fps;
    private bool loop;
    private bool destroyOnEnd;

    public void Setup(SpriteRenderer sr, List<Sprite> frames, float fps, bool loop, bool destroyOnEnd)
    {
        this.sr = sr;
        this.frames = frames;
        this.fps = fps;
        this.loop = loop;
        this.destroyOnEnd = destroyOnEnd;

        if (sr != null && frames != null && frames.Count > 0)
        {
            StartCoroutine(AnimateRoutine());
        }
    }

    private IEnumerator AnimateRoutine()
    {
        float timeStep = 1f / fps;
        int currentFrame = 0;

        while (true)
        {
            if (sr == null) yield break;
            sr.sprite = frames[currentFrame];
            yield return new WaitForSeconds(timeStep);
            
            currentFrame++;
            if (currentFrame >= frames.Count)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    if (destroyOnEnd)
                    {
                        Destroy(gameObject);
                    }
                    break;
                }
            }
        }
    }
}
