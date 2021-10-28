#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    [CustomPropertyDrawer(typeof(ContainerRefWithId))]
    [CustomPropertyDrawer(typeof(ContainerRef))]
    public class ContainerRefWithIdDrawer : PropertyDrawer
    {
        private static GUIContent notSelectedLabel = new GUIContent("---", "No container selected");
        private static GUIContent invalidLabel = new GUIContent("Inv", "Selected TextAsset is not a valid container");
        private static GUIContent noIdLabel = new GUIContent("NoID", "The ID specified is not present in the container");

        private ContainerEditorInfo _container;
        private TextAsset _text;
        private DefaultAsset _defaultAsset;
        private GUIContent _infoLabel;
        private Color _color;

        private void UpdateContainer(SerializedProperty property)
        {
            //var text = property.objectReferenceValue as TextAsset;
            //if (_container == null | _text != text)
            var defaultAsset = property.objectReferenceValue as DefaultAsset;
            if (_container == null | _defaultAsset != defaultAsset)
                ForceUpdateContainer(property);
        }
        private void ForceUpdateContainer(SerializedProperty property)
        {
            //var text = property.objectReferenceValue as TextAsset;
            //_text = text;
            //_container = text == null ? null : new ContainerEditorInfo(text, Event.current.alt);

            var defaultAsset = property.objectReferenceValue as DefaultAsset;
            _defaultAsset = defaultAsset;
            _container = defaultAsset == null ? null : new ContainerEditorInfo(defaultAsset, Event.current.alt);

            _color = Color.white;
            if (_container == null)
                _infoLabel = notSelectedLabel;
            else if (!_container.IsValid)
            {
                _color = Color.red;
                _infoLabel = invalidLabel;
            }
            else
            {
                string entitiesCount = _container.EntriesCount.ToString();
                _infoLabel = new GUIContent(entitiesCount, $"Entities: {entitiesCount}, size: {ContainerEditorView.Size(_container.Size)} bytes");
            }
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, property);

            var containerProp = property.FindPropertyRelative(ContainerRefWithId.TextAssetFieldName);
            var idProp = property.FindPropertyRelative(nameof(ContainerRefWithId.Id));

            var position = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);
            var infoRect = position.SliceRight(30f);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (idProp != null)
                EditorGUI.PropertyField(position.SliceRight(60f), idProp, GUIContent.none);
            EditorGUI.PropertyField(position, containerProp, GUIContent.none);
            EditorGUI.indentLevel = oldIndent;

            UpdateContainer(containerProp);

            GUIContent infoLabel = _infoLabel;
            Color color = _color;
            if (_container != null && _container.IsValid
                && idProp != null && !_container.ContainsObjectWithId(idProp.intValue))
            {
                color = Color.yellow;
                infoLabel = noIdLabel;
            }
            GUI.backgroundColor = color;
            if (GUI.Button(infoRect, infoLabel))
            {
                ForceUpdateContainer(containerProp);
                if (_container != null && _container.IsValid)
                {
                    var textAsset = containerProp.objectReferenceValue as TextAsset;
                    PopupWindow.Show(infoRect, new ContainerInspectorPopup(_container, textAsset));
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.EndProperty();
        }
    }
}

#endif