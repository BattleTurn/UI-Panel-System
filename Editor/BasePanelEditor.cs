using UnityEngine;
using UnityEditor;
using BattleTurn.UI_Panel.Runtime;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace BattleTurn.UI_Panel.Editor
{
    [CustomEditor(typeof(BasePanel), true)]
    public class BasePanelEditor :
#if ODIN_INSPECTOR
        OdinEditor
#else
        UnityEditor.Editor
#endif
    {

#if ODIN_INSPECTOR
        private PropertyTree _tree;
#else
        private SerializedProperty id;
        private SerializedProperty panelManager;
#endif

#if ODIN_INSPECTOR
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // Read-only display of id (still invokes property drawer so dropdown appears but is disabled)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(id, new GUIContent("Panel ID", "The ID of the panel. This should be unique for each panel."));
            EditorGUI.EndDisabledGroup();

            // Draw rest excluding script and id (already drawn)
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(id), nameof(panelManager));
            serializedObject.ApplyModifiedProperties();
        }
#endif

#if ODIN_INSPECTOR
        protected override void OnEnable()
        {
            base.OnEnable();
        }
#else
        private void OnEnable()
        {
            id = serializedObject.FindProperty(nameof(id));
            panelManager = serializedObject.FindProperty(nameof(panelManager));
        }
#endif
    }
}