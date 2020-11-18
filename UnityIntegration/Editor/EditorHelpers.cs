#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public static class EditorHelpers
    {
        private static float vSpace => EditorGUIUtility.standardVerticalSpacing;
        private static float line => EditorGUIUtility.singleLineHeight;

        public static Rect TopRow(this Rect rect, out Rect rowRect, bool applySpacing = true)
        { return TopRow(rect, line, out rowRect, applySpacing); }
        public static Rect TopRow(this Rect rect, float height, out Rect rowRect, bool applySpacing = true)
        {
            float spacing = applySpacing ? vSpace : 0f;
            rowRect = new Rect(rect.xMin, rect.yMin, rect.width, height);
            return new Rect(rect.xMin, rect.yMin + height + spacing, rect.width, rect.height - height - spacing);
        }

        public static Rect SliceRight(this ref Rect rect, bool applySpacing = true)
            => SliceRight(ref rect, rect.width - EditorGUIUtility.labelWidth, applySpacing);
        public static Rect SliceRight(this ref Rect rect, float size, bool applySpacing = true)
        {
            rect = rect.RightColumn(size, out var result, applySpacing);
            return result;
        }
        public static Rect RightColumn(this Rect rect, float width, out Rect columnRect, bool applySpacing = true)
        {
            float spacing = applySpacing ? vSpace : 0f;
            columnRect = new Rect(rect.xMax - width, rect.yMin, width, rect.height);
            return new Rect(rect.xMin, rect.yMin, rect.width - width - spacing, rect.height);
        }
    }
}

#endif