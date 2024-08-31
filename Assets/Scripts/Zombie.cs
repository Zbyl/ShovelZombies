using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    private Transform target;
    float minimalDistanceToTarget = 2.0f;      /// We won't come closer to target than this.
    public float minimalDistanceToTargetDefault = 2.0f;      /// We won't come closer to target than this.
    public float minimalDistanceToTargetForCowards = 3.0f;      /// Cowardly Zombies won't come closer to target than this.
    public float cowardProbability = 0.5f;      /// How likely it is to have a cowardly Zombie. It will have effecting minimalDistanceToTarget a bit random.
    private NavMeshAgent navMeshAgent;

    private Vector3 currentPushForce = Vector3.zero;   /// Force used to simulate Zombie being pushed.
    public float pushForceDecay = 0.01f;              /// How quickly pushForce goes down to zero.
    public float pushForce = 0.7f;                    /// Strength of a push.

    public float health = 50.0f;

    public float stunTime = 3.0f;
    private float stunTimer = 0.0f;
    private bool isStunned = false;

    public Transform animatedMesh;  /// Must be set to the mesh with Animator component.
    private Animator animator;

    private AudioSource footsteps;
    private AudioSource waveSound;
    private AudioSource[] painSounds;

    public GameObject hitParticlesPrefab; // Prefab for hit particles.

    // Zombie AI
    // If Zombie is within superCloseRadius it walks straight to the player.
    // If Zombie is within farRadius it walks to a random position on closeRadius, within approachAngle.
    // Otherwise zombie walks in random direction for a random distance within randomRadius.
    private float superCloseRadius = 7.0f;
    private float closeRadius = 6.0f;
    private float farRadius = 20.0f;
    private float randomRadius = 10.0f;
    private float approachAngle = 75.0f; // In degrees.

    private bool showDebugTarget = false;
    public GameObject debugTargetPrefab;
    private GameObject debugTarget;

    enum ZombieState
    {
        RandomWalk,
        Approach,
        Direct,
    }
    private ZombieState zombieState = ZombieState.Direct;
    private Vector3 zombieTarget = Vector3.zero;

    void ZombieAI()
    {
        var dirToPlayer = (target.position - transform.position).normalized;
        var distToPlayer = (target.position - transform.position).magnitude;
        var dirToCurrentTarget = (zombieTarget - transform.position).normalized;
        var distToCurrentTarget = (zombieTarget - transform.position).magnitude;

        var desiredState = ZombieState.RandomWalk;
        if (distToPlayer < farRadius)
        {
            desiredState = ZombieState.Approach;
        }
        if (distToPlayer < superCloseRadius)
        {
            desiredState = ZombieState.Direct;
        }

        var lookForNewTarget = false;
        if ((zombieState == desiredState) && (distToCurrentTarget < 1.0f))
        {
            //Debug.Log($"Looking for new target for {zombieState}");
            lookForNewTarget = true;
        }

        if ((desiredState != zombieState) || lookForNewTarget)
        {
            if (desiredState == ZombieState.RandomWalk)
            {
                var angle = Random.Range(0.0f, 360.0f);
                var offset = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.forward * randomRadius;
                zombieTarget = transform.position + offset;
            }
            if (desiredState == ZombieState.Approach)
            {
                var angle = Random.Range(-approachAngle, approachAngle);
                var offset = Quaternion.Euler(0.0f, angle, 0.0f) * -dirToPlayer * closeRadius;
                zombieTarget = target.position + offset;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(zombieTarget, out hit, 100.0f, NavMesh.AllAreas))
            {
                zombieTarget = hit.position;
            }
            else
            {
                //Debug.Log($"Cannot find target on navmesh.");
            }

            //Debug.Log($"Switching from {zombieState} to {desiredState}. Target: {zombieTarget}");
            zombieState = desiredState;
        }

        if (zombieState == ZombieState.Direct)
        {
            zombieTarget = target.position - dirToPlayer.normalized * minimalDistanceToTarget;
        }

        if (showDebugTarget)
        {
            debugTarget.transform.position = zombieTarget;
        }
    }

    void Awake()
    {
        footsteps = transform.Find("Footsteps").GetComponent<AudioSource>();
        waveSound = transform.Find("WaveSound").GetComponent<AudioSource>();
        painSounds = transform.Find("PainSounds").GetComponentsInChildren<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = animatedMesh.GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        Transform player_target = GameObject.Find("Player").transform;
        target = player_target;
        if (Random.Range(0.0f, 1.0f) <= cowardProbability)
        {
            minimalDistanceToTarget = Random.Range(minimalDistanceToTargetDefault, minimalDistanceToTargetForCowards);
        }
        else
        {
            minimalDistanceToTarget = minimalDistanceToTargetDefault;
        }

        if (showDebugTarget)
        {
            debugTarget = Instantiate(debugTargetPrefab, transform.position, transform.rotation, transform.parent);
        }
    }

    void FixedUpdate()
    {
        if (GameState.Instance.isPaused)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        ZombieAI();
        navMeshAgent.destination = zombieTarget;
        navMeshAgent.isStopped = isStunned;

        var wasWalking = animator.GetBool("Walking");
        var isWalking = (zombieTarget - transform.position).magnitude > 0.1f;
        animator.SetBool("Walking", isWalking);
        if (wasWalking != isWalking)
        {
            //Debug.Log($"Walking: {isWalking}");
            if (isWalking)
            {
                footsteps.Play();
                footsteps.time = Random.Range(0.0f, footsteps.clip.length);
            }
            else
            {
                footsteps.Stop();
            }
        }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                isStunned = false;
            }
        }
        animator.SetBool("Stunned", isStunned);
        transform.position += currentPushForce;
        currentPushForce = Vector3.MoveTowards(currentPushForce, Vector3.zero, pushForceDecay);

        GameState.Instance.ZombieSoundTick(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Zombie"))
        {
            var collisionPoint = collision.contacts[0].point;
            collision.gameObject.GetComponent<Zombie>().TakeHit(0.0f, collisionPoint, collisionPoint - transform.position, false);
        }
        else if (collision.gameObject.CompareTag("DieArea"))
        {
            Grave grave = collision.gameObject.GetComponentInParent<Grave>();
            if (grave.isDeadly)
            {
                grave.CloseGrave();
                Die();
            }
        }
    }

    public void TakeHit(float damage, Vector3 pushPosition, Vector3 pushDirection, bool waveHit)
    {
        var q = new Quaternion();
        q.SetFromToRotation(Vector3.up, -pushDirection.normalized);
        var hitParticles = Instantiate(hitParticlesPrefab, pushPosition, q);

        if (waveHit)
        {
            waveSound.Play();
        }
        else
        {
            painSounds[Random.Range(0, painSounds.Length)].Play();
        }

        if (!isStunned)
        {
            isStunned = true;
            stunTimer = stunTime;
            Debug.Log($"Zombie {this.GetHashCode()} is stunned");
            animator.SetTrigger("StunTrigger");
        }

        health -= damage;
        Debug.Log($"Damage taken: {damage} {this.GetHashCode()}");
        currentPushForce = pushDirection.normalized * pushForce;
        animator.SetTrigger("Hit");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Add death effects, animations, etc.
        Destroy(gameObject);
    }
}
