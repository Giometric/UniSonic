using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace Giometric.UniSonic.Editor
{
    [CustomEditor(typeof(GroundTile), true)]
    [CanEditMultipleObjects]
    public class GroundTileEditor : UnityEditor.Editor
    {
        private SerializedProperty spriteProperty;
        private SerializedProperty colliderTypeProperty;
        private SerializedProperty useFixedGroundAngleProperty;
        private SerializedProperty isAngledProperty;
        private SerializedProperty angleProperty;
        private SerializedProperty isOneWayPlatformProperty;

        private float anglePreviewHeight = 32f;

        private void OnEnable()
        {
            spriteProperty = serializedObject.FindProperty("m_Sprite");
            colliderTypeProperty = serializedObject.FindProperty("m_ColliderType");
            useFixedGroundAngleProperty = serializedObject.FindProperty("useFixedGroundAngle");
            isAngledProperty = serializedObject.FindProperty("isAngled");
            angleProperty = serializedObject.FindProperty("angle");
            isOneWayPlatformProperty = serializedObject.FindProperty("isOneWayPlatform");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spriteProperty);
            Rect tileRect;
            Sprite sprite = spriteProperty.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                var texture = AssetPreview.GetAssetPreview(sprite);
                if (texture != null && texture.height > 0)
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    float aspect = sprite.rect.width / sprite.rect.height;
                    float width = 64f * aspect;
                    GUILayoutUtility.GetRect(width, 64);
                    tileRect = GUILayoutUtility.GetLastRect();
                    tileRect.width = width;
                    tileRect.height = 64;

                    // Draw a border around the sprite texture
                    Rect line = tileRect;
                    line.x -= 1;
                    line.y -= 1;
                    line.width = 1;
                    line.height += 2;
                    EditorGUI.DrawRect(line, new Color32(128, 128, 128, 255));
                    line.x = tileRect.x + tileRect.width;
                    EditorGUI.DrawRect(line, new Color32(128, 128, 128, 255));
                    line.x = tileRect.x;
                    line.width = tileRect.width;
                    line.height = 1;
                    EditorGUI.DrawRect(line, new Color32(128, 128, 128, 255));
                    line.y = tileRect.y + tileRect.height;
                    EditorGUI.DrawRect(line, new Color32(128, 128, 128, 255));

                    GUI.DrawTexture(tileRect, texture);
                }
                else
                {
                    GUILayoutUtility.GetRect(64f, 64f);
                    tileRect = GUILayoutUtility.GetLastRect();
                }
            }
            else
            {
                GUILayoutUtility.GetRect(64f, 64f);
                tileRect = GUILayoutUtility.GetLastRect();
            }

            EditorGUILayout.PropertyField(useFixedGroundAngleProperty);
            EditorGUILayout.Separator();
            GUI.enabled = useFixedGroundAngleProperty.boolValue;

            if (isAngledProperty.boolValue)
            {
                // Slider for moving the preview line up and down
                Rect sliderRect = tileRect;
                sliderRect.x += tileRect.width + 4;
                sliderRect.width = EditorGUIUtility.singleLineHeight;
                sliderRect.height = tileRect.height;
                anglePreviewHeight = GUI.VerticalSlider(sliderRect, anglePreviewHeight, 0f, tileRect.height);

                // Use super-hacks to draw a rotated line showing the current angle
                Rect lineRect = tileRect;
                lineRect.y = tileRect.y + anglePreviewHeight;// tileRect.height / 2f;
                lineRect.height = 1;
                Vector2 pivot = new Vector2(tileRect.x + tileRect.width / 2f, tileRect.y + anglePreviewHeight);
                GUIUtility.RotateAroundPivot(-angleProperty.floatValue, pivot);
                EditorGUI.DrawRect(lineRect, Color.green);
                GUIUtility.RotateAroundPivot(angleProperty.floatValue, pivot);
            }

            EditorGUILayout.PropertyField(isAngledProperty);
            GUI.enabled = useFixedGroundAngleProperty.boolValue && isAngledProperty.boolValue;
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(angleProperty);
            --EditorGUI.indentLevel;
            GUI.enabled = true;
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(colliderTypeProperty);
            EditorGUILayout.Separator();

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(isOneWayPlatformProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}