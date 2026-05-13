using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

/// <summary>
/// Physical bullet: hit detection via trigger and collision callbacks (no hitscan ray).
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
    private bool _infiniteLifetime;
    private Collider[] _ownerColliders;
    private Transform _ownerRoot;
    private string[] _damageableTags;
    private string _hurtBuffId;
    private bool _destroyOnHit;
    private float _maxHitDistanceSqr;

    /// <summary>their Collider instance id -> receiver root GameObject instance id</summary>
    private readonly Dictionary<int, int> _otherColliderToReceiverKey = new Dictionary<int, int>();

    /// <summary>receiver root instance id -> how many of their colliders are overlapping (valid hit)</summary>
    private readonly Dictionary<int, int> _receiverOverlapDepth = new Dictionary<int, int>();

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
        float lifetime,
        string[] damageableTags,
        string hurtBuffId,
        bool destroyOnHit,
        float maxHitDistance)
    {
        _ownerRoot = ownerRoot;
        _ownerColliders = ownerColliders;
        _damage = damage;
        _gravityScale = gravityScale;
        _damageableTags = damageableTags;
        _hurtBuffId = hurtBuffId ?? string.Empty;
        _destroyOnHit = destroyOnHit;
        var maxDist = Mathf.Max(0.01f, maxHitDistance);
        _maxHitDistanceSqr = maxDist * maxDist;

        _infiniteLifetime = lifetime <= 0f;
        _despawnAt = _infiniteLifetime ? float.PositiveInfinity : Time.time + lifetime;

        _otherColliderToReceiverKey.Clear();
        _receiverOverlapDepth.Clear();

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
        if (!_infiniteLifetime && Time.time >= _despawnAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        NotifyOverlapEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        NotifyOverlapExit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null)
        {
            NotifyOverlapEnter(collision.collider);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider != null)
        {
            NotifyOverlapExit(collision.collider);
        }
    }

    private void NotifyOverlapEnter(Collider other)
    {
        if (IsOwnerHierarchy(other))
        {
            return;
        }

        if (!TryResolveValidHit(other, useDistanceFilter: true, out var receiver))
        {
            return;
        }

        if (receiver is not Component receiverComponent)
        {
            return;
        }

        var otherId = other.GetInstanceID();
        if (_otherColliderToReceiverKey.ContainsKey(otherId))
        {
            return;
        }

        var receiverKey = receiverComponent.gameObject.GetInstanceID();
        _otherColliderToReceiverKey[otherId] = receiverKey;

        if (!_receiverOverlapDepth.TryGetValue(receiverKey, out var depth))
        {
            depth = 0;
        }

        if (depth == 0)
        {
            // 使用玩家根物体作为 attacker，便于怪物 HitPerceptor 解析 AITargetable 并写入 Attackers 仇恨桶（弹丸自身通常无 AITargetable）。
            object attackerForAggro = _ownerRoot != null ? _ownerRoot.gameObject : gameObject;
            receiver.ReceiveAIDamage(_damage, _hurtBuffId, attackerForAggro);
            if (_ownerRoot != null && _ownerRoot.GetComponentInParent<Player>() != null)
            {
                WeaponHitHudSignal.RaisePlayerDealtDamageToReceiver();
            }

            if (_destroyOnHit)
            {
                Destroy(gameObject);
                return;
            }
        }

        _receiverOverlapDepth[receiverKey] = depth + 1;
    }

    private void NotifyOverlapExit(Collider other)
    {
        if (IsOwnerHierarchy(other))
        {
            return;
        }

        var otherId = other.GetInstanceID();
        if (!_otherColliderToReceiverKey.TryGetValue(otherId, out var receiverKey))
        {
            return;
        }

        _otherColliderToReceiverKey.Remove(otherId);

        if (!_receiverOverlapDepth.TryGetValue(receiverKey, out var depth))
        {
            return;
        }

        depth--;
        if (depth <= 0)
        {
            _receiverOverlapDepth.Remove(receiverKey);
        }
        else
        {
            _receiverOverlapDepth[receiverKey] = depth;
        }
    }

    private bool TryResolveValidHit(Collider other, bool useDistanceFilter, out IAIHurtReceiver receiver)
    {
        receiver = null;
        if (other == null || _damage <= 0f)
        {
            return false;
        }

        if (!PassesTagFilter(other))
        {
            return false;
        }

        if (useDistanceFilter && !PassesProximityFilter(other))
        {
            return false;
        }

        receiver = FindHurtReceiver(other);
        return receiver != null;
    }

    private bool PassesTagFilter(Collider other)
    {
        if (_damageableTags == null || _damageableTags.Length == 0)
        {
            return false;
        }

        for (var t = other.transform; t != null; t = t.parent)
        {
            for (var i = 0; i < _damageableTags.Length; i++)
            {
                var tag = _damageableTags[i];
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                if (t.CompareTag(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool PassesProximityFilter(Collider other)
    {
        var closest = other.ClosestPoint(transform.position);
        return (closest - transform.position).sqrMagnitude <= _maxHitDistanceSqr;
    }

    private static IAIHurtReceiver FindHurtReceiver(Collider other)
    {
        var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IAIHurtReceiver hr)
            {
                return hr;
            }
        }

        return null;
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
