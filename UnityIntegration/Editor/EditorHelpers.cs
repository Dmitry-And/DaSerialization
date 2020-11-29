#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public static class EditorHelpers
    {
        private static float vSpace => EditorGUIUtility.standardVerticalSpacing;
        private static float line => EditorGUIUtility.singleLineHeight;

        #region rect/layout operations

        public static Rect SliceTop(this ref Rect rect, bool applySpacing = true)
            => SliceTop(ref rect, line, applySpacing);
        public static Rect SliceTop(this ref Rect rect, float size, bool applySpacing = true)
        {
            rect = rect.TopRow(size, out var result, applySpacing);
            return result;
        }
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
        public static Rect SliceRightRelative(this ref Rect rect, float size, bool applySpacing = true)
            => SliceRight(ref rect, size * (rect.width - (applySpacing ? vSpace : 0f)), applySpacing);
        public static Rect RightColumn(this Rect rect, float width, out Rect columnRect, bool applySpacing = true)
        {
            float spacing = applySpacing ? vSpace : 0f;
            columnRect = new Rect(rect.xMax - width, rect.yMin, width, rect.height);
            return new Rect(rect.xMin, rect.yMin, rect.width - width - spacing, rect.height);
        }

        public static Rect SliceLeft(this ref Rect rect, bool applySpacing = true)
            => SliceLeft(ref rect, EditorGUIUtility.labelWidth, applySpacing);
        public static Rect SliceLeft(this ref Rect rect, float size, bool applySpacing = true)
        {
            rect = rect.LeftColumn(size, out var result, applySpacing);
            return result;
        }
        public static Rect LeftColumn(this Rect rect, float width, out Rect columnRect, bool applySpacing = true)
        {
            float spacing = applySpacing ? vSpace : 0f;
            columnRect = new Rect(rect.xMin, rect.yMin, width, rect.height);
            return new Rect(rect.xMin + width + spacing, rect.yMin, rect.width - width - spacing, rect.height);
        }

        public static Rect Expand(this Rect r, float margin)
        {
            var m = new Vector2(margin, margin);
            return new Rect(r.min - m, r.size + 2 * m);
        }

        #endregion

        public static float GetLinesHeight(int lines) => lines * line + (lines - 1) * vSpace;

        public static void AddLineHeight(ref float height, float lineHeight = -1f)
        {
            if (lineHeight < 0)
                lineHeight = line;
            if (height > 0.1f)
                height += vSpace;
            height += lineHeight;
        }
    }
}

#endif