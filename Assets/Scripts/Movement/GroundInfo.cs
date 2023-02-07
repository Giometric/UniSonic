using UnityEngine;
using UnityEngine.Tilemaps;

namespace Giometric.UniSonic
{
    public struct GroundInfo
    {
        /// <Summary>
        /// The world-space position of the ground point.
        /// </Summary>
        public Vector3 Point;
        /// <Summary>
        /// The normal vector of the ground point's surface.
        /// </Summary>
        public Vector2 Normal;
        /// <Summary>
        /// The angle (in radians) of the ground point's surface.
        /// </Summary>
        public float Angle;
        /// <Summary>
        /// The RaycastHit2D info associated with this ground.
        /// </Summary>
        public RaycastHit2D Hit;
        /// <Summary>
        /// Whether or not the GroundInfo contains valid data.
        /// </Summary>
        public bool IsValid;

        public static GroundInfo Invalid
        {
            get { return new GroundInfo { IsValid = false }; }
        }
    }
}