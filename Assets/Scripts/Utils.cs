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
                DebugUtils.DrawArrow(hit.point, hit.point + (hit.normal * 4f), 2f, Color.magenta);
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
    }
}