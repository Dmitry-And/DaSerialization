﻿#if UNITY_2018_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public static class ContainerEditorViewDrawExtension
    {
        // returns updated container if it was changed, null otherwise
        public static BinaryContainer Draw(this ContainerEditorView view, Rect position)
        {
            if (view == null)
            {
                var boxRect = position.SliceTop(EditorHelpers.GetLinesHeight(2));
                EditorGUI.HelpBox(boxRect, "Select a container asset", MessageType.Info);
                return null;
            }
            if (!view.Info.IsValid)
            {
                var boxRect = position.SliceTop(EditorHelpers.GetLinesHeight(2));
                EditorGUI.HelpBox(boxRect, "This is not a valid container", MessageType.Error);
                return null;
            }
            return view.DrawContainerContent(position);
        }
    }
    public class ContainerEditorView
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
        private static GUIContent JsonLabel = new GUIContent("D ", "Show object data in JSON-like format...");
        private static GUIContent UpdateSerializersButton = new GUIContent("Update\nSerializers", "Deserializer container and serialize it again with newest available serializers");
        private static float _lineHeight;

        public ContainerEditorInfo Info { get; private set; }
        public bool RenderSelfSize = true;
        public bool RenderTotalSize = true; // if not - effective size will be rendered
        public bool RenderRefType = true; // if not - only object type will be rendered
        public bool Editable { get; private set; }

        private GUIContent _sizeText;
        private float _idWidth;

        public ContainerEditorView(ContainerEditorInfo info, bool editable)
        {
            InitStatic();

            Info = info;
            if (info.IsValid)
                info.UpdateDetailedInfo();
            _sizeText = new GUIContent(Size(Info.Size), $"Total size: {Info.Size}\nMeta data: {Info.MetaInfoSize}\nUseful: {Info.Size - Info.MetaInfoSize}");
            _expandedObjects.Clear();
            _idWidth = GetMaxIdWidth(Info, _idWidth);
            Editable = editable;
        }

        private static void InitStatic()
        {
            if (Bold != null)
                return;
            Bold = new GUIStyle(EditorStyles.whiteBoldLabel);
            Bold.normal.textColor = Color.white;
            Normal = EditorStyles.whiteLabel;
            BoldRight = new GUIStyle(Bold);
            BoldRight.alignment = TextAnchor.LowerRight;
            NormalRight = new GUIStyle(Normal);
            NormalRight.alignment = TextAnchor.LowerRight;
            _lineHeight = EditorHelpers.GetLinesHeight(1);
        }

        // returns updated container if it was changed, null otherwise
        private Vector2 _scrollPos;
        public BinaryContainer DrawContainerContent(Rect position)
        {
            BinaryContainer updatedContainer = null;

            _nextLineIsEven = false;
            var pos = position.SliceTop();
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(SizeWidth), _sizeText, Bold);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos.SliceRight(36f), "Size:", NormalRight);
            GUI.contentColor = Color.white;
            EditorGUI.LabelField(pos.SliceRight(46f), Info.EntriesCount.ToString(), Bold);
            GUI.contentColor = Color.grey;
            EditorGUI.LabelField(pos.SliceRight(52f), "Objects:", NormalRight);
            EditorGUI.LabelField(pos.SliceLeft(60f), "Container", EditorStyles.boldLabel);
            GUI.contentColor = Color.white;

            if (Info.HasOldVersions)
            {
                var warnRect = position.SliceTop(EditorHelpers.GetLinesHeight(Editable ? 2 : 1));
                if (Editable
                    && GUI.Button(warnRect.SliceRight(75f), UpdateSerializersButton))
                {
                    updatedContainer = Info.GetContainer();
                    updatedContainer.UpdateSerializers();
                }
                EditorGUI.HelpBox(warnRect, "Has old serializers (newer version exists). Marked with yellow", MessageType.Warning);
            }

            var colHeaderRect = position.SliceTop(); // reserve a line for columns rendering (later)

            position = position.Expand(2f); // negate window margins
            GUILayout.BeginArea(position);
            _scrollViewHeight = position.height;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            _parentEntries.Clear();
            foreach (var e in Info.RootObjects)
                DrawEntry(e);
            GetNextLineVisible(out var innerLineRect);
            float inScrollWidth = innerLineRect.width;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // we render column header section after the table because we want to know its layout
            // particularly the width of the view area as the vertical scroll bar may or may not be visible
            GUI.contentColor = Color.gray;
            colHeaderRect = colHeaderRect.SliceLeft(inScrollWidth, false);
            EditorGUI.LabelField(colHeaderRect.SliceLeft(_idWidth), "Id", NormalRight);
            EditorGUI.LabelField(colHeaderRect.SliceRight(18f), JsonLabel, NormalRight);
            if (GUI.Button(colHeaderRect.SliceRight(SizeWidth),
                RenderTotalSize ? TotalSizeHeader : DataSizeHeader, NormalRight))
                RenderTotalSize = !RenderTotalSize;
            GUI.contentColor = RenderSelfSize ? Color.gray : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            if (GUI.Button(colHeaderRect.SliceRight(SizeWidth), SelfSizeHeader, NormalRight))
                RenderSelfSize = !RenderSelfSize;
            colHeaderRect.SliceLeft(16f);
            GUI.contentColor = Color.gray;
            if (GUI.Button(colHeaderRect, RenderRefType ? "Ref : Object" : "Object", Normal))
                RenderRefType = !RenderRefType;

            if (!Editable & updatedContainer != null)
            {
                Debug.LogError("Trying to modify uneditable container!\n");
                updatedContainer = null;
            }
            return updatedContainer;
        }

        private float _scrollViewHeight;
        private bool _nextLineIsEven;
        private bool GetNextLineVisible(out Rect rect, bool evenLineHighlighted = false)
        {
            rect = GUILayoutUtility.GetRect(100f, 2000f, _lineHeight, _lineHeight);
            _nextLineIsEven = !_nextLineIsEven;
            bool isVisible = rect.yMax >= _scrollPos.y & rect.yMin <= _scrollPos.y + _scrollViewHeight;
            if (_nextLineIsEven & evenLineHighlighted & isVisible)
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.13f));
            return isVisible;
        }

        private void DrawEntry(ContainerEditorInfo.RootObjectInfo e)
        {
            var pos = DrawEntry(e.Data, 0f);
            if (!e.IsSupported)
                EditorGUI.HelpBox(pos.SliceRightRelative(0.7f), e.Error, MessageType.Error);
        }

        private Stack<ContainerEditorInfo.InnerObjectInfo> _parentEntries = new Stack<ContainerEditorInfo.InnerObjectInfo>();
        private HashSet<ContainerEditorInfo.InnerObjectInfo> _expandedObjects = new HashSet<ContainerEditorInfo.InnerObjectInfo>();
        private static GUIContent _tempContent = new GUIContent();
        private Rect DrawEntry(ContainerEditorInfo.InnerObjectInfo e, float indent)
        {
            bool isRoot = _parentEntries.Count == 0;
            bool isVisible = GetNextLineVisible(out var pos, true);
            var lineRect = pos;
            if (isVisible)
            {
                // id
                var idRect = pos.SliceLeft(_idWidth);
                if (isRoot)
                {
                    EditorGUI.DrawRect(pos, new Color(0.3f, 0.3f, 1f, 0.2f));
                    if (!e.HasOldVersions & !e.OldVersion)
                        EditorGUI.DrawRect(idRect, new Color(0.3f, 0.3f, 1f, 0.2f));
                }
                if (e.OldVersion)
                    EditorGUI.DrawRect(idRect, new Color(0.9f, 0.9f, 0.3f, 0.8f));
                else if (e.HasOldVersions)
                    EditorGUI.DrawRect(idRect, new Color(0.9f, 0.9f, 0.3f, 0.4f));
                if (e.Id != -1)
                {
                    GUI.contentColor = isRoot ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                    EditorGUI.LabelField(idRect, e.Id.ToString(), isRoot ? BoldRight : NormalRight);
                }
                if (e.HasOldVersions && GUI.Button(idRect, GUIContent.none, GUIStyle.none))
                    ExpandOldWarnings(e);

                var collapseRect = pos.SliceLeft(indent);
                if (!isRoot && GUI.Button(collapseRect, "", GUIStyle.none))
                    SetExpanded(_parentEntries.Peek(), false, Event.current.alt);

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
                var jsonRect = pos.SliceRight(19f);
                if (e.IsRealObject & !e.IsNull & e.IsSupported)
                {
                    bool requiresJsonUpdate = e.JsonData == null;
                    GUI.contentColor = new Color(1f, 1f, 1f, 0.5f);
                    GUI.backgroundColor = requiresJsonUpdate ? Color.clear
                        : !e.JsonHasErrors ? new Color(0.7f, 0.9f, 0.7f, 0.6f)
                        : e.JsonCreated ? new Color(0.9f, 0.9f, 0.7f, 0.6f) : new Color(0.9f, 0.7f, 0.7f, 0.6f);
                    if (GUI.Button(jsonRect, "D"))
                        PopupWindow.Show(jsonRect, new ObjectContentPopupWindow(Info, e));
                    GUI.backgroundColor = Color.white;
                }

                var nameRect = pos;
                // size (persistent)
                {
                    _tempContent.text = Size(RenderTotalSize ? e.TotalSize : e.DataSize);
                    var width = BoldRight.CalcSize(_tempContent).x;
                    nameRect.xMax = pos.xMax - width;
                    GUI.contentColor = Color.white;
                    EditorGUI.LabelField(pos.SliceRight(SizeWidth), _tempContent, BoldRight);
                }
                // size (optional)
                if (RenderSelfSize & e.SelfSize > 0)
                {
                    _tempContent.text = Size(e.SelfSize);
                    var width = BoldRight.CalcSize(_tempContent).x;
                    nameRect.xMax = pos.xMax - width;
                    GUI.contentColor = Color.gray;
                    EditorGUI.LabelField(pos.SliceRight(SizeWidth), _tempContent, NormalRight);
                }

                // name
                GUI.contentColor = e.IsSupported
                    ? e.IsNull ? Color.grey : Color.black
                    : Color.red;
                if (GUI.Button(nameRect, RenderRefType ? e.Caption : e.TypeInfo.Type.PrettyName(), e.IsRealObject ? Bold : Normal)
                    & e.IsExpandable)
                    SetExpanded(e, !expanded, Event.current.alt);
            }

            // internal entries
            if (e.IsExpandable && _expandedObjects.Contains(e))
            {
                _parentEntries.Push(e);
                foreach (var inner in e.InnerObjects)
                    DrawEntry(inner, indent + 12f);
                _parentEntries.Pop();
            }
            return lineRect;
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
        private void ExpandOldWarnings(ContainerEditorInfo.InnerObjectInfo e)
        {
            if (!e.HasOldVersions | !e.IsExpandable)
                return;
            SetExpanded(e, true);
            foreach (var inner in e.InnerObjects)
                if (inner.IsExpandable)
                    ExpandOldWarnings(inner);
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
        public static string Size(long size)
        {
            if (size < 1024)
                return size.ToString();

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
                _sb.AppendFormat("{0:0.00}", value);
            else if (value < 100f)
                _sb.AppendFormat("{0:0.0}", value);
            else
                _sb.AppendFormat("{0:0}", value);

            _sb.Append(' ');
            _sb.Append(suffix);

            var result = _sb.ToString();
            _sb.Length = 0;
            return result;
        }
    }
}

#endif