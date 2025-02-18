using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]


    [SerializeField] private float jumpForce = 30;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;
    [Space(5)]
    

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]
    

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [Space(5)]


    PlayerStateList pState;
    private Rigidbody2D rb;
    private float xAxis;
    private float gravity;
    Animator anim;
    private bool canDash;
    private bool dashed;


    public static PlayerController Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pState = GetComponent<PlayerStateList>();

        rb = GetComponent<Rigidbody2D>();

        anim = GetComponent<Animator>();

        gravity = rb.gravityScale;

        canDash = true;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpingVariables();

        if (pState.dashing) return;
        Flip();
        Move();
        Jump();
        StartDash();

        if (Input.GetButtonDown("Dash"))
        {
            Debug.Log("Dash button pressed!");
        }

        Debug.Log("Grounded: " + Grounded());
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-3, transform.localScale.y);
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(3, transform.localScale.y);
        }

    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
        anim.SetBool("Running", rb.linearVelocity.x != 0 && Grounded());
    }

    void StartDash()
    {
        Debug.Log("StartDash() called!"); // Debug check

        if (Input.GetButtonDown("Dash") && canDash && !pState.dashing)
        {
            Debug.Log("Dash triggered!"); // Debug check
            StartCoroutine(Dash());
        }

        if (Grounded())
        {
            canDash = true; // Reset when grounded
        }
    }

    IEnumerator Dash()
    {
        Debug.Log("DashingStart");
        canDash = false;
        dashed = true;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;

    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            pState.jumping = false;
        }

        // Check jump buffer AND grounded status
        if (!pState.jumping && jumpBufferCounter > 0 && Grounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            pState.jumping = true;
            jumpBufferCounter = 0;  // Reset buffer after using it
        }

        // Reset jumping when grounded
        if (Grounded() && pState.jumping)
        {
            pState.jumping = false;
        }

        anim.SetBool("Jumping", !Grounded());
    }


    void UpdateJumpingVariables()
    {
        if (Grounded())
        {
            pState.jumping = false;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else if (jumpBufferCounter > 0)
        {
            jumpBufferCounter--;
        }
    }

}
