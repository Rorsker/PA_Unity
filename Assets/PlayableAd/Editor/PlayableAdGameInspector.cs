using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PlayableAd.Editor
{
    [CustomEditor(typeof(PlayableAdGame))]
    [CanEditMultipleObjects]
    public sealed class PlayableAdGameInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    DrawProperty(property);
                } while (property.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawProperty(SerializedProperty property)
        {
            bool isScript = property.propertyPath == "m_Script";
            bool drawFoldout = property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren;
            if (!drawFoldout)
            {
                using (new EditorGUI.DisabledScope(isScript))
                {
                    EditorGUILayout.PropertyField(property, GetLabel(property), false);
                }
                return;
            }

            if (property.isArray && (property.name == "soldierSections" || property.name == "additionalStoneWalls"))
            {
                DrawModuleArray(property);
                return;
            }

            DrawHeader(property);

            Rect foldoutRect = EditorGUILayout.GetControlRect();
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GetLabel(property), true);
            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            SerializedProperty child = property.Copy();
            SerializedProperty end = child.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                DrawProperty(child);
                enterChildren = false;
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawModuleArray(SerializedProperty property)
        {
            DrawHeader(property);
            Rect foldoutRect = EditorGUILayout.GetControlRect();
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GetLabel(property), true);
            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < property.arraySize; i++)
                DrawProperty(property.GetArrayElementAtIndex(i));

            using (new EditorGUI.DisabledScope(property.serializedObject.isEditingMultipleObjects))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add（新增）")) AddModuleElement(property);
                using (new EditorGUI.DisabledScope(property.arraySize == 0))
                {
                    if (GUILayout.Button("Remove Last（删除末项）"))
                        property.DeleteArrayElementAtIndex(property.arraySize - 1);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private static void AddModuleElement(SerializedProperty property)
        {
            int index = property.arraySize;
            float previousOffset = GetMaximumStartOffset(property);
            property.arraySize++;
            SerializedProperty element = property.GetArrayElementAtIndex(index);

            if (property.name == "soldierSections")
            {
                element.FindPropertyRelative("sectionName").stringValue = (index + 1).ToString();
                element.FindPropertyRelative("startOffsetFromTutorial").floatValue = previousOffset + 18.75f;
                element.FindPropertyRelative("soldierCount").intValue = 5;
                element.FindPropertyRelative("placementMode").enumValueIndex = 0;
                element.FindPropertyRelative("minimumForwardSpacing").floatValue = 0.5f;
                element.FindPropertyRelative("horizontalCoverage").floatValue = 1f;
                element.FindPropertyRelative("forwardRandomness").floatValue = 0.8f;
                return;
            }

            element.FindPropertyRelative("sectionName").stringValue = "StoneWall" + (index + 1);
            float nextOffset = previousOffset + 62.5f;
            float maximumOffset = GetMaximumStoneWallOffset(property.serializedObject);
            if (nextOffset > maximumOffset)
            {
                nextOffset = maximumOffset;
                if (previousOffset >= maximumOffset)
                    Debug.LogWarning("No free course distance remains before the Boss for another stone wall.",
                        property.serializedObject.targetObject);
            }
            element.FindPropertyRelative("startOffsetFromTutorial").floatValue = nextOffset;
            element.FindPropertyRelative("blockingMode").enumValueIndex = 0;
            SerializedProperty bulletTime = element.FindPropertyRelative("bulletTime");
            bulletTime.FindPropertyRelative("enabled").boolValue = false;
            bulletTime.FindPropertyRelative("triggerDistance").floatValue = 3f;
            bulletTime.FindPropertyRelative("duration").floatValue = 0.55f;
            bulletTime.FindPropertyRelative("worldTimeScale").floatValue = 0.25f;
            bulletTime.FindPropertyRelative("enterDuration").floatValue = 0.2f;
            bulletTime.FindPropertyRelative("exitDuration").floatValue = 0.15f;
        }

        private static float GetMaximumStartOffset(SerializedProperty array)
        {
            float maximum = 0f;
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty offset = array.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("startOffsetFromTutorial");
                if (offset != null) maximum = Mathf.Max(maximum, offset.floatValue);
            }
            return maximum;
        }

        private static float GetMaximumStoneWallOffset(SerializedObject serializedObject)
        {
            SerializedProperty forwardSpeeds = serializedObject.FindProperty("playerSpeed.forwardSpeeds");
            float openingForwardSpeed = forwardSpeeds != null && forwardSpeeds.isArray && forwardSpeeds.arraySize > 0
                ? forwardSpeeds.GetArrayElementAtIndex(0).floatValue
                : 6f;
            float openingElixirZ = GetFloat(serializedObject, "tuning.openingElixirTime", 1.23f)
                * openingForwardSpeed;
            int soldierCount = Mathf.Clamp(GetInt(serializedObject, "tuning.tutorialSoldierCount", 5), 3, 5);
            float tutorialEndZ = openingElixirZ
                + GetFloat(serializedObject, "tuning.tutorialFirstSoldierGap", 2.46f)
                + (soldierCount - 1) * GetFloat(serializedObject, "tuning.tutorialSoldierSpacing", 1.85f)
                + GetFloat(serializedObject, "tuning.tutorialWallGap", 6.15f);
            float bossDistance = GetFloat(serializedObject, "tuning.bossDistance", 500f);
            float bossPadding = GetFloat(serializedObject, "tuning.bossApproachPadding", 13.46154f);
            return Mathf.Max(0f, bossDistance - tutorialEndZ - bossPadding);
        }

        private static float GetFloat(SerializedObject serializedObject, string path, float fallback)
        {
            SerializedProperty value = serializedObject.FindProperty(path);
            return value != null ? value.floatValue : fallback;
        }

        private static int GetInt(SerializedObject serializedObject, string path, int fallback)
        {
            SerializedProperty value = serializedObject.FindProperty(path);
            return value != null ? value.intValue : fallback;
        }

        private static void DrawHeader(SerializedProperty property)
        {
            FieldInfo field = FindField(property.serializedObject.targetObject.GetType(), property.propertyPath);
            HeaderAttribute header = field != null ? field.GetCustomAttribute<HeaderAttribute>() : null;
            if (header != null) EditorGUILayout.LabelField(header.header, EditorStyles.boldLabel);
        }

        private static GUIContent GetLabel(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.propertyPath.Contains(".Array.data["))
            {
                SerializedProperty sectionName = property.FindPropertyRelative("sectionName");
                if (sectionName != null && !string.IsNullOrEmpty(sectionName.stringValue))
                    return new GUIContent(sectionName.stringValue, property.tooltip);
            }

            FieldInfo field = FindField(property.serializedObject.targetObject.GetType(), property.propertyPath);
            if (field != null)
            {
                InspectorNameAttribute inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
                if (inspectorName != null && !string.IsNullOrEmpty(inspectorName.displayName))
                    return new GUIContent(inspectorName.displayName, property.tooltip);
            }

            return new GUIContent(property.displayName, property.tooltip);
        }

        private static FieldInfo FindField(Type rootType, string propertyPath)
        {
            Type currentType = rootType;
            FieldInfo result = null;
            string[] parts = propertyPath.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data[", StringComparison.Ordinal))
                {
                    currentType = currentType.IsArray
                        ? currentType.GetElementType()
                        : GetCollectionElementType(currentType);
                    i++;
                    if (i == parts.Length - 1) return null;
                    result = null;
                    continue;
                }

                result = GetField(currentType, part);
                if (result == null)
                    return null;

                currentType = result.FieldType;
            }

            return result;
        }

        private static FieldInfo GetField(Type type, string name)
        {
            if (type == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (type != null)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                    return field;
                type = type.BaseType;
            }

            return null;
        }

        private static Type GetCollectionElementType(Type type)
        {
            if (type == null)
                return null;
            if (type.IsArray)
                return type.GetElementType();
            if (type.IsGenericType && type.GetGenericArguments().Length == 1)
                return type.GetGenericArguments()[0];
            return null;
        }
    }
}
