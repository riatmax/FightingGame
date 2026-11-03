using System.Collections;
using UnityEngine;

public class PopIn : MonoBehaviour
{
    /// <summary>
    ///
    ///     Makes UI pop in by starting big and snapping down to its normal size.
    ///     Plays when the object turns on, or call Play to pop it again
    ///
    /// </summary>



    [SerializeField] private float startScale = 1.8f;
    [SerializeField] private float duration = 0.15f;

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float scale = Mathf.Lerp(startScale, 1f, time / duration);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }
}
