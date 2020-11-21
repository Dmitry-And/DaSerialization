#if UNITY_2018_1_OR_NEWER

using System;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    [CustomPropertyDrawer(typeof(ContainerRefWithId))]
    [CustomPropertyDrawer(typeof(ContainerRef))]
    public class ContainerRefWithIdDrawer : PropertyDrawer
    {
        private ContainerEditorInfo _container;
        private float _height;
        private TextAsset _text;

        private void UpdateContainer(SerializedProperty property)
        {
            var text = property.objectReferenceValue as TextAsset;

            if (text == null)
            {
                _text = text;
                _height = EditorGUIUtility.singleLineHeight;
                _container = null;
                return;
            }
            // TODO: check file content was changed OR add manual refresh
            if (_container == null
                | _text != text)
            {
                _text = text;
                var container = new ContainerEditorInfo(text);
                if (!container.IsValid)
                {
                    // text asset is not a container
                    _height = EditorGUIUtility.singleLineHeight * 2;
                    _container = null;
                    return;
                }
                else
                {
                    _container = container;
                    _height = EditorGUIUtility.singleLineHeight * 2;
                }
            }
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, property);

            var containerProp = property.FindPropertyRelative(ContainerRefWithId.TextAssetFieldName);
            var idProp = property.FindPropertyRelative(nameof(ContainerRefWithId.Id));

            var position = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (idProp != null)
                EditorGUI.PropertyField(position.SliceRight(60f), idProp, GUIContent.none);
            EditorGUI.PropertyField(position, containerProp, GUIContent.none);
            EditorGUI.indentLevel = oldIndent;

            UpdateContainer(containerProp);
            DrawContainerContent(pos, position, _container, containerProp,
                out int idToDelete, out Type typeToDelete);

            if (idToDelete != 0)
            {
                var textAsset = property.FindPropertyRelative(ContainerRef.TextAssetFieldName).objectReferenceValue as TextAsset;
                var cRef = ContainerRef.FromTextAsset(textAsset);
                cRef.Remove(idToDelete, typeToDelete, true);
            }

            EditorGUI.EndProperty();
        }

        public static void DrawContainerContent(Rect pos, Rect containerRect,
            ContainerEditorInfo container, SerializedProperty containerProp,
            out int entryToDelete, out Type typeToDelete)
        {
            var rect = new Rect(pos);
            rect.yMin += containerRect.height;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.indentLevel++;
            rect = EditorGUI.IndentedRect(rect);
            EditorGUI.indentLevel--;
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            entryToDelete = 0;
            typeToDelete = null;
            if (container != null)
            {
                EditorGUI.LabelField(rect, container.EntriesCount + " entries, " + container.Size / 1024 + " KB");
            }
            else
            {
                if (containerProp.objectReferenceValue != null)
                {
                    GUI.contentColor = Color.red;
                    EditorGUI.LabelField(rect, "It's not a container!");
                    GUI.contentColor = Color.white;
                }
            }
            EditorGUI.indentLevel = oldIndent;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGuiUtils.GetLinesHeight(1);
        }
    }
}

#endif