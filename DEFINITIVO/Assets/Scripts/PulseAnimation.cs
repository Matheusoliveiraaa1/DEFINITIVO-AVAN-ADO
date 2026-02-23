using UnityEngine;

public class PulseAnimation : MonoBehaviour
{
    public float speed = 2f;
    public float scaleAmount = 0.08f;

    private Vector3 originalScale;

    void OnEnable()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = originalScale * scale;
    }

    void OnDisable()
    {
        transform.localScale = originalScale;
    }
}