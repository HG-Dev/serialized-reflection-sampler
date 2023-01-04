using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SerializedReflectionSampler.Editor
{
    using Runtime;
    
    [CustomEditor(typeof(GetterMethodSampler))]
    public class SerializedFieldSamplerEditor : UnityEditor.Editor
    {
        SerializedProperty _sampleOnStartProp;
        SerializedProperty _samplerListProp;

        GUIContent _guiTargetsHeaderContent;
        GUIContent _guiAddButtonContent;
        GUIContent _guiRemoveButtonContent;
        Vector2 _guiRemoveButtonSize;

        const float GUI_ADD_BUTTON_WIDTH = 200f;

        private void OnEnable()
        {
            _sampleOnStartProp = serializedObject.FindProperty("_sampleOnStart");
            _samplerListProp = serializedObject.FindProperty("_samplers");

            _guiTargetsHeaderContent = new GUIContent("Targets");
            _guiAddButtonContent = new GUIContent("Add Target Entry",
                "Add a slot to select a target object and field for sampling.");
            _guiRemoveButtonContent = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
            _guiRemoveButtonContent.tooltip = "Remove";
            _guiRemoveButtonSize = GUIStyle.none.CalcSize(_guiRemoveButtonContent);
        }

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_sampleOnStartProp);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_guiTargetsHeaderContent, EditorStyles.boldLabel);

            RenderTargetListInspector();
            RenderAddSamplerButton();

            serializedObject.ApplyModifiedProperties();
        }

        public void RenderTargetListInspector()
        {
            int removalIndex = -1;
            for (int i = 0; i < _samplerListProp.arraySize; i++)
            {
                var samplerProperty = _samplerListProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SerializedProperty targetObjectProp = samplerProperty.FindPropertyRelative("target");
                    SerializedProperty targetObjectFieldNameProp = samplerProperty.FindPropertyRelative("fieldName");

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
                    {
                        EditorGUILayout.PropertyField(samplerProperty, _guiRemoveButtonContent);

                        Rect rect = GUILayoutUtility.GetLastRect();
                        Rect removeButtonRect = new Rect(rect.xMax - _guiRemoveButtonSize.x - 8,
                            rect.y + 1 + SamplerInfoDrawer.BLOCK_MARGIN_Y, _guiRemoveButtonSize.x,
                            _guiRemoveButtonSize.y);
                        if (GUI.Button(removeButtonRect, _guiRemoveButtonContent, GUIStyle.none))
                        {
                            removalIndex = i;
                        }
                    }
                }
            }

            if (removalIndex >= 0)
            {
                _samplerListProp.DeleteArrayElementAtIndex(removalIndex);
                _samplerListProp.serializedObject.ApplyModifiedProperties();
            }
        }

        public void RenderAddSamplerButton()
        {
            Rect addButtonRect = GUILayoutUtility.GetRect(_guiAddButtonContent, GUI.skin.button);
            addButtonRect.x += (addButtonRect.width - GUI_ADD_BUTTON_WIDTH) * 0.5f;
            addButtonRect.width = GUI_ADD_BUTTON_WIDTH;
            if (GUI.Button(addButtonRect, _guiAddButtonContent))
            {
                GrowArray(_samplerListProp);
            }
        }

        public void RenderDebugFields(SerializedProperty samplerProperty)
        {
            EditorGUILayout.Space(24);
            GUI.enabled = false;
            var props = SamplerInfoDrawer.PropGroup.Create(samplerProperty);
            EditorGUILayout.PropertyField(props.target);
            EditorGUILayout.PropertyField(props.getterName);
            EditorGUILayout.PropertyField(props.getterTypeName);
            EditorGUILayout.PropertyField(props.listeners);
            GUI.enabled = true;
        }

        public static SerializedProperty GrowArray(SerializedProperty arrayProperty, bool onlyToInitialize = false)
        {
            if (!arrayProperty.isArray)
                return null;

            if (arrayProperty.arraySize < 1)
                arrayProperty.InsertArrayElementAtIndex(0);
            else if (!onlyToInitialize)
                arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);

            return arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        }
    }
}