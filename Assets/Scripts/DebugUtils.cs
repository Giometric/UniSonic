using UnityEngine;

namespace Giometric.UniSonic
{    
    public static class DebugUtils
    {
        public static void DrawHorizontalTick(Vector2 center, float width, Color color, float duration = 0f, bool depthTest = false)
        {
            Debug.DrawLine(center + new Vector2(width * -0.5f, 0f), center + new Vector2(width * 0.5f, 0f), color, duration, depthTest);
        }

        public static void DrawVerticalTick(Vector2 center, float height, Color color, float duration = 0f, bool depthTest = false)
        {
            Debug.DrawLine(center + new Vector2(0f, height * -0.5f), center + new Vector2(0f, height * 0.5f), color, duration, depthTest);
        }

        public static void DrawCross(Vector2 center, float size, Color color, float duration = 0f, bool depthTest = false)
        {
            DrawHorizontalTick(center, size, color, duration, depthTest);
            DrawVerticalTick(center, size, color, duration, depthTest);
        }

        public static void DrawDiagonalCross(Vector2 center, float size, Color color, float duration = 0f, bool depthTest = false)
        {
            // Equivalent to sin(PI * 0.25) * 0.5, shorthand magic number so we get diagonal lines that have about the right length
            float coord = size * 0.353553f;
            Debug.DrawLine(center + new Vector2(-coord, -coord), center + new Vector2(coord, coord), color, duration, depthTest);
            Debug.DrawLine(center + new Vector2(-coord, coord), center + new Vector2(coord, -coord), color, duration, depthTest);
        }

        public static void DrawArrow(Vector2 start, Vector2 end, float arrowheadSize, Color color, float duration = 0f, bool depthTest = false)
        {
            Vector2 dif = end - start;
            Vector2 arrowDir = Vector2.up;
            if (dif.sqrMagnitude > 0f)
            {
                arrowDir = dif.normalized;
                Debug.DrawLine(start, end, color, duration, depthTest);
            }

            Vector3 arrowhead1 = Quaternion.Euler(0f, 0f, 135f) * arrowDir * arrowheadSize * 0.5f;
            Vector3 arrowhead2 = Quaternion.Euler(0f, 0f, -135f) * arrowDir * arrowheadSize * 0.5f;
            Vector2 arrowEnd1 = end + new Vector2(arrowhead1.x, arrowhead1.y);
            Vector2 arrowEnd2 = end + new Vector2(arrowhead2.x, arrowhead2.y);

            Debug.DrawLine(end, arrowEnd1, color, duration, depthTest);
            Debug.DrawLine(end, arrowEnd2, color, duration, depthTest);
        }
    }
}