using System.Collections;
using UnityEngine;

public class MovingBox : MonoBehaviour
{
    [SerializeField] private Vector3 raisedPosition; 
    [SerializeField] private Vector3 loweredPosition; 
    [SerializeField] private float moveSpeed = 2f;

    private Coroutine moveCoroutine;

    public void MoveUp()
    {
        StartMove(raisedPosition);
    }

    public void MoveDown()
    {
        StartMove(loweredPosition);
    }

    private void StartMove(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveTo(targetPosition));
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
    }
}