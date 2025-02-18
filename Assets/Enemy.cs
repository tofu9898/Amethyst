using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float health = 25f;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;
    [SerializeField] protected PlayerController player;
    [SerializeField] protected float speed;
    [SerializeField] protected float damage;

    protected float recoilTimer;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = PlayerController.Instance;
        if (health <= 0)
        {
            Debug.LogWarning(gameObject.name + " started with 0 health! Setting default health to 25.");
            health = 25f;
        }
    }

    protected virtual void Update()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
        }
        if (isRecoiling)
        {
            if (recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;
        if (!isRecoiling)
        {
            rb.AddForce(-_hitForce * recoilFactor * _hitDirection);
        }
    }

    // Use OnTriggerEnter2D to apply damage once on collision entry.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the colliding object is the player and that the player is not invincible.
        if (collision.gameObject.CompareTag("Player") && !PlayerController.Instance.pState.invincible)
        {
            Debug.Log("Enemy collided with Player via collision.");
            Attack();
        }
    }


    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(damage);
    }
}
