using UnityEngine;
using UnityEngine.Tilemaps;

namespace Giometric.UniSonic
{
    [CreateAssetMenu(menuName = "UniSonic/Ground Tile", fileName = "GroundTile")]
    public class GroundTile : Tile
    {
        [SerializeField] private bool useFixedGroundAngle = true;

        /// <Summary>
        /// If true, when the character performs ground checks and finds this tile, the reported angle
        /// for movement will be the pre-defined value set in Angle, instead of the raycast hit's angle.
        /// </Summary>
        public bool UseFixedGroundAngle { get { return useFixedGroundAngle; } }

        [SerializeField] private bool isAngled = true;

        /// <Summary>
        /// Should be true if the tile has an angled surface.
        /// If false, this tile is considered fully solid and its reported angle depends on the current movement mode of the character.
        /// </Summary>
        public bool IsAngled { get { return isAngled; } }

        [Range(0f, 359.999f)]
        [SerializeField] private float angle = 0f;

        /// <Summary>
        /// The angle, in degrees, used to move along this ground tile.
        /// </Summary>
        public float Angle { get { return angle; } }

        [SerializeField] private bool isOneWayPlatform = false;

        /// <Summary>
        /// If true, characters do not collide with it from underneath, or from the sides.
        /// </Summary>
        public bool IsOneWayPlatform { get { return isOneWayPlatform; } }
    }
}