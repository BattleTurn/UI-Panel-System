using System.Linq;
using UnityEngine;
using UnityEditor;
using BattleTurn.UI_Panel.Runtime.Attributes;

namespace BattleTurn.UI_Panel.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(PanelNameDropdownAttribute), true)]
    internal sealed class PanelNamesDropdownDrawer : PropertyDrawer
    {
        private static bool _attemptedRefresh;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (attribute is not PanelNameDropdownAttribute || property.propertyType != SerializedPropertyType.String)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }

            var dict = PanelNamesAttribute.PanelNames;
            bool onlyDefault = dict == null || dict.Count == 0 || (dict.Count == 1 && dict.ContainsKey("<default>"));

            // Add extra height for help box if only default is present
            if (onlyDefault)
            {
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight * 1.2f + 4f;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Debug: Always log when drawer is called

            if (attribute is not PanelNameDropdownAttribute || property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var dict = PanelNamesAttribute.PanelNames;

            // Auto refresh once if only <default> present
            if (!_attemptedRefresh && dict.Count <= 1)
            {
                _attemptedRefresh = true;
                PanelNamesAttribute.Refresh();
                dict = PanelNamesAttribute.PanelNames;
            }

            if (dict == null || dict.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Check if only <default> present
            bool onlyDefault = dict.Count == 1 && dict.ContainsKey("<default>");

            // Calculate rects
            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect helpRect = new Rect(position.x, position.yMax - EditorGUIUtility.singleLineHeight * 1.2f - 2f, position.width, EditorGUIUtility.singleLineHeight * 1.2f);

            var entries = dict
                .OrderBy(e => e.Key == "<default>" ? 0 : 1)
                .ThenBy(e => e.Key == "<default>" ? string.Empty : e.Key)
                .ToList();

            var display = entries.Select(e => e.Key).ToArray();
            var values = entries.Select(e => e.Value).ToArray();

            string current = property.stringValue;
            int selected = 0;
            if (!string.IsNullOrEmpty(current))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == current) { selected = i; break; }
                }
            }

            // Draw dropdown
            EditorGUI.BeginChangeCheck();
            int newSel = EditorGUI.Popup(fieldRect, label.text, selected, display);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                property.stringValue = values[newSel];
            }

            // Show help box if only default is present
            if (onlyDefault)
            {
                EditorGUI.HelpBox(helpRect, "No panel names found. Add a static class with [PanelNames] and public const string fields.", MessageType.Info);
            }
        }
    }
}