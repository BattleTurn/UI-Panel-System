using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using BattleTurn.UI_Panel.Runtime;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace BattleTurn.UI_Panel.Editor
{
    /// <summary>
    /// Custom editor for the PanelManager class.
    /// </summary>
    [CustomEditor(typeof(PanelManager))]
    public class PanelManagerEditor :
#if ODIN_INSPECTOR
        OdinEditor
#else
        UnityEditor.Editor
#endif
    {
        private static string folderPath = $"Assets/Plugins/{nameof(BattleTurn)}/{nameof(UI_Panel)}/Generated";
        
        // Serialized fields in PanelManager
        private SerializedProperty _panelMapProp;
        private SerializedProperty _containerProp;
        private SerializedProperty _uiCameraProp;
        private SerializedProperty _uiCanvasProp;

        // Add-panel UI state (editor-only, not serialized in target)
        private bool _showAddPanel;
        private string _newId = string.Empty;
        private BasePanel _newPanel;
        private PanelType _newType = PanelType.Screen;
        private bool _newDestroyOnHide;

        private void OnEnable()
        {
            _panelMapProp = serializedObject.FindProperty("_panelMap");
            _containerProp = serializedObject.FindProperty("_container");
            _uiCameraProp = serializedObject.FindProperty("_uiCamera");
            _uiCanvasProp = serializedObject.FindProperty("_uiCanvas");
        }

        // Auto generation after script reload if missing PanelIds file
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            TryForceGenerateIfMissing();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw basic references
            EditorGUILayout.LabelField("Panel Manager", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_uiCameraProp);
            EditorGUILayout.PropertyField(_uiCanvasProp);
            EditorGUILayout.PropertyField(_containerProp);

            EditorGUILayout.Space();

            // Draw panel map and add button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Panels (Count: {_panelMapProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                _showAddPanel = !_showAddPanel;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_panelMapProp, includeChildren: true);

            // Add new panel UI
            if (_showAddPanel)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Add Panel", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    _newId = EditorGUILayout.TextField("Id", _newId);
                    _newPanel = (BasePanel)EditorGUILayout.ObjectField("Prefab", _newPanel, typeof(BasePanel), false);
                    _newType = (PanelType)EditorGUILayout.EnumPopup("Type", _newType);
                    _newDestroyOnHide = EditorGUILayout.Toggle("Destroy On Hide", _newDestroyOnHide);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add to List"))
                    {
                        if (TryAddPanelEntry())
                        {
                            _showAddPanel = false;
                            _newId = string.Empty;
                            _newPanel = null;
                            _newType = PanelType.Screen;
                            _newDestroyOnHide = false;
                        }
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        _showAddPanel = false;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            // APPLY: generate PanelIds static class
            if (GUILayout.Button("Apply (Generate PanelIds)"))
            {
                serializedObject.ApplyModifiedProperties();
                GeneratePanelIds((PanelManager)target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool TryAddPanelEntry()
        {
            if (string.IsNullOrWhiteSpace(_newId))
            {
                EditorUtility.DisplayDialog("Add Panel", "Id cannot be empty.", "OK");
                return false;
            }
            if (_newPanel == null)
            {
                EditorUtility.DisplayDialog("Add Panel", "Please assign a BasePanel prefab.", "OK");
                return false;
            }

            // Prevent duplicate IDs
            for (int i = 0; i < _panelMapProp.arraySize; i++)
            {
                var el = _panelMapProp.GetArrayElementAtIndex(i);
                var idProp = el.FindPropertyRelative("Id");
                if (idProp != null && string.Equals(idProp.stringValue, _newId, StringComparison.Ordinal))
                {
                    EditorUtility.DisplayDialog("Add Panel", $"Id '{_newId}' already exists.", "OK");
                    return false;
                }
            }

            int newIndex = _panelMapProp.arraySize;
            _panelMapProp.InsertArrayElementAtIndex(newIndex);
            var newEl = _panelMapProp.GetArrayElementAtIndex(newIndex);
            newEl.FindPropertyRelative("Id").stringValue = _newId;
            newEl.FindPropertyRelative("Panel").objectReferenceValue = _newPanel;
            newEl.FindPropertyRelative("Type").enumValueIndex = (int)_newType;
            newEl.FindPropertyRelative("destroyOnHide").boolValue = _newDestroyOnHide;

            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static void GeneratePanelIds(PanelManager panelManager)
        {
            try
            {
                var listField = typeof(PanelManager).GetField("_panelMap", BindingFlags.NonPublic | BindingFlags.Instance);
                var list = listField?.GetValue(panelManager) as IEnumerable<PanelInfo>;
                var ids = (list ?? Enumerable.Empty<PanelInfo>())
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Id))
                    .Select(p => p.Id)
                    .Distinct()
                    .ToList();

                string folder = folderPath;
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string className = "PanelNames";
                string ns = $"{nameof(BattleTurn)}.{nameof(UI_Panel)}";
                string path = Path.Combine(folder, className + ".cs");

                string content = BuildNameClass(ns, className, ids);
                File.WriteAllText(path, content);
                AssetDatabase.Refresh();
                Debug.Log($"[PanelManagerEditor] Generated {className} with {ids.Count} ids at: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[PanelManagerEditor] Failed to generate PanelIds: " + ex);
            }
        }

        /// <summary>
        /// Checks if the auto-generated PanelIds file exists; if not, attempts generation using the first found PanelManager in the scene.
        /// </summary>
        private static void TryForceGenerateIfMissing()
        {
            
            const string fileName = "PanelNames.cs";
            string path = Path.Combine(folderPath, fileName);
            if (File.Exists(path)) return; // Already present

            var pm = FindFirstObjectByType<PanelManager>();
            if (pm == null)
            {
                // Attempt to load from prefabs if no instance in scene
                string[] guids = AssetDatabase.FindAssets("t:Prefab PanelManager");
                foreach (var guid in guids)
                {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null && prefab.TryGetComponent<PanelManager>(out var found))
                    {
                        pm = found;
                        break;
                    }
                }
            }
            if (pm == null)
            {
                Debug.LogWarning("[PanelManagerEditor] PanelIds missing but no PanelManager instance/prefab found to generate from.");
                return;
            }

            Debug.Log("[PanelManagerEditor] PanelIds file missing after recompile. Forcing generation...");
            GeneratePanelIds(pm);
        }

        private static string BuildNameClass(string ns, string className, IList<string> ids)
        {
            var lines = new List<string>();
            lines.Add("// <auto-generated/> Do not modify by hand.");
            lines.Add("#pragma warning disable 1591");
            lines.Add($"namespace {ns}");
            lines.Add("{");
            lines.Add($"    public static class {className}");
            lines.Add("    {");

            var nameMap = new Dictionary<string, string>();
            var used = new HashSet<string>();
            foreach (var id in ids)
            {
                string field = SanitizeToIdentifier(id);
                string baseField = field;
                int suffix = 1;
                while (used.Contains(field))
                {
                    field = baseField + "_" + suffix++;
                }
                used.Add(field);
                nameMap[field] = id;
            }

            foreach (var kv in nameMap)
            {
                lines.Add($"        public const string {kv.Key} = \"{kv.Value}\";");
            }

            string allJoin = string.Join(", ", nameMap.Keys.Select(k => k));
            lines.Add($"        public static readonly string[] All = new[] {{ {allJoin} }};")
;
            lines.Add("    }");
            lines.Add("}");
            return string.Join("\n", lines);
        }

        private static string SanitizeToIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return "ID_Empty";
            var chars = id.Select(c => (char.IsLetterOrDigit(c) || c == '_') ? c : '_').ToArray();
            string s = new string(chars);
            if (char.IsDigit(s[0])) s = "ID_" + s;
            while (s.Contains("__")) s = s.Replace("__", "_");
            return s;
        }
    }
}