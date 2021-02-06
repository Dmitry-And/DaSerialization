#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ObjectContentPopupWindow : PopupWindowContent
    {
        // smaller objects will be represented as json w/o confirmation
        private static int AutoCreateJsonSize = 1024;
        private static GUIStyle TextStyle;
        private readonly ContainerEditorInfo _containerInfo;
        private readonly ContainerEditorInfo.InnerObjectInfo _objectInfo;
        private readonly string _caption;

        public ObjectContentPopupWindow(ContainerEditorInfo containerInfo, ContainerEditorInfo.InnerObjectInfo objInfo)
        {
            _containerInfo = containerInfo;
            _objectInfo = objInfo;
            _caption = _objectInfo.IsNull ? "Null" : _objectInfo.TypeInfo.Type.PrettyName();
            if (_objectInfo.JsonData == null
                & _objectInfo.TotalSize <= AutoCreateJsonSize)
                _containerInfo.UpdateJsonData(_objectInfo);

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

            if (_objectInfo.JsonData == null)
                return new Vector2(230f, 102f);

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
            EditorHelpers.AddLineHeight(ref visibleHeight);
            if (_objectInfo.JsonHasErrors)
                EditorHelpers.AddLineHeight(ref visibleHeight, 20f);
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
            if (_objectInfo.JsonData == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(_caption, GUILayout.MinWidth(50f), GUILayout.MaxHeight(EditorHelpers.GetLinesHeight(1)));
                if (GUILayout.Button("X", GUILayout.Width(20f)))
                    editorWindow.Close();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("The object seems to be rather large. It may take a while to create JSON representation of it. Do you want to continue?", MessageType.Warning);
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Show Object Data as JSON"))
                    _containerInfo.UpdateJsonData(_objectInfo);
                GUI.backgroundColor = Color.white;
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(_caption, GUILayout.MinWidth(50f), GUILayout.MaxHeight(EditorHelpers.GetLinesHeight(1)));
            if (GUILayout.Button("Copy", GUILayout.Width(42f)))
                GUIUtility.systemCopyBuffer = _objectInfo.JsonData;
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