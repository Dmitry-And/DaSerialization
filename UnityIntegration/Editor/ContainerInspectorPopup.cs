#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;
using DaSerialization.Internal;

namespace DaSerialization.Editor
{
    public class ContainerInspectorPopup : PopupWindowContent
    {
        private static GUIStyle TextStyle;
        private ContainerEditorView _view;
        private TextAsset _asset;

        public ContainerInspectorPopup(ContainerEditorInfo containerInfo, TextAsset asset)
        {
            _view = new ContainerEditorView(containerInfo, asset != null);
            _asset = asset;
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
            pos = pos.Expand(-2f); // 2-pixel margins
            var updatedContainer = _view.Draw(pos);
            if (updatedContainer != null && _asset != null)
            {
                ContainerAssetUtils.WriteToTextAsset(updatedContainer, _asset);
                var container = ContainerRef.FromTextAsset(_asset).Container;
                _view = new ContainerEditorView(new ContainerEditorInfo(container), _asset);
            }
        }
    }
}

#endif