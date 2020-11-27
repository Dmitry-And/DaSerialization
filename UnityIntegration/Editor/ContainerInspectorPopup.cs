#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInspectorPopup : PopupWindowContent
    {
        private static GUIStyle TextStyle;
        private readonly ContainerEditorView _view;

        public ContainerInspectorPopup(ContainerEditorView containerView)
        {
            _view = containerView;
            if (TextStyle == null)
            {
                TextStyle = new GUIStyle(EditorStyles.textArea);
                TextStyle.wordWrap = true;
                TextStyle.fontSize--;
            }
        }

        public override Vector2 GetWindowSize() => new Vector2(300f, 450f);

        public override void OnGUI(Rect rect)
        {
            var pos = new Rect(new Vector2(), GetWindowSize());
            pos = pos.Shrink(2f); // 2-pixel margins
            _view.Draw(pos);
        }
    }
}

#endif