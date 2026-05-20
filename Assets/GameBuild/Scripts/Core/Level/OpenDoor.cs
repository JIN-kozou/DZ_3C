using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    private Vector3 targetPos;
    public float moveSpeed = 2f;
    public List<AppearRevealGate> gate;

    [Header("true = 任意一个满足即可开门")]
    public bool openWithAnyOne = false;

    private Vector3 startPos;
    private Coroutine moveCoroutine;

    private void Start()
    {
        startPos = transform.localPosition;
        targetPos = new Vector3(transform.localPosition.x - 3, transform.localPosition.y, transform.localPosition.z);
    }

    public void CheckAllFinish()
    {
        bool shouldOpen = openWithAnyOne ? CheckAnyOne() : CheckAll();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveDoor(shouldOpen ? targetPos : startPos));
    }

    bool CheckAll()
    {
        foreach (var g in gate)
        {
            if (g.revealAlongU <= 0.1f)
            {
                return false;
            }
        }

        return true;
    }

    bool CheckAnyOne()
    {
        foreach (var g in gate)
        {
            if (g.revealAlongU == 1)
            {
                return true;
            }
        }

        return false;
    }

    IEnumerator MoveDoor(Vector3 target)
    {
        while (Vector3.Distance(transform.localPosition, target) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.localPosition = target;
    }
}