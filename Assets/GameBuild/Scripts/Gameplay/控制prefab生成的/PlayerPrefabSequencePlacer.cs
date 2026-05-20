using UnityEngine;

public class PlayerPrefabSequencePlacer : MonoBehaviour
{
    [Header("Prefab 设置")]
    public GameObject prefabToSpawn;

    [Header("最多可生成数量")]
    public int maxPrefabCount = 3;

    [Header("生成位置偏移")]
    public Vector3 spawnOffset = Vector3.zero;

    private GameObject[] spawnedPrefabs;

    void Awake()
    {
        if (maxPrefabCount <= 0)
        {
            maxPrefabCount = 3;
        }

        spawnedPrefabs = new GameObject[maxPrefabCount];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnPrefab();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DeleteLastPrefab();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DeletePrefabByIndex(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            DeletePrefabByIndex(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DeletePrefabByIndex(2);
        }
    }

    void SpawnPrefab()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("没有指定 prefabToSpawn！");
            return;
        }

        int emptyIndex = FindFirstEmptySlot();

        if (emptyIndex == -1)
        {
            Debug.Log("摆放的序列已满，请删除后再生成序列。");
            return;
        }

        Vector3 spawnPosition = //这里用local坐标，保证一直生成在玩家面前
    transform.position
    + transform.forward * spawnOffset.z
    + transform.right * spawnOffset.x
    + transform.up * spawnOffset.y;

        GameObject newPrefab = Instantiate(
            prefabToSpawn,
            spawnPosition,
            transform.rotation
        );

        spawnedPrefabs[emptyIndex] = newPrefab;

        Debug.Log("这是第 " + (emptyIndex + 1) + " 个 prefab。");
    }

    void DeletePrefabByIndex(int index)
    {
        if (index < 0 || index >= spawnedPrefabs.Length)
        {
            return;
        }

        if (spawnedPrefabs[index] == null)
        {
            return;
        }

        Destroy(spawnedPrefabs[index]);
        spawnedPrefabs[index] = null;

        Debug.Log("已收回第 " + (index + 1) + " 个 prefab。");
    }

    void DeleteLastPrefab()
    {
        for (int i = spawnedPrefabs.Length - 1; i >= 0; i--)
        {
            if (spawnedPrefabs[i] != null)
            {
                Destroy(spawnedPrefabs[i]);
                spawnedPrefabs[i] = null;

                Debug.Log("已删除当前最后一个 prefab：第 " + (i + 1) + " 个。");
                return;
            }
        }
    }

    int FindFirstEmptySlot()
    {
        for (int i = 0; i < spawnedPrefabs.Length; i++)
        {
            if (spawnedPrefabs[i] == null)
            {
                return i;
            }
        }

        return -1;
    }
}