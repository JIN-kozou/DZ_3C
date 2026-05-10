using UnityEngine;

namespace DZ_3C.MonsterSpawn
{
    /// <summary>
    /// 单张地图的怪物周期生成配置（每张地图一个 SO，由关卡内 <see cref="MonsterSpawnManager"/> 读取）。
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterMapSpawnConfig", menuName = "DZ_3C/Monster Spawn/Map Spawn Config", order = 0)]
    public class MonsterMapSpawnConfigSO : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("周期 X（秒）：波次 n = floor(自首次出生后经过时间 / X)，且每隔 X 秒尝试生成一次（开局另有一次）。")]
        [Min(0.1f)]
        public float spawnPeriodSeconds = 8f;

        [Tooltip("每次周期尝试生成的怪物只数 K（受权值与存活上限约束，可能少于 K）。")]
        [Min(1)]
        public int monstersPerSpawnTick = 3;

        [Header("Weights & retries")]
        [Tooltip("本图怪物权值上限；当前占用达到后不再生成。")]
        [Min(1)]
        public int maxMonsterWeight = 50;

        [Tooltip("单次抽取类型时，命中非法（存活已满或权值不够）的最大重试次数；超过则中止本周期剩余配额。")]
        [Min(1)]
        public int maxPickRetriesPerSpawnSlot = 48;

        [Header("Placement")]
        [Tooltip("同一周期内多次落点之间的最小水平距离（XZ）。")]
        [Min(0f)]
        public float minHorizontalDistanceBetweenSpawnsThisTick = 2.5f;

        [Tooltip("出生点加权随机：权重 ∝ 1 / (distanceBias + 到玩家的水平距离)。越大越均匀。")]
        [Min(0.01f)]
        public float spawnPointInverseDistanceBias = 0.75f;

        [Header("Debug")]
        public bool logWhenAllWeightsZero = true;

        [Header("Entries")]
        public MonsterSpawnEntry[] entries;
    }

    [System.Serializable]
    public class MonsterSpawnEntry
    {
        [Tooltip("仅用于 Inspector 区分。")]
        public string debugName;

        public GameObject prefab;

        [Tooltip("生成一只该怪占用的权值；死亡时释放。")]
        [Min(1)]
        public int weightCost = 1;

        [Tooltip("场上该 prefab 同时存活上限（死亡后计数下降，可再刷）。")]
        [Min(0)]
        public int maxAlive = 6;

        [Tooltip("波次 n = floor(自首次出生后经过时间 / spawnPeriod)。索引越界时使用最后一项。")]
        public float[] baseWeightByWave = new float[] { 1f };

        [Tooltip("时间修正：横轴 = 关卡内自角色第一次出生后经过的秒数（死亡再出生不重置）。")]
        public AnimationCurve timeWeight = AnimationCurve.Constant(0f, 3600f, 1f);

        [Tooltip("数量修正：横轴 = 当前场上该类型存活数量。")]
        public AnimationCurve aliveCountWeight = AnimationCurve.Constant(0f, 32f, 1f);
    }
}
