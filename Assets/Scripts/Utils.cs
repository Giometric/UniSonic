using UnityEngine;
using UnityEngine.Tilemaps;

namespace Giometric.UniSonic
{
    public static class Utils
    {
        public static GroundTile GetGroundTile(RaycastHit2D hit, out Matrix4x4 tileTransform, bool showDebug = false)
        {
            GroundTile groundTile = null;
            var tilemap = hit.collider.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // Use a world position that is dug into the collision just a bit, to avoid sampling from the edges of tiles
                Vector3 checkWorldPos = hit.point + (hit.normal * (tilemap.cellSize * -0.1f));
                groundTile = GetGroundTile(tilemap, checkWorldPos, out tileTransform, showDebug);
                return groundTile;
            }
            else
            {
                tileTransform = Matrix4x4.identity;
            }
            return groundTile;
        }

        // Note: The tile anchor setting on the Tilemap must be the default value of (0.5, 0.5, 0) or this method will find the wrong tile!
        // Also, it is best to use a worldPosition that is not right on the edge of tiles, or sometimes the wrong cell is selected
        public static GroundTile GetGroundTile(Tilemap tilemap, Vector3 worldPosition, out Matrix4x4 tileTransform, bool showDebug = false)
        {
            Vector3Int tilePos = tilemap.WorldToCell(worldPosition);
            var groundTile = tilemap.GetTile<GroundTile>(tilePos);
            if (showDebug)
            {
                var worldTileCenter = tilemap.GetCellCenterWorld(tilePos);
                Debug.DrawLine(worldPosition, worldTileCenter, Color.green, 0f, false);
            }
            tileTransform = tilemap.GetTransformMatrix(tilePos);
            return groundTile;
        }

        private static RaycastHit2D[] hitResultsCache = new RaycastHit2D[10];

        public static bool GroundRaycast(Vector2 castStart, Vector2 dir, float distance, ContactFilter2D filter,
            float minValidDistance, bool ignoreOneWayPlatforms, out RaycastHit2D resultHit, bool showDebug = false)
        {
            resultHit = new RaycastHit2D();
            int hitCount = Physics2D.Raycast(castStart, dir, filter, hitResultsCache, distance);

            // Physics2D.Raycast results should be sorted by distance, so find the first valid result
            for (int i = 0; i < hitCount; ++i)
            {
                var hit = hitResultsCache[i];

                if (hit.distance < minValidDistance) // TODO: Should this be direct comparison of hit.y to castStart.y?
                {
                    // The hit is not within the valid distance range - it's outside of our step-up limit
                    continue;
                }

                var platform = hit.collider.GetComponent<OneWayPlatform>();
                if (platform != null && (ignoreOneWayPlatforms || !platform.CanCollideInDirection(Vector2.down)))
                {
                    continue;
                }

                if (ignoreOneWayPlatforms)
                {
                    var groundTile = Utils.GetGroundTile(hit, out var tileTransform, showDebug);
                    if (groundTile != null && groundTile.IsOneWayPlatform) // TODO: Also check angle?
                    {
                        continue;
                    }
                }

                resultHit = hit;
                return true;
            }
            return false;
        }
    }
}