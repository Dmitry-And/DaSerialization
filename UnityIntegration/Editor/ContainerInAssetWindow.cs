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
            _nextLineIsEven = false;
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
            _parentEntry.Clear();
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
            EditorGUI.LabelField(colHeaderRect.SliceRight(18f), JsonLabel, NormalRight);
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
        private static GUIContent JsonLabel = new GUIContent("J", "Show JSON representation of the object...");

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

        private Stack<ContainerEditorInfo.InnerObjectInfo> _parentEntry = new Stack<ContainerEditorInfo.InnerObjectInfo>();
        private bool _renderSelfSize = true;
        private HashSet<ContainerEditorInfo.InnerObjectInfo> _expandedObjects = new HashSet<ContainerEditorInfo.InnerObjectInfo>();
        private Rect DrawEntry(Rect pos, ContainerEditorInfo.InnerObjectInfo e, float indent)
        {
            bool isRoot = _parentEntry.Count == 0;

            if (isRoot)
                EditorGUI.DrawRect(pos, new Color(0.3f, 0.3f, 1f, 0.2f));

            // id
            var idRect = pos.SliceLeft(IdWidth);
            if (e.Id != -1)
            {
                GUI.contentColor = isRoot ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                EditorGUI.LabelField(idRect, e.Id.ToString(), isRoot ? BoldRight : NormalRight);
            }
            
            var collapseRect = pos.SliceLeft(indent);
            if (!isRoot && GUI.Button(collapseRect, "", GUIStyle.none))
                ToggleExpand(_parentEntry.Peek());

            // expanded
            var expandRect = pos.SliceLeft(16f);
            if (e.IsExpandable)
            {
                GUI.contentColor = Color.gray;
                bool expanded = _expandedObjects.Contains(e);
                if (GUI.Button(expandRect, expanded ? ShrinkButton : ExpandButton, BoldRight))
                    ToggleExpand(e);
            }

            // json
            var jsonRect = pos.SliceRight(18f);
            if (e.IsRealObject & !e.IsNull & e.IsSupported)
            {
                GUI.backgroundColor = e.JsonHasErrors ? new Color(0.9f, 0.7f, 0.7f, 0.6f)
                    : e.JsonData == null ? Color.clear : new Color(0.7f, 0.9f, 0.7f, 0.6f);
                if (GUI.Button(jsonRect, "J"))
                {
                    _info.UpdateJsonData(e);
                    PopupWindow.Show(jsonRect, new JsonPopupWindow(e));
                }
                GUI.backgroundColor = Color.white;
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
            if (GUI.Button(pos, e.Caption, e.IsRealObject ? Bold : Normal)
                & e.IsExpandable)
                ToggleExpand(e);

            // internal entries
            if (e.IsExpandable && _expandedObjects.Contains(e))
            {
                _parentEntry.Push(e);
                foreach (var inner in e.InnerObjects)
                    DrawEntry(GetNextLineRect(true), inner, indent + 12f);
                _parentEntry.Pop();
            }

            return result;
        }
        private void ToggleExpand(ContainerEditorInfo.InnerObjectInfo e)
        {
            if (_expandedObjects.Contains(e))
                _expandedObjects.Remove(e);
            else
                _expandedObjects.Add(e);
        }

        private static string Size(long size)
        {
            return size.ToStringFast();
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

    public class JsonPopupWindow : PopupWindowContent
    {
        private static GUIStyle TextStyle;
        private readonly ContainerEditorInfo.InnerObjectInfo _objectInfo;

        public JsonPopupWindow(ContainerEditorInfo.InnerObjectInfo objInfo)
        {
            _objectInfo = objInfo;
            if (TextStyle == null)
            {
                TextStyle = new GUIStyle(EditorStyles.textArea);
                TextStyle.wordWrap = true;
            }
        }

        private float _width = -1f;
        private Vector2 _windowSize;
        public override Vector2 GetWindowSize()
        {
            const float MaxWidth = 260f;
            const float MaxHeight = 400f;

            if (_width > 0f)
                return _windowSize;

            var size = TextStyle.CalcSize(new GUIContent(_objectInfo.JsonData));
            _width = size.x < MaxWidth ? size.x : MaxWidth;
            float height = size.y + 6f;
            EditorGuiUtils.AddLineHeight(ref height);
            if (_objectInfo.JsonHasErrors)
            {
                EditorGuiUtils.AddLineHeight(ref height);
                EditorGuiUtils.AddLineHeight(ref height);
                _width = MaxWidth;
                while (size.x > _width)
                {
                    EditorGuiUtils.AddLineHeight(ref height);
                    size.x -= _width - 30f;
                }
            }
            if (height > MaxHeight)
                height = MaxHeight;
            if (height < 120f)
                height = 120f;
            _windowSize = new Vector2(_width + 20f, height);
            return _windowSize;
        }

        private Vector2 _scrollPos;
        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField(_objectInfo.Caption);
            if (_objectInfo.JsonHasErrors)
                EditorGUILayout.HelpBox("There were errors during JSON serialization. They are displayed at the end", MessageType.Warning);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.TextArea(_objectInfo.JsonData, TextStyle, GUILayout.Width(_width));
            EditorGUILayout.EndScrollView();
        }
        public override void OnOpen() { }
        public override void OnClose() { }
    }
}

#endif
