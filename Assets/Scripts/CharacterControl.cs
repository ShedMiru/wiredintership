using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControl : MonoBehaviour
{
    private Rigidbody2D rb2d;
    private float h;
    private float v;
    private bool isInteracting = false;
    private Vector2 move;
    private Vector2 lastMoveDir = Vector2.down;
    private Animator anim;
    private RectTransform rect;

    public float speed = 10f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private IInteractable currentInteractable;

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isInteracting)
        {
            move = Vector2.zero;
            SetAnim(Vector2.zero);
            return;
        }

        // movement
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        move = new Vector2(h, v);
        if (move.magnitude > 1)
            move.Normalize();

        // interact
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            move = Vector2.zero;
            rb2d.velocity = Vector2.zero;
            SetAnim(Vector2.zero);

            isInteracting = true;
            currentInteractable.Interact(gameObject);
        }
        SetAnim(move);
    }

    void FixedUpdate()
    {
        float currentSpeed = (v != 0f) ? 7f : 10f; // your logic, but avoid changing speed field
        rb2d.velocity = move * currentSpeed;
    }

    private void SetAnim(Vector2 dir)
    {
        if (anim == null) return;

        bool moving = dir.sqrMagnitude > 0.001f;

        anim.SetBool("IsMoving", moving);
        anim.SetFloat("MoveX", dir.x);
        anim.SetFloat("MoveY", dir.y);

        if (moving)
        {
            lastMoveDir = dir;
            anim.SetFloat("LastX", lastMoveDir.x);
            anim.SetFloat("LastY", lastMoveDir.y);
        }

        if (Mathf.Abs(dir.x) > 0.01f)
        {
            Vector3 s = rect.localScale;
            s.x = dir.x < 0 ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
            rect.localScale = s;
        }
    }

    public void EndInteract()
    {
        isInteracting = false;
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
