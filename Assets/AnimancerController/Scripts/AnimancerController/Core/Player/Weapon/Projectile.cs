using UnityEngine;

/// <summary>
/// Physical bullet: hit detection via collision callbacks only (no hitscan ray).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float defaultMuzzleSpeed = 60f;
    [SerializeField] private float defaultGravityScale = 1f;
    [SerializeField] private float defaultLifetime = 6f;

    private Rigidbody _rb;
    private float _damage;
    private float _gravityScale;
    private float _despawnAt;
    private Collider[] _ownerColliders;
    private Transform _ownerRoot;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(
        Transform ownerRoot,
        Collider[] ownerColliders,
        Vector3 worldVelocity,
        float damage,
        float gravityScale,
        float lifetime)
    {
        _ownerRoot = ownerRoot;
        _ownerColliders = ownerColliders;
        _damage = damage;
        _gravityScale = gravityScale;
        _despawnAt = Time.time + lifetime;
        _rb.velocity = worldVelocity;

        if (_ownerColliders != null)
        {
            var mine = GetComponents<Collider>();
            for (var i = 0; i < mine.Length; i++)
            {
                for (var j = 0; j < _ownerColliders.Length; j++)
                {
                    if (_ownerColliders[j] != null)
                    {
                        Physics.IgnoreCollision(mine[i], _ownerColliders[j], true);
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        _rb.velocity += Physics.gravity * (_gravityScale * Time.fixedDeltaTime);
        if (Time.time >= _despawnAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsOwnerHierarchy(collision.collider))
        {
            return;
        }

        Debug.Log($"Projectile hit {collision.collider.name} damage={_damage:F1}", collision.collider);
        Destroy(gameObject);
    }

    private bool IsOwnerHierarchy(Collider c)
    {
        if (_ownerRoot == null || c == null)
        {
            return false;
        }

        return c.transform == _ownerRoot || c.transform.IsChildOf(_ownerRoot);
    }
}
