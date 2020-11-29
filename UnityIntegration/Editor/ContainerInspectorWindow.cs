#if UNITY_2018_1_OR_NEWER

using System;
using DaSerialization.Internal;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInspectorWindow : EditorWindow
    {
        private static GUIContent RefreshButton = new GUIContent("Refresh", "Reload container from the asset and update all stats");
        private static Type[] _docksNextTo = new[] { typeof(ContainerInspectorWindow), null };

        public TextAsset Target;
        private TextAsset _displayedAsset;
        private ContainerEditorView _containerView;

        [MenuItem("Window/Container Inspector")]
        protected static void OpenContainerWindow()
            => GetOrCreateWindow();

        public static ContainerInspectorWindow GetOrCreateWindow(bool forceCreateNew = false)
        {
            if (_docksNextTo[1] == null)
                _docksNextTo[1] = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

            var window = forceCreateNew
                ? CreateWindow<ContainerInspectorWindow>("Container Inspector", _docksNextTo)
                : GetWindow<ContainerInspectorWindow>("Container Inspector", true, _docksNextTo);
            window.minSize = new Vector2(300f, 300f);
            window.Show();
            return window;
        }

        private void Refresh()
        {
            _containerView = null;
            _displayedAsset = null;
        }

        void OnGUI()
        {
            var pos = new Rect(new Vector2(), position.size);
            pos = pos.Expand(-2f); // 2-pixel margins
            var line = pos.SliceTop();
            EditorGUI.BeginDisabledGroup(Target == null);
            if (GUI.Button(line.SliceRight(60f), RefreshButton))
                Refresh();
            EditorGUI.EndDisabledGroup();
            EditorGUI.LabelField(line.SliceLeft(38f), "Asset");
            Target = (TextAsset)EditorGUI.ObjectField(line, Target, typeof(TextAsset), false);

            if (Target != null && (_containerView == null || _displayedAsset != Target))
            {
                var info = new ContainerEditorInfo(Target);
                _displayedAsset = Target;
                _containerView = new ContainerEditorView(info, true);
            }
            if (Target == null)
                Refresh();
            var updatedContainer = _containerView.Draw(pos);
            if (updatedContainer != null)
            {
                ContainerAssetUtils.WriteToTextAsset(updatedContainer, _displayedAsset);
                Refresh();
            }
        }
    }

}

#endif
