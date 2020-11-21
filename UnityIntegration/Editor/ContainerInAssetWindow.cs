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
            EditorGUI.LabelField(pos.SliceLeft(80f), info.Size + " bytes", Normal);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos, info.TableSize + " tbl", Normal);

            pos = GetNextLineRect();
            GUI.contentColor = Color.gray;
            EditorGUI.LabelField(pos.SliceLeft(IdWidth), "Id", NormalRight);
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), TotalHeader, NormalRight);
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), SelfHeader, NormalRight);
            pos.SliceLeft(16f);
            EditorGUI.LabelField(pos, "Ref Type : Object Type", Normal);
            GUI.contentColor = Color.white;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var e in info.RootObjects)
            {
                pos = GetNextLineRect();
                DrawEntry(pos, e);
            }

            EditorGUILayout.EndScrollView();
        }

        private const float SizeWidth = 52f;
        private const float IdWidth = 50f;
        private static GUIStyle Bold;
        private static GUIStyle Normal;
        private static GUIStyle BoldRight;
        private static GUIStyle NormalRight;
        private static GUIContent TotalHeader = new GUIContent("Total", "Total size of this object, including meta-information.\nIn bytes");
        private static GUIContent SelfHeader = new GUIContent("Self", "It's total size excluding inner objects and meta info.\nIn bytes");
        private static GUIContent ExpandButton = new GUIContent("+", "Expand");
        private static GUIContent ShrinkButton = new GUIContent("-", "Shrink");
        private Rect GetNextLineRect()
        {
            var lineHeight = EditorGuiUtils.GetLinesHeight(1);
            var rect = GUILayoutUtility.GetRect(100f, 2000f, lineHeight, lineHeight);
            rect.SliceLeft(2f);
            rect.SliceRight(2f);
            return rect;
        }

        private void DrawEntry(Rect pos, ContainerEditorInfo.RootObjectInfo e)
        {
            var nameRect = DrawEntry(pos, e.Data, 0f);
            if (!e.IsSupported)
                EditorGUI.HelpBox(nameRect.SliceRightRelative(0.7f), e.Error, MessageType.Error);
        }

        private HashSet<ContainerEditorInfo.InnerObjectInfo> _expandedObjects = new HashSet<ContainerEditorInfo.InnerObjectInfo>();
        private Rect DrawEntry(Rect pos, ContainerEditorInfo.InnerObjectInfo e, float indent)
        {
            pos.SliceLeft(indent);

            // id
            var idRect = pos.SliceLeft(IdWidth);
            if (e.Id != -1)
            {
                bool isRoot = indent == 0f;
                GUI.contentColor = isRoot ? Color.red : Color.grey;
                EditorGUI.LabelField(idRect, e.Id.ToString(), isRoot ? BoldRight : NormalRight);
            }

            // size
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), e.TotalSize.ToString(), BoldRight);
            if (e.SelfSize > 0)
            {
                GUI.contentColor = Color.gray;
                EditorGUI.LabelField(pos.SliceRight(SizeWidth), e.SelfSize.ToString(), NormalRight);
            }

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

            // name
            var result = pos;
            GUI.contentColor = e.IsSupported
                ? e.IsNull ? Color.grey : Color.black
                : Color.red;
            EditorGUI.LabelField(pos, e.Name, Bold);

            // internal entries
            if (e.IsExpandable && _expandedObjects.Contains(e))
                foreach (var inner in e.InnerObjects)
                    DrawEntry(GetNextLineRect(), inner, indent + 16f);

            return result;
        }
    }
}

#endif
