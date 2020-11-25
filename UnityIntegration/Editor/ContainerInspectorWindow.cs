#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerInspectorWindow : EditorWindow
    {
        private static GUIContent RefreshButton = new GUIContent("Refresh", "Reload container from the asset and update all stats");

        public TextAsset Target;
        private TextAsset _displayedAsset;
        private ContainerEditorView _containerView;

        [MenuItem("Window/Container Viewer")]
        public static void InitWindow()
        {
            ContainerInspectorWindow window = (ContainerInspectorWindow)GetWindow(typeof(ContainerInspectorWindow));
            window.name = "Container Inspector";
            window.titleContent = new GUIContent("Container Inspector");
            window.minSize = new Vector2(300f, 300f);
            window.Show();
        }

        void OnGUI()
        {
            var pos = new Rect(new Vector2(), position.size);
            pos = pos.Shrink(2f); // 2-pixel margins
            var line = pos.SliceTop();
            EditorGUI.BeginDisabledGroup(Target == null);
            if (GUI.Button(line.SliceRight(60f), RefreshButton))
            {
                _containerView = null;
                _displayedAsset = null;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.LabelField(line.SliceLeft(38f), "Asset");
            Target = (TextAsset)EditorGUI.ObjectField(line, Target, typeof(TextAsset), false);

            if (Target != null && (_containerView == null || _displayedAsset != Target))
            {
                var info = new ContainerEditorInfo(Target);
                _displayedAsset = Target;
                _containerView = new ContainerEditorView(info);
            }
            if (Target == null)
            {
                _containerView = null;
                _displayedAsset = null;
            }
            _containerView.Draw(pos);
        }
    }

}

#endif
