using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private float angleOffset = 0f;
    [SerializeField] private bool useLocalRotation = false;

    public bool CanCollideInDirection(Vector2 castDirection)
    {
        Vector2 platformDirection = Quaternion.Euler(0f, 0f, angleOffset) * Vector2.up;
        if (useLocalRotation)
        {
            platformDirection = transform.localRotation * platformDirection;
        }
        return Vector2.Dot(castDirection, platformDirection) < 0f;
    }

    // TODO: Convert to Handles-based code
    private void OnDrawGizmos()
    {
        var collider2d = GetComponent<Collider2D>();
        if (collider2d != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 platformDirection = Quaternion.Euler(0f, 0f, angleOffset) * Vector2.up;
            if (useLocalRotation)
            {
                platformDirection = transform.localRotation * platformDirection;
            }
            var bounds = collider2d.bounds;

            // Place gizmo somewhere at the top of the overall collider shape
            var lineStart = collider2d.ClosestPoint(bounds.center + new Vector3(0f, bounds.max.y));

            Vector2 lineEnd = lineStart + platformDirection * 16f;
            Gizmos.DrawLine(lineStart, lineEnd);
            Gizmos.DrawCube(lineEnd, Vector3.one * 3f);
        }
    }
}