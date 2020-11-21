#if UNITY_2018_1_OR_NEWER

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInAssetWindow : EditorWindow
    {
        public TextAsset Target;
        private ContainerEditorInfo _info;

        [MenuItem("Window/Container Viewer")]
        public static void InitWindow()
        {
            ContainerInAssetWindow window = (ContainerInAssetWindow)GetWindow(typeof(ContainerInAssetWindow));
            window.name = "Container Viewer";
            window.Show();
        }

        public static void Init()
        {
            if (Bold != null)
                return;
            Bold = EditorGuiUtils.whiteBoldLabel;
            Normal = EditorStyles.whiteLabel;
            BoldRight = new GUIStyle(Bold);
            BoldRight.alignment = TextAnchor.LowerRight;
            NormalRight = new GUIStyle(Normal);
            NormalRight.alignment = TextAnchor.LowerRight;
        }

        void OnGUI()
        {
            Init();
            EditorGUILayout.BeginHorizontal();
            Target = (TextAsset)EditorGUILayout.ObjectField("Target", Target, typeof(TextAsset), false);
            if (GUILayout.Button("Refresh", GUILayout.Width(60f)))
                Refresh();
            EditorGUILayout.EndHorizontal();

            if (Target != null && (_info == null || _info.Asset != Target))
            {
                _info = new ContainerEditorInfo(Target);
                _info.UpdateDetailedInfo();
                _expandedObjects.Clear();
            }
            if (Target == null)
                _info = null;
            DrawContainerContent(_info);
        }

        public void Refresh()
        {
            _info = null;
            _expandedObjects.Clear();
        }

        private Vector2 _scrollPos;
        public void DrawContainerContent(ContainerEditorInfo info)
        {
            if (info == null)
            {
                EditorGUILayout.HelpBox("Select a container asset", MessageType.Info);
                return;
            }
            if (!info.IsValid)
            {
                EditorGUILayout.HelpBox("This is not a valid container", MessageType.Error);
                return;
            }
            var pos = GetNextLineRect();
            EditorGUI.LabelField(pos.SliceLeft(60f), "Container", EditorStyles.boldLabel);
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceLeft(80f), Size(info.Size), Normal);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos, Size(info.TableSize) + " tbl", Normal);

            var colHeaderRect = GetNextLineRect(); // reserve a line for columns rendering (later)

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var e in info.RootObjects)
            {
                pos = GetNextLineRect(true);
                DrawEntry(pos, e);
            }
            float inScrollWidth = GetNextLineRect().width;
            EditorGUILayout.EndScrollView();

            // we render column header section after the table because we want to know its layout
            // particularly the width of the view area as the vertical scroll bar may or may not be visible
            GUI.contentColor = Color.gray;
            colHeaderRect = colHeaderRect.SliceLeft(inScrollWidth, false);
            EditorGUI.LabelField(colHeaderRect.SliceLeft(IdWidth), "Id", NormalRight);
            EditorGUI.LabelField(colHeaderRect.SliceRight(SizeWidth), TotalHeader, NormalRight);
            GUI.contentColor = _renderSelfSize ? Color.gray : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            if (GUI.Button(colHeaderRect.SliceRight(SizeWidth), SelfHeader, NormalRight))
                _renderSelfSize = !_renderSelfSize;
            colHeaderRect.SliceLeft(16f);
            GUI.contentColor = Color.gray;
            EditorGUI.LabelField(colHeaderRect, "Ref Type : Object Type", Normal);
        }

        private const float SizeWidth = 46f;
        private const float IdWidth = 50f;
        private static GUIStyle Bold;
        private static GUIStyle Normal;
        private static GUIStyle BoldRight;
        private static GUIStyle NormalRight;
        private static GUIContent TotalHeader = new GUIContent("Total", "Total size of this object, including meta-information.\nIn bytes");
        private static GUIContent SelfHeader = new GUIContent("Self", "It's total size excluding inner objects and meta info.\nIn bytes");
        private static GUIContent ExpandButton = new GUIContent("+", "Expand");
        private static GUIContent ShrinkButton = new GUIContent("-", "Shrink");

        private bool _nextLineIsEven;
        private Rect GetNextLineRect(bool evenLineHighlighted = false)
        {
            var lineHeight = EditorGuiUtils.GetLinesHeight(1);
            var rect = GUILayoutUtility.GetRect(100f, 2000f, lineHeight, lineHeight);
            rect.SliceLeft(2f);
            rect.SliceRight(2f);
            _nextLineIsEven = !_nextLineIsEven;
            if (_nextLineIsEven & evenLineHighlighted)
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.13f));
            return rect;
        }

        private void DrawEntry(Rect pos, ContainerEditorInfo.RootObjectInfo e)
        {
            var nameRect = DrawEntry(pos, e.Data, 0f);
            if (!e.IsSupported)
                EditorGUI.HelpBox(nameRect.SliceRightRelative(0.7f), e.Error, MessageType.Error);
        }

        private bool _renderSelfSize = true;
        private HashSet<ContainerEditorInfo.InnerObjectInfo> _expandedObjects = new HashSet<ContainerEditorInfo.InnerObjectInfo>();
        private Rect DrawEntry(Rect pos, ContainerEditorInfo.InnerObjectInfo e, float indent)
        {
            bool isRoot = indent == 0f;

            if (isRoot)
                EditorGUI.DrawRect(pos, new Color(0.3f, 0.3f, 1f, 0.2f));

            // id
            var idRect = pos.SliceLeft(IdWidth);
            if (e.Id != -1)
            {
                GUI.contentColor = isRoot ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                EditorGUI.LabelField(idRect, e.Id.ToString(), isRoot ? BoldRight : NormalRight);
            }
            
            pos.SliceLeft(indent);

            // expanded
            var expandRect = pos.SliceLeft(16f);
            if (e.IsExpandable)
            {
                GUI.contentColor = Color.gray;
                bool expanded = _expandedObjects.Contains(e);
                if (GUI.Button(expandRect, expanded ? ShrinkButton : ExpandButton, BoldRight))
                {
                    if (expanded)
                        _expandedObjects.Remove(e);
                    else
                        _expandedObjects.Add(e);
                }
            }

            // size
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), Size(e.TotalSize), BoldRight);
            if (_renderSelfSize & e.SelfSize > 0)
            {
                GUI.contentColor = Color.gray;
                EditorGUI.LabelField(pos.SliceRight(SizeWidth), Size(e.SelfSize), NormalRight);
            }

            // name
            var result = pos;
            GUI.contentColor = e.IsSupported
                ? e.IsNull ? Color.grey : Color.black
                : Color.red;
            EditorGUI.LabelField(pos, e.Name, e.IsRealObject ? Bold : Normal);

            // internal entries
            if (e.IsExpandable && _expandedObjects.Contains(e))
                foreach (var inner in e.InnerObjects)
                    DrawEntry(GetNextLineRect(true), inner, indent + 12f);

            return result;
        }

        private static string Size(long size)
        {
            if (size < 1024)
                return size.ToStringFast();
            float kSize = size / 1024f;
            if (kSize < 10f)
                return kSize.ToStringFast(2, false) + "k";
            if (kSize < 100f)
                return kSize.ToStringFast(1, false) + "k";
            if (kSize < 1024f)
                return kSize.ToStringFast(0) + "k";
            float mSize = kSize / 1024f;
            if (mSize < 10f)
                return mSize.ToStringFast(2, false) + "m";
            if (mSize < 100f)
                return mSize.ToStringFast(1, false) + "m";
            if (mSize < 1024f)
                return mSize.ToStringFast(0) + "m";
            float gSize = mSize / 1024f;
            if (gSize < 10f)
                return gSize.ToStringFast(2, false) + "g";
            if (gSize < 100f)
                return gSize.ToStringFast(1, false) + "g";
            return gSize.ToStringFast(0) + "g";
        }
    }
}

#endif
