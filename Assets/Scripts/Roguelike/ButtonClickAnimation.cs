using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] float squishScale = 0.88f;
    [SerializeField] float squishTime  = 0.07f;
    [SerializeField] float bounceTime  = 0.12f;

    public void OnPointerDown(PointerEventData _)
    {
        StopAllCoroutines();
        StartCoroutine(ClickRoutine());
    }

    IEnumerator ClickRoutine()
    {
        // Squeeze down
        for (float t = 0; t < squishTime; t += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(1f, squishScale, t / squishTime);
            yield return null;
        }
        transform.localScale = Vector3.one * squishScale;

        // Bounce back with slight overshoot
        for (float t = 0; t < bounceTime; t += Time.unscaledDeltaTime)
        {
            float p = t / bounceTime;
            float s = Mathf.Lerp(squishScale, 1f, p) + (1f - p) * p * 0.08f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
