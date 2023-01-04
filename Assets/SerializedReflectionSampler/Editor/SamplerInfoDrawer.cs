using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SerializedReflectionSampler.Editor
{
    using Runtime;
    
    [CustomPropertyDrawer(typeof(SamplerInfo))]
    public class SamplerInfoDrawer : PropertyDrawer
    {
        public const float BLOCK_MARGIN_Y = 4;

        public class PropGroup
        {
            public SerializedProperty parent;
            public SerializedProperty target;
            public SerializedProperty getterName;
            public SerializedProperty getterTypeName;
            public SerializedProperty listeners;

            public static PropGroup Create(SerializedProperty property)
            {
                return new PropGroup
                {
                    parent = property,
                    target = property.FindPropertyRelative("target"),
                    getterName = property.FindPropertyRelative("sampleGetterName"),
                    getterTypeName = property.FindPropertyRelative("sampleTypeName"),
                    listeners = property.FindPropertyRelative("listeners")
                };
            }
        }

        public class SamplerMethodMap
        {
            private PropGroup _props;
            private Component _target;
            private MethodInfo _getter;

            public SamplerMethodMap(PropGroup sampleProperties, Component target, MethodInfo getter)
            {
                _props = sampleProperties;
                _target = target;
                _getter = getter;
            }

            public Component Target => _target;
            public string TargetTypeName => _target.GetType().Name;
            public string GetterName => _getter.Name;
            public string ReturnTypeName => _getter.ReturnType.Name;
            public string ShortReturnTypeName => ShortTypeName(_getter.ReturnType);
            public bool IsCurrentSampler => _props.target.objectReferenceValue == _target
                && _props.getterName.stringValue == GetterName
                && _props.getterTypeName.stringValue == ReturnTypeName;

            // Alter the property fields to match this sampler method map.
            public void Apply()
            {
                Debug.Log($"Applying sample info: {TargetTypeName}.{GetterName}");
                _props.target.objectReferenceValue = _target;
                _props.getterName.stringValue = GetterName;
                _props.getterTypeName.stringValue = ReturnTypeName;
                _props.parent.serializedObject.ApplyModifiedProperties();
            }

            public void Clear()
            {
                _props.getterName.stringValue = "";
                _props.getterTypeName.stringValue = "";
            }
        }

        public static string ShortTypeName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            return t.Name;
        }

        public static StringBuilder BuildCleanMethodName(string getterName, StringBuilder sb = null)
        {
            sb = sb ?? new StringBuilder();

            if (getterName.Substring(1, 3) == "et_")
                sb.Append(getterName.Substring(4));
            else
                sb.Append(getterName);

            return sb;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var _propListeners = property.FindPropertyRelative("listeners");
            var listenerDrawer = new UnityEventDrawer();
            var listenerHeight = listenerDrawer.GetPropertyHeight(_propListeners, GUIContent.none);
            return listenerHeight + EditorGUIUtility.singleLineHeight * 1.5f + BLOCK_MARGIN_Y;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent removeButton)
        {
            EditorGUI.BeginProperty(rect, removeButton, property);

            var fieldWidth = rect.width * 0.5f;
            var removeButtonWidth = GUI.skin.button.CalcSize(removeButton).x + 8;

            var objectRect = new Rect(rect.x, rect.y + BLOCK_MARGIN_Y,
                fieldWidth - 8, EditorGUIUtility.singleLineHeight);
            var fieldNameRect = new Rect(rect.x + fieldWidth, rect.y + BLOCK_MARGIN_Y,
                fieldWidth - removeButtonWidth, EditorGUIUtility.singleLineHeight);
            var listenerRect = new Rect(rect.x, rect.y + BLOCK_MARGIN_Y + EditorGUIUtility.singleLineHeight * 1.5f,
                rect.width, rect.height - EditorGUIUtility.singleLineHeight * 1.5f - BLOCK_MARGIN_Y);

            var props = PropGroup.Create(property);

            EditorGUI.PropertyField(objectRect, props.target, GUIContent.none);

            var isTargetSet = props.target.objectReferenceValue != (UnityEngine.Object) null;

            //EditorGUI.PropertyField(fieldNameRect, props.getMethodName, GUIContent.none);

            using (new EditorGUI.DisabledGroupScope(!isTargetSet))
            {
                EditorGUI.BeginProperty(fieldNameRect, GUIContent.none, props.getterName);

                var guiDropdownLabelContent = !EditorGUI.showMixedValue
                    ? new GUIContent(GetDropdownLabel(props))
                    : EditorGUIUtility.TrTextContent("-", "Mixed Values");
                
                if (EditorGUI.DropdownButton(fieldNameRect, guiDropdownLabelContent, FocusType.Passive, EditorStyles.popup))
                {
                    GetDropdownMenu(props).DropDown(fieldNameRect);
                }

                EditorGUI.EndProperty();
            }

            if (isTargetSet)
            {
                EditorGUI.PropertyField(listenerRect, props.listeners);
            }

            EditorGUI.EndProperty();
        }

        public static string GetDropdownLabel(PropGroup props)
        {
            StringBuilder label = new StringBuilder();
            var getterName = props.getterName.stringValue;

            if (props.target.objectReferenceValue == (UnityEngine.Object) null
                || string.IsNullOrEmpty(getterName) )
            {
                label.Append("Unset");
            }
            //else if (!) something about IsPersistantListenerValid
            else
            {
                label.Append(props.target.objectReferenceValue.GetType().Name);

                if (!string.IsNullOrEmpty(getterName))
                {
                    label.Append(".");

                    BuildCleanMethodName(getterName, label);
                }
            }

            return label.ToString();
        }

        // Compare to
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/UnityEventDrawer.cs
        public GenericMenu GetDropdownMenu(PropGroup props)
        {
            UnityEngine.Object host = props.target.objectReferenceValue;
            if (host is Component targetComp)
                host = targetComp.gameObject;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Unset"), string.IsNullOrEmpty(props.getterName.stringValue), SetSamplerTarget, new SamplerMethodMap(props, null, null));

            if (host == (UnityEngine.Object)null)
                return menu;

            menu.AddSeparator("");

            List<SamplerMethodMap> samplerMethodMaps = new List<SamplerMethodMap>();
            Component[] hostedComps = (host as GameObject).GetComponents<Component>();

            foreach ( Component comp in hostedComps )
            {
                foreach (var mInfo in GetSamplingMethods(comp))
                {
                    samplerMethodMaps.Add(new SamplerMethodMap(props, comp, mInfo));
                }
            }

            StringBuilder selectionLabel = new StringBuilder();
            Dictionary<string, List<Component>> knownComponents = new Dictionary<string, List<Component>>();
            foreach (var samplerMethodMap in samplerMethodMaps)
            {
                string targetTypeName = samplerMethodMap.TargetTypeName;
                if (!knownComponents.TryGetValue(targetTypeName, out var compList))
                {
                    knownComponents.Add(targetTypeName, new List<Component> { samplerMethodMap.Target });
                }
                else if (!compList.Contains(samplerMethodMap.Target))
                {
                    knownComponents[targetTypeName].Add(samplerMethodMap.Target);
                }

                var sameCompIndex = knownComponents[targetTypeName].IndexOf(samplerMethodMap.Target);
                var isCurrentlySet = props.target.objectReferenceValue == samplerMethodMap.Target
                    && props.getterName.stringValue == samplerMethodMap.GetterName
                    && props.getterTypeName.stringValue == samplerMethodMap.ShortReturnTypeName;

                selectionLabel.Clear();
                selectionLabel.Append(targetTypeName);
                if (sameCompIndex > 0)
                    selectionLabel.Append($" ({sameCompIndex})");

                selectionLabel.Append("/");
                selectionLabel.Append($"{samplerMethodMap.ShortReturnTypeName,-8} ");
                BuildCleanMethodName(samplerMethodMap.GetterName, selectionLabel);
                menu.AddItem(new GUIContent(selectionLabel.ToString()), samplerMethodMap.IsCurrentSampler, SetSamplerTarget, samplerMethodMap);
            }

            return menu;
        }

        public void SetSamplerTarget(object value)
        {
            var samplerMethodMap = (SamplerMethodMap)value;

            if (samplerMethodMap == null)
                return;
            else if (samplerMethodMap.Target == null)
                samplerMethodMap.Clear();
            else
                samplerMethodMap.Apply();
        }

        /// <summary>
        /// Specifically, acquire methods on a component that
        /// return a value or object and do not have parameters
        /// </summary>
        public static IEnumerable<MethodInfo> GetSamplingMethods(Component comp)
        {
            System.Type compType = comp.GetType();

            // "Special Name" methods cannot be called by the user,
            // and should be filtered out.
            // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.isspecialname?view=net-7.0
            var filteredMethods = compType.GetMethods()
                .Where(m =>
                    !m.IsSpecialName &&
                    !m.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() &&
                    !m.GetParameters().Any() &&
                     m.ReturnType != typeof(void) &&
                    !m.ReturnType.IsArray);
            var filteredProperties = compType.GetProperties()
                .Where(p =>
                    !p.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() &&
                     p.GetMethod != null &&
                    !p.PropertyType.IsArray);

            var combinedMethods = filteredProperties.Select(p => p.GetMethod).Concat(filteredMethods);

            return combinedMethods;
        }
    }
}
