#if UNITY_2018_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInAssetWindow : EditorWindow
    {
        private const float SizeWidth = 48f;
        private static GUIStyle Bold;
        private static GUIStyle Normal;
        private static GUIStyle BoldRight;
        private static GUIStyle NormalRight;
        private static GUIContent TotalSizeHeader = new GUIContent("Total", "Total size of this object, including meta-information.\nIn bytes");
        private static GUIContent DataSizeHeader = new GUIContent("Data", "Size of this object, excluding meta-information.\nIn bytes");
        private static GUIContent SelfSizeHeader = new GUIContent("Self", "It's total size excluding inner objects and meta info.\nIn bytes");
        private static GUIContent ExpandButton = new GUIContent("+", "Expand");
        private static GUIContent ShrinkButton = new GUIContent("-", "Shrink");
        private static GUIContent JsonLabel = new GUIContent("J", "Show JSON representation of the object...");
        private static GUIContent RefreshButton = new GUIContent("Refresh", "Reload container from the asset and update all stats");

        public TextAsset Target;
        private ContainerEditorInfo _info;
        private GUIContent _sizeText;
        private bool _renderSelfSize = true;
        private bool _renderTotalSize = true; // if not - effective size will be rendered
        private bool _renderRefType = true; // if not - only object type will be rendered
        private float _idWidth;

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
                if (_info.IsValid)
                    _info.UpdateDetailedInfo();
                _sizeText = new GUIContent(Size(_info.Size), $"Total size: {_info.Size}\nMeta data: {_info.MetaInfoSize}\nUseful: {_info.Size - _info.MetaInfoSize}");
                _expandedObjects.Clear();
                _idWidth = GetMaxIdWidth(_info, _idWidth);
            }
            if (Target == null)
                _info = null;
            DrawContainerContent(_info);
        }

        public void Refresh()
        {
            _info = null;
            _idWidth = 20f;
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
            EditorGUI.LabelField(pos.SliceRight(52f), "Objects:", NormalRight);
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
            EditorGUI.LabelField(colHeaderRect.SliceLeft(_idWidth), "Id", NormalRight);
            EditorGUI.LabelField(colHeaderRect.SliceRight(18f), JsonLabel, NormalRight);
            if (GUI.Button(colHeaderRect.SliceRight(SizeWidth),
                _renderTotalSize ? TotalSizeHeader : DataSizeHeader, NormalRight))
                _renderTotalSize = !_renderTotalSize;
            GUI.contentColor = _renderSelfSize ? Color.gray : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            if (GUI.Button(colHeaderRect.SliceRight(SizeWidth), SelfSizeHeader, NormalRight))
                _renderSelfSize = !_renderSelfSize;
            colHeaderRect.SliceLeft(16f);
            GUI.contentColor = Color.gray;
            if (GUI.Button(colHeaderRect, _renderRefType ? "Ref : Object" : "Object", Normal))
                _renderRefType = !_renderRefType;
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
        private HashSet<ContainerEditorInfo.InnerObjectInfo> _expandedObjects = new HashSet<ContainerEditorInfo.InnerObjectInfo>();
        private static GUIContent _tempContent = new GUIContent();
        private Rect DrawEntry(Rect pos, ContainerEditorInfo.InnerObjectInfo e, float indent)
        {
            bool isRoot = _parentEntry.Count == 0;

            if (isRoot)
                EditorGUI.DrawRect(pos, new Color(0.3f, 0.3f, 1f, 0.2f));

            // id
            var idRect = pos.SliceLeft(_idWidth);
            if (e.Id != -1)
            {
                GUI.contentColor = isRoot ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                EditorGUI.LabelField(idRect, e.Id.ToString(), isRoot ? BoldRight : NormalRight);
            }
            
            var collapseRect = pos.SliceLeft(indent);
            if (!isRoot && GUI.Button(collapseRect, "", GUIStyle.none))
                SetExpanded(_parentEntry.Peek(), false, Event.current.alt);

            // expanded
            var expandRect = pos.SliceLeft(16f);
            bool expanded = e.IsExpandable && _expandedObjects.Contains(e);
            if (e.IsExpandable)
            {
                GUI.contentColor = Color.gray;
                if (GUI.Button(expandRect, expanded ? ShrinkButton : ExpandButton, BoldRight))
                    SetExpanded(e, !expanded, Event.current.alt);
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

            var nameRect = pos;
            // size (persistent)
            {
                _tempContent.text = Size(_renderTotalSize ? e.TotalSize : e.DataSize);
                var width = BoldRight.CalcSize(_tempContent).x;
                nameRect.xMax = pos.xMax - width;
                GUI.contentColor = Color.white;
                EditorGUI.LabelField(pos.SliceRight(SizeWidth), _tempContent, BoldRight);
            }
            // size (optional)
            if (_renderSelfSize & e.SelfSize > 0)
            {
                _tempContent.text = Size(e.SelfSize);
                var width = BoldRight.CalcSize(_tempContent).x;
                nameRect.xMax = pos.xMax - width;
                GUI.contentColor = Color.gray;
                EditorGUI.LabelField(pos.SliceRight(SizeWidth), _tempContent, NormalRight);
            }
            var result = pos;

            // name
            GUI.contentColor = e.IsSupported
                ? e.IsNull ? Color.grey : Color.black
                : Color.red;
            if (GUI.Button(nameRect, _renderRefType ? e.Caption : e.TypeInfo.Type.PrettyName(), e.IsRealObject ? Bold : Normal)
                & e.IsExpandable)
                SetExpanded(e, !expanded, Event.current.alt);

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
        private void SetExpanded(ContainerEditorInfo.InnerObjectInfo e, bool value, bool child = false)
        {
            var oldValue = _expandedObjects.Contains(e);
            if (!value & oldValue)
                _expandedObjects.Remove(e);
            if (value & !oldValue)
                _expandedObjects.Add(e);
            if (child & e.IsExpandable)
                foreach (var inner in e.InnerObjects)
                    if (inner.IsExpandable)
                        SetExpanded(inner, value, true);
        }

        private static float GetMaxIdWidth(ContainerEditorInfo info, float minValue = 0f)
        {
            int maxId = 0;
            if (info.RootObjects != null)
                foreach (var root in info.RootObjects)
                {
                    var rootId = GetMaxAbsId(root.Data);
                    maxId = maxId > rootId ? maxId : rootId;
                }
            float width = Bold.CalcSize(new GUIContent("-" + maxId)).x;
            return width > minValue ? width : minValue;
        }
        private static int GetMaxAbsId(ContainerEditorInfo.InnerObjectInfo info)
        {
            int maxAbsId = Math.Abs(info.Id);
            if (info.IsExpandable)
                foreach (var inner in info.InnerObjects)
                {
                    int innerId = Math.Abs(GetMaxAbsId(inner));
                    maxAbsId = maxAbsId > innerId ? maxAbsId : innerId;
                }
            return maxAbsId;
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
            const float MinWidth = 120f;
            const float MaxWidth = 300f;
            const float MaxHeight = 400f;

            if (_textSize.x > 0f)
                return _windowSize;

            var textContent = new GUIContent(_objectInfo.JsonData);
            _textSize.x = TextStyle.CalcSize(textContent).x;
            if (_objectInfo.JsonHasErrors | _textSize.x > MaxWidth)
                _textSize.x = MaxWidth;
            if (_textSize.x < MinWidth)
                _textSize.x = MinWidth;
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
            _windowSize.y = visibleHeight + 8f;
            return _windowSize;
        }

        private Vector2 _scrollPos;
        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(_objectInfo.Caption, GUILayout.MinWidth(50f), GUILayout.MaxHeight(EditorGuiUtils.GetLinesHeight(1)));
            if (GUILayout.Button("Copy", GUILayout.Width(42f)))
                UniClipboard.SetText(_objectInfo.JsonData);
            if (GUILayout.Button("X", GUILayout.Width(20f)))
                editorWindow.Close();
            EditorGUILayout.EndHorizontal();
            if (_objectInfo.JsonHasErrors)
            {
                var boxRect = GUILayoutUtility.GetRect(100f, 2000f, 20f, 20f);
                if (_objectInfo.JsonCreated)
                    EditorGUI.HelpBox(boxRect, "JSON errors displayed at the end of the text", MessageType.Warning);
                else
                    EditorGUI.HelpBox(boxRect, "Failed to serialize to JSON", MessageType.Error);
            }
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
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
