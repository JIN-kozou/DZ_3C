using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.MonsterSpawn
{
    /// <summary>
    /// 关卡内怪物生成：读取 <see cref="MonsterMapSpawnConfigSO"/>，权值预算、周期生成、
    /// 基础×时间×数量权重归一化抽样，出生点按 reverse vision（<see cref="ShaderPosition"/> 揭示球）筛选。
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterSpawnManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private MonsterMapSpawnConfigSO config;

        [Header("Player")]
        [Tooltip("空则在运行时 FindObjectOfType<Player>()。")]
        [SerializeField] private Player player;

        [Header("Spawn points")]
        [Tooltip("世界空间出生点。空则尝试使用本物体下名为 SpawnPoint 的子节点 Transform。")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Behaviour")]
        [Tooltip("关卡开始后、时间轴起点（首次检测到玩家激活）立刻刷第一波。")]
        [SerializeField] private bool spawnImmediatelyOnTimelineStart = true;

        private bool _trackingReleases = true;
        private bool _timelineBegun;
        private float _timelineStartTime;
        private float _nextPeriodicSpawnTime;
        private int _currentUsedWeight;
        private int[] _aliveByEntry;
        private readonly List<Vector3> _scratchPositionsThisTick = new List<Vector3>(16);
        private readonly List<float> _scratchWeights = new List<float>(32);
        private readonly List<int> _scratchIndices = new List<int>(32);
        private Transform[] _resolvedSpawnPoints;

        /// <summary>供 <see cref="MonsterSpawnRuntimeLink"/> 判断是否在应统计销毁的上下文中。</summary>
        public bool IsTrackingReleases => _trackingReleases && isActiveAndEnabled;

        private void OnEnable()
        {
            _trackingReleases = true;
        }

        private void OnDisable()
        {
            _trackingReleases = false;
        }

        private void Awake()
        {
            ResolveSpawnPoints();
        }

        private void EnsureAliveBuffer(int entryCount)
        {
            entryCount = Mathf.Max(1, entryCount);
            if (_aliveByEntry == null || _aliveByEntry.Length < entryCount) _aliveByEntry = new int[entryCount];
        }

        private void Start()
        {
            if (player == null) player = FindObjectOfType<Player>();
        }

        private void Update()
        {
            if (config == null || config.entries == null || config.entries.Length == 0) return;

            if (!_timelineBegun)
            {
                if (player == null) player = FindObjectOfType<Player>();
                if (player == null || !player.gameObject.activeInHierarchy) return;

                _timelineBegun = true;
                _timelineStartTime = Time.time;
                _nextPeriodicSpawnTime = _timelineStartTime + Mathf.Max(0.1f, config.spawnPeriodSeconds);

                if (spawnImmediatelyOnTimelineStart) RunSpawnTick();
                return;
            }

            if (Time.time >= _nextPeriodicSpawnTime)
            {
                float period = Mathf.Max(0.1f, config.spawnPeriodSeconds);
                while (Time.time >= _nextPeriodicSpawnTime)
                    _nextPeriodicSpawnTime += period;

                RunSpawnTick();
            }
        }

        private void ResolveSpawnPoints()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                _resolvedSpawnPoints = spawnPoints;
                return;
            }

            var t = transform.Find("SpawnPoints");
            if (t == null) t = transform;

            var list = new List<Transform>();
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (c != null && c.name.Contains("SpawnPoint")) list.Add(c);
            }

            _resolvedSpawnPoints = list.Count > 0 ? list.ToArray() : System.Array.Empty<Transform>();
        }

        private float ElapsedSinceTimelineStart => _timelineBegun ? Time.time - _timelineStartTime : 0f;

        private int CurrentWaveIndex
        {
            get
            {
                float p = Mathf.Max(0.1f, config.spawnPeriodSeconds);
                return Mathf.FloorToInt(ElapsedSinceTimelineStart / p);
            }
        }

        /// <summary>由 <see cref="MonsterSpawnRuntimeLink"/> 调用。</summary>
        public void NotifyMonsterDestroyed(int entryIndex, int weightCost)
        {
            if (!IsTrackingReleases) return;
            if (config == null || entryIndex < 0 || entryIndex >= config.entries.Length) return;
            if (_aliveByEntry == null || entryIndex >= _aliveByEntry.Length) return;

            weightCost = Mathf.Max(0, weightCost);
            _currentUsedWeight = Mathf.Max(0, _currentUsedWeight - weightCost);
            _aliveByEntry[entryIndex] = Mathf.Max(0, _aliveByEntry[entryIndex] - 1);
        }

        private void RunSpawnTick()
        {
            if (config == null) return;
            EnsureAliveBuffer(config.entries.Length);
            if (_currentUsedWeight >= config.maxMonsterWeight) return;

            int k = Mathf.Max(1, config.monstersPerSpawnTick);
            int rMax = Mathf.Max(1, config.maxPickRetriesPerSpawnSlot);
            _scratchPositionsThisTick.Clear();

            for (int slot = 0; slot < k; slot++)
            {
                if (_currentUsedWeight >= config.maxMonsterWeight) break;

                bool placed = false;
                for (int attempt = 0; attempt < rMax && !placed; attempt++)
                {
                    if (!TryPickEntryIndex(out int entryIndex)) break;

                    var entry = config.entries[entryIndex];
                    if (!TryPickSpawnPosition(out Vector3 spawnPos)) break;

                    if (!CanPlaceWithMinDistance(spawnPos)) continue;

                    if (!TryInstantiate(entryIndex, entry, spawnPos)) continue;

                    _scratchPositionsThisTick.Add(spawnPos);
                    placed = true;
                }

                if (!placed) break;
            }
        }

        private bool TryPickEntryIndex(out int entryIndex)
        {
            entryIndex = -1;
            BuildNonZeroNormalizedWeights(out float sum);
            if (sum <= 0f)
            {
                if (config.logWhenAllWeightsZero)
                    Debug.LogWarning("[MonsterSpawnManager] All spawn weights are zero for this tick; skipping draw.", this);
                return false;
            }

            float roll = Random.value * sum;
            for (int i = 0; i < _scratchIndices.Count; i++)
            {
                int ei = _scratchIndices[i];
                roll -= _scratchWeights[i];
                if (roll <= 0f)
                {
                    entryIndex = ei;
                    return IsEntrySpawnableNow(ei);
                }
            }

            entryIndex = _scratchIndices[_scratchIndices.Count - 1];
            return IsEntrySpawnableNow(entryIndex);
        }

        private void BuildNonZeroNormalizedWeights(out float sum)
        {
            _scratchWeights.Clear();
            _scratchIndices.Clear();
            sum = 0f;

            int n = config.entries.Length;
            int wave = CurrentWaveIndex;

            for (int i = 0; i < n; i++)
            {
                if (!IsEntrySpawnableNow(i)) continue;

                float w = ComputeRawWeight(config.entries[i], wave, i);
                if (w <= 0f) continue;

                _scratchIndices.Add(i);
                _scratchWeights.Add(w);
                sum += w;
            }

            if (sum <= 0f) return;

            for (int i = 0; i < _scratchWeights.Count; i++)
                _scratchWeights[i] /= sum;

            sum = 1f;
        }

        private float ComputeRawWeight(MonsterSpawnEntry entry, int waveIndex, int entryIndexForAlive)
        {
            float[] arr = entry.baseWeightByWave;
            float baseW = 1f;
            if (arr != null && arr.Length > 0)
            {
                int idx = Mathf.Clamp(waveIndex, 0, arr.Length - 1);
                baseW = Mathf.Max(0f, arr[idx]);
            }

            float tMul = 1f;
            if (entry.timeWeight != null && entry.timeWeight.length > 0)
                tMul = Mathf.Max(0f, entry.timeWeight.Evaluate(ElapsedSinceTimelineStart));

            int alive = _aliveByEntry[entryIndexForAlive];
            float cMul = 1f;
            if (entry.aliveCountWeight != null && entry.aliveCountWeight.length > 0)
                cMul = Mathf.Max(0f, entry.aliveCountWeight.Evaluate(alive));

            return baseW * tMul * cMul;
        }

        private bool IsEntrySpawnableNow(int entryIndex)
        {
            var e = config.entries[entryIndex];
            if (e.prefab == null) return false;
            if (_aliveByEntry[entryIndex] >= e.maxAlive) return false;
            if (_currentUsedWeight + e.weightCost > config.maxMonsterWeight) return false;
            return true;
        }

        private bool TryInstantiate(int entryIndex, MonsterSpawnEntry entry, Vector3 position)
        {
            Quaternion rot = Quaternion.identity;
            if (player != null)
            {
                Vector3 flat = player.transform.position - position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.0001f) rot = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }

            GameObject go = Instantiate(entry.prefab, position, rot);
            var link = go.GetComponent<MonsterSpawnRuntimeLink>();
            if (link == null) link = go.AddComponent<MonsterSpawnRuntimeLink>();
            link.Initialize(this, entryIndex, entry.weightCost);

            _aliveByEntry[entryIndex]++;
            _currentUsedWeight += entry.weightCost;
            PlaySpawnAudio(go);
            return true;
        }

        private static void PlaySpawnAudio(GameObject spawnedMonster)
        {
            if (spawnedMonster == null)
            {
                return;
            }

            EnemyAudio enemyAudio = spawnedMonster.GetComponent<EnemyAudio>();
            if (enemyAudio == null)
            {
                enemyAudio = spawnedMonster.GetComponentInChildren<EnemyAudio>(true);
            }

            if (enemyAudio == null)
            {
                return;
            }

            enemyAudio.PlaySpawn();
        }

        private bool TryPickSpawnPosition(out Vector3 world)
        {
            world = default;
            if (_resolvedSpawnPoints == null || _resolvedSpawnPoints.Length == 0) return false;

            if (player == null) player = FindObjectOfType<Player>();
            Vector3 playerPos = player != null ? player.transform.position : transform.position;

            _scratchIndices.Clear();
            _scratchWeights.Clear();

            float outsideSum = 0f;
            for (int i = 0; i < _resolvedSpawnPoints.Length; i++)
            {
                Transform tr = _resolvedSpawnPoints[i];
                if (tr == null) continue;

                Vector3 p = tr.position;
                if (ShaderPosition.IsWorldPositionInsideAnyRevealSphere(p, true)) continue;

                float dist = HorizontalDistance(playerPos, p);
                float w = 1f / (config.spawnPointInverseDistanceBias + dist);
                _scratchIndices.Add(i);
                _scratchWeights.Add(w);
                outsideSum += w;
            }

            if (outsideSum > 0f)
            {
                int idx = PickWeightedSpawnIndex(outsideSum);
                world = _resolvedSpawnPoints[_scratchIndices[idx]].position;
                return true;
            }

            int valid = 0;
            for (int i = 0; i < _resolvedSpawnPoints.Length; i++)
            {
                if (_resolvedSpawnPoints[i] != null) valid++;
            }

            if (valid == 0) return false;

            int pick = Random.Range(0, valid);
            for (int i = 0; i < _resolvedSpawnPoints.Length; i++)
            {
                if (_resolvedSpawnPoints[i] == null) continue;
                if (pick-- == 0)
                {
                    world = _resolvedSpawnPoints[i].position;
                    return true;
                }
            }

            return false;
        }

        private int PickWeightedSpawnIndex(float sum)
        {
            float roll = Random.value * sum;
            for (int i = 0; i < _scratchWeights.Count; i++)
            {
                roll -= _scratchWeights[i];
                if (roll <= 0f) return i;
            }

            return _scratchWeights.Count - 1;
        }

        private bool CanPlaceWithMinDistance(Vector3 candidate)
        {
            float minD = config.minHorizontalDistanceBetweenSpawnsThisTick;
            if (minD <= 0f) return true;

            float minSq = minD * minD;
            for (int i = 0; i < _scratchPositionsThisTick.Count; i++)
            {
                if (HorizontalDistanceSq(candidate, _scratchPositionsThisTick[i]) < minSq) return false;
            }

            return true;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            ResolveSpawnPoints();
            if (_resolvedSpawnPoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _resolvedSpawnPoints.Length; i++)
            {
                if (_resolvedSpawnPoints[i] == null) continue;
                Gizmos.DrawWireSphere(_resolvedSpawnPoints[i].position, 0.35f);
            }
        }
#endif
    }
}
