#if UNITY_2018_1_OR_NEWER

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInAssetWindow : EditorWindow
    {
        private const float SizeWidth = 48f;
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
        private static GUIContent RefreshButton = new GUIContent("Refresh", "Reload container from the asset and update all stats");

        public TextAsset Target;
        private ContainerEditorInfo _info;
        private GUIContent _sizeText;

        [MenuItem("Window/Container Viewer")]
        public static void InitWindow()
        {
            ContainerInAssetWindow window = (ContainerInAssetWindow)GetWindow(typeof(ContainerInAssetWindow));
            window.name = "DaSerialization Inspector";
            window.titleContent = new GUIContent("DaSerialization Inspector");
            window.minSize = new Vector2(300f, 300f);
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
            var pos = GetNextLineRect();
            EditorGUI.BeginDisabledGroup(Target == null);
            if (GUI.Button(pos.SliceRight(60f), RefreshButton))
                Refresh();
            EditorGUI.EndDisabledGroup();
            EditorGUI.LabelField(pos.SliceLeft(38f), "Asset");
            Target = (TextAsset)EditorGUI.ObjectField(pos, Target, typeof(TextAsset), false);

            if (Target != null && (_info == null || _info.Asset != Target))
            {
                _info = new ContainerEditorInfo(Target);
                _info.UpdateDetailedInfo();
                _sizeText = new GUIContent(Size(_info.Size), $"Total size: {_info.Size}\nMeta data: {_info.MetaInfoSize}\nUseful: {_info.Size - _info.MetaInfoSize}");
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
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), _sizeText, Bold);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos.SliceRight(36f), "Size:", NormalRight);
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(46f), info.EntriesCount.ToStringFast(), Bold);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos.SliceRight(45f), "Entries:", NormalRight);
            EditorGUI.LabelField(pos.SliceLeft(60f), "Container", EditorStyles.boldLabel);

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
            EditorGUI.LabelField(colHeaderRect, "Ref : Object", Normal);
        }

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
                bool requiresJsonUpdate = e.JsonData == null;
                GUI.contentColor = new Color(1f, 1f, 1f, 0.5f);
                GUI.backgroundColor = requiresJsonUpdate ? Color.clear
                    : !e.JsonHasErrors ? new Color(0.7f, 0.9f, 0.7f, 0.6f)
                    : e.JsonCreated ? new Color(0.9f, 0.9f, 0.7f, 0.6f) : new Color(0.9f, 0.7f, 0.7f, 0.6f);
                if (GUI.Button(jsonRect, "J")
                    && (!requiresJsonUpdate | e.TotalSize < 1024
                        || EditorUtility.DisplayDialog("Large object", $"Object \"{e.Caption}\" seems to be large and it may take a while to convert it to Json string.\nAre you sure you want to continue?", "Continue", "Cancel")))
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

        private static StringBuilder _sb = new StringBuilder(16);
        private static string Size(long size)
        {
            if (size < 1024)
                return size.ToStringFast();

            char suffix = 'k';
            float value = size / 1024f;
            if (value >= 1024f)
            {
                value /= 1024f;
                suffix = 'm';
            }
            if (value >= 1024f)
            {
                value /= 1024f;
                suffix = 'g';
            }
            if (value >= 1024f)
            {
                value /= 1024f;
                suffix = 't';
            }

            _sb.Length = 0;
            if (value < 10f)
                _sb.AppendFast(value, 2, false);
            else if (value < 100f)
                _sb.AppendFast(value, 1, false);
            else
                _sb.AppendFast(value, 0, false);

            _sb.Append(' ');
            _sb.Append(suffix);

            var result = _sb.ToString();
            _sb.Length = 0;
            return result;
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
                TextStyle.fontSize--;
            }
        }

        private Vector2 _textSize = new Vector2();
        private Vector2 _windowSize;
        public override Vector2 GetWindowSize()
        {
            const float MaxWidth = 300f;
            const float MaxHeight = 400f;

            if (_textSize.x > 0f)
                return _windowSize;

            var textContent = new GUIContent(_objectInfo.JsonData);
            _textSize.x = TextStyle.CalcSize(textContent).x;
            if (_objectInfo.JsonHasErrors | _textSize.x > MaxWidth)
                _textSize.x = MaxWidth;
            _textSize.y = TextStyle.CalcHeight(textContent, _textSize.x);
            float visibleHeight = _textSize.y;
            EditorGuiUtils.AddLineHeight(ref visibleHeight);
            if (_objectInfo.JsonHasErrors)
                EditorGuiUtils.AddLineHeight(ref visibleHeight, 20f);
            _windowSize.x = _textSize.x + 6f;
            if (visibleHeight > MaxHeight)
            {
                visibleHeight = MaxHeight;
                _windowSize.x += 14f; // scroll bar
            }
            _windowSize.y = visibleHeight + 6f;
            return _windowSize;
        }

        private Vector2 _scrollPos;
        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_objectInfo.Caption);
            if (GUILayout.Button("Copy", GUILayout.Width(42f)))
                UniClipboard.SetText(_objectInfo.JsonData);
            EditorGUILayout.EndHorizontal();
            if (_objectInfo.JsonHasErrors)
            {
                var boxRect = GUILayoutUtility.GetRect(100f, 2000f, 20f, 20f);
                if (_objectInfo.JsonCreated)
                    EditorGUI.HelpBox(boxRect, "JSON errors displayed at the end of the text", MessageType.Warning);
                else
                    EditorGUI.HelpBox(boxRect, "Failed to serialize to JSON", MessageType.Error);
            }
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            var textRect = GUILayoutUtility.GetRect(_textSize.x, _textSize.y, TextStyle);
            // TODO: unity has a bug: SelectableLabel cannot select text after a certain line number
            EditorGUI.SelectableLabel(textRect, _objectInfo.JsonData, TextStyle);
            EditorGUILayout.EndScrollView();
        }
        public override void OnOpen() { }
        public override void OnClose() { }
    }
}

#endif
