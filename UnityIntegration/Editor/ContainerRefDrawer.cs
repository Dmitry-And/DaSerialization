#if UNITY_2018_1_OR_NEWER

using System;
using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    [CustomPropertyDrawer(typeof(ContainerRefWithId))]
    public class ContainerRefWithIdDrawer : PropertyDrawer
    {
        private ContainerInAssetWindow.ContainerInfo _container;
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
                var container = ContainerInAssetWindow.GetContainerInfo(text);
                if (container == null)
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

            position.TopRow(out position);
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(position.SliceRight(60f), idProp, GUIContent.none);
            EditorGUI.PropertyField(position, containerProp, GUIContent.none);
            EditorGUI.indentLevel = oldIndent;

            UpdateContainer(containerProp);
            property.isExpanded = DrawContainerContent(pos, position, _container, containerProp, property.isExpanded,
                out int idToDelete, out Type typeToDelete);

            if (idToDelete != 0)
            {
                var textAsset = property.FindPropertyRelative(ContainerRef.TextAssetFieldName).objectReferenceValue as TextAsset;
                var cRef = ContainerRef.FromTextAsset(textAsset);
                cRef.Remove(idToDelete, typeToDelete, true);
            }

            EditorGUI.EndProperty();
        }

        public static bool DrawContainerContent(Rect pos, Rect containerRect,
            ContainerInAssetWindow.ContainerInfo container, SerializedProperty containerProp,
            bool expanded, out int entryToDelete, out Type typeToDelete)
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
                expanded = EditorGUI.Foldout(rect, expanded, container.Entries.Count + " entries, " + container.Size / 1024 + " KB");
                rect.yMin = rect.yMax;
                rect.yMax = rect.yMin + EditorGUIUtility.singleLineHeight;
                if (expanded)
                    foreach (var entry in container.Entries)
                    {
                        rect = DrawEntry(rect, entry, out bool delete);
                        if (delete)
                        {
                            entryToDelete = entry.Id;
                            typeToDelete = entry.Type;
                        }
                    }
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
            return expanded;
        }

        public static Rect DrawEntry(Rect rect, ContainerInAssetWindow.ContainerInfo.Entry entry, out bool delete)
        {
            var idRect = rect;
            idRect.width = 40f;
            GUI.contentColor = Color.red;
            EditorGUI.LabelField(idRect, entry.Id.ToString(), EditorStyles.whiteLabel);
            GUI.contentColor = Color.white;
            var typeRect = rect;
            typeRect.xMin = idRect.xMax;
            typeRect.width -= 85f;
            EditorGUI.LabelField(typeRect, entry.TypeName);
            var sizeRect = rect;
            sizeRect.xMin = typeRect.xMax;
            sizeRect.xMax = rect.xMax - 20f;
            GUI.contentColor = new Color(0.4f, 0.4f, 0.4f);
            EditorGUI.LabelField(sizeRect, entry.Size / 1024 + " KB", EditorStyles.whiteLabel);
            GUI.contentColor = Color.white;
            var deleteRect = rect;
            deleteRect.xMin = sizeRect.xMax;
            GUI.backgroundColor = Color.red;
            delete = GUI.Button(deleteRect, "X");
            GUI.backgroundColor = Color.white;
            rect.yMin = rect.yMax;
            rect.yMax = rect.yMin + EditorGUIUtility.singleLineHeight;
            return rect;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var containerProp = property.FindPropertyRelative(ContainerRefWithId.TextAssetFieldName);
            UpdateContainer(containerProp);
            var height = _height;
            if (property.isExpanded && _container != null)
                height += _container.Entries.Count * EditorGUIUtility.singleLineHeight;
            return height;
        }
    }

    [CustomPropertyDrawer(typeof(ContainerRef))]
    public class ContainerRefDrawer : PropertyDrawer
    {
        private ContainerInAssetWindow.ContainerInfo _container;
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
                var container = ContainerInAssetWindow.GetContainerInfo(text);
                if (container == null)
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

            var containerProp = property.FindPropertyRelative(ContainerRef.TextAssetFieldName);

            var position = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);

            position.TopRow(out position);
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(position, containerProp, GUIContent.none);
            EditorGUI.indentLevel = oldIndent;

            UpdateContainer(containerProp);
            property.isExpanded = ContainerRefWithIdDrawer.DrawContainerContent(pos, position, _container,
                containerProp, property.isExpanded, out int idToDelete, out Type typeToDelete);

            if (idToDelete != 0)
            {
                var textAsset = property.FindPropertyRelative(ContainerRef.TextAssetFieldName).objectReferenceValue as TextAsset;
                var cRef = ContainerRef.FromTextAsset(textAsset);
                cRef.Remove(idToDelete, typeToDelete, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var containerProp = property.FindPropertyRelative(ContainerRef.TextAssetFieldName);
            UpdateContainer(containerProp);
            var height = _height;
            if (property.isExpanded && _container != null)
                height += _container.Entries.Count * EditorGUIUtility.singleLineHeight;
            return height;
        }

        public static void Draw<T>(ref ContainerRef container, int idToCheck = -1)
        {
            bool valid = idToCheck == -1 || (container.IsValid && container.Container.Has<T>(idToCheck));

            GUI.backgroundColor = valid ? Color.white : Color.red;
            EditorGUI.BeginChangeCheck();
            var newTextAsset = EditorGUILayout.ObjectField(container.TextAsset, typeof(TextAsset), false) as TextAsset;
            if (EditorGUI.EndChangeCheck())
                container.TextAsset = newTextAsset;
            GUI.backgroundColor = Color.white;
        }
    }
}

#endif