using UnityEngine;

namespace DZ_3C.MonsterSpawn
{
    /// <summary>
    /// 挂在生成的怪物 prefab 上（或由管理器在 Instantiate 后 AddComponent），
    /// 在销毁时向 <see cref="MonsterSpawnManager"/> 归还权值并减少存活计数。
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterSpawnRuntimeLink : MonoBehaviour
    {
        private MonsterSpawnManager _manager;
        private int _entryIndex;
        private int _weightCost;
        private bool _released;

        public void Initialize(MonsterSpawnManager manager, int entryIndex, int weightCost)
        {
            _manager = manager;
            _entryIndex = entryIndex;
            _weightCost = weightCost;
            _released = false;
        }

        private void OnDestroy()
        {
            if (_released) return;
            if (_manager == null) return;
            if (!_manager.IsTrackingReleases) return;

            _released = true;
            _manager.NotifyMonsterDestroyed(_entryIndex, _weightCost);
        }
    }
}
