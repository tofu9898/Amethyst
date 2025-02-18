using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]

    [Header("Vertical Movement Options")]
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

    [Header("Attack Settings")]
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;
    [Space(5)]

    [Header("Recoil")]
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    [SerializeField] float recoilYSpeed = 100;
    int stepsXRecoiled, stepsYRecoiled;
    [SerializeField] private float recoilYMultiplier = 1.0f;
    [Space(5)]

    [Header("Health Settings")]
    public int health;
    public int maxHealth;
    [Space(5)]


    [HideInInspector] public PlayerStateList pState;
    private Rigidbody2D rb;
    private float xAxis, yAxis;
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

        health = maxHealth;
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


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);

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
        Attack();
        Recoil();

        if (Input.GetButtonDown("Dash"))
        {
            //Debug.Log("Dash button pressed!");
        }
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-3, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(3, transform.localScale.y);
            pState.lookingRight = true;
        }

    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
        anim.SetBool("Running", rb.linearVelocity.x != 0 && Grounded());
    }

    void StartDash()
    {
        //Debug.Log("StartDash() called!"); // Debug check

        if (Input.GetButtonDown("Dash") && canDash && !pState.dashing)
        {
            //Debug.Log("Dash triggered!"); // Debug check
            StartCoroutine(Dash());
        }

        if (Grounded())
        {
            canDash = true; // Reset when grounded
        }
    }

    IEnumerator Dash()
    {
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

    void Attack()
    {
        //Debug.Log("Attack triggered: " + yAxis);

        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            // If pressing UP, use UpAttack
            if (yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingX, recoilXSpeed);
            }
            // If pressing DOWN while NOT grounded, use DownAttack
            else if (yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYSpeed);
            }
            // Default: Use Side Attack (even when grounded)
            else
            {
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
            }

        }
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        if (objectsToHit.Length > 0)
        {
            Debug.Log("Succesful hit");
            _recoilDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            if (objectsToHit[i].GetComponent<Enemy>() != null)
            {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage,
                    (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
            }
        }
    }

    void Recoil()
    {
        if (pState.recoilingX)
        {
            float xRecoil = pState.lookingRight ? -recoilXSpeed : recoilXSpeed;
            rb.linearVelocity = new Vector2(xRecoil, rb.linearVelocity.y); // Keep Y velocity unchanged!
        }


        if (pState.recoilingY)
        {
            rb.gravityScale = 0;
            float yRecoil = recoilYSpeed * recoilYMultiplier;

            // Apply smaller recoil if grounded to prevent excessive jumps
            if (Grounded()) yRecoil *= 0.3f;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, yRecoil);
        }

        else
        {
            rb.gravityScale = gravity;
        }

        if (pState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else if (stepsXRecoiled >= recoilXSteps)
        {
            StopRecoilX();
        }

        if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }

        if(Grounded())
        {
            StopRecoilY();
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;  
    }

    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

    public void TakeDamage(float _damage)
    {
        Debug.Log("Player Hit! Health: " + health);
        health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }
    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;  // Prevent taking damage again during invincibility window
        anim.SetTrigger("TakeDamage");  // Ensure this matches your Animator trigger
        ClampHealth();
        yield return new WaitForSeconds(1f);  // Invincibility time to avoid multiple hits in short time
        pState.invincible = false;
    }
    void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
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
