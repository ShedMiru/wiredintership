using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControl : MonoBehaviour
{
    private Rigidbody2D rb2d;
    private float h;
    private float v;

    public float speed = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private IInteractable currentInteractable;

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // movement
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        Vector2 move = new Vector2(h, v);
        if (move.magnitude > 1)
            move.Normalize();

        rb2d.velocity = move * speed;

        // interact
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            rb2d.velocity = Vector2.zero;
            currentInteractable.Interact(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            currentInteractable = interactable;

            if (interactable is UIInteractable uiInteractable)
                uiInteractable.ShowPrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentInteractable != null && other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (ReferenceEquals(interactable, currentInteractable))
            {
                if (interactable is UIInteractable uiInteractable)
                    uiInteractable.ShowPrompt(false);

                currentInteractable = null;
            }
        }
    }
}
