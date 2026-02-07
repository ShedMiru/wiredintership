using UnityEngine;
using UnityEngine.UI;

public class AutoErrorFeedback : MonoBehaviour
{
    private Outline outline;
    private bool isError = false;
    private float blinkSpeed = 4f;

    public void SetError(bool errorState)
    {
        isError = errorState;

        // Lazy initialization: Buat Outline jika belum ada
        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();

            outline.effectDistance = new Vector2(5, -5); // Ketebalan outline
            outline.effectColor = Color.red;
        }

        outline.enabled = isError;
    }

    private void Update()
    {
        if (isError && outline != null)
        {
            // Efek Kedip (PingPong Alpha)
            float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            // Kita mainkan alpha color-nya saja
            Color c = outline.effectColor;
            c.a = 0.3f + (alpha * 0.7f); // Minimal 0.3, Maksimal 1.0
            outline.effectColor = c;
        }
    }
}