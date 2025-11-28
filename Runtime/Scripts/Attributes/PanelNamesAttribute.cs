using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BattleTurn.UI_Panel.Runtime.Attributes
{
    /// <summary>
    /// Place this attribute on a static class that contains public const/static string fields representing panel names.
    /// Example:
    /// [PanelNames]
    /// public static class PanelIds { public const string Inventory = "Inventory"; }
    /// The collected map (fieldName -> value) is exposed via PanelNamesAttribute.PanelNames.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PanelNamesAttribute : Attribute
    {
        private const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static bool _built;
        private static readonly Dictionary<string, string> _panelNames = new(StringComparer.Ordinal);
        private static readonly List<Type> _sources = new();

        public static IReadOnlyDictionary<string, string> PanelNames
        {
            get
            {
                if (!_built) BuildAll();
                return _panelNames;
            }
        }

        public static IReadOnlyList<Type> SourceTypes
        {
            get
            {
                if (!_built) BuildAll();
                return _sources;
            }
        }

        // Ensure <default> always present
        private static void EnsureDefault()
        {
            if (!_panelNames.ContainsKey("<default>"))
                _panelNames["<default>"] = string.Empty;
        }

        private static void BuildAll()
        {
            _panelNames.Clear();
            _sources.Clear();
            EnsureDefault();
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    foreach (var t in types)
                    {
                        if (t.GetCustomAttribute<PanelNamesAttribute>() == null) continue;
                        if (!t.IsAbstract || !t.IsSealed)
                        {
                            Debug.LogWarning($"[PanelNames] Type {t.FullName} is not a static class; skipping.");
                            continue;
                        }
                        _sources.Add(t);
                        Extract(t);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PanelNames] Build error: {ex.Message}");
            }
            _built = true;
        }

        private static void Extract(Type t)
        {
            foreach (var f in t.GetFields(FLAGS))
            {
                if (f.FieldType != typeof(string)) continue;
                string name = f.Name;
                string value = string.Empty;
                try
                {
                    if (f.IsLiteral && !f.IsInitOnly)
                        value = f.GetRawConstantValue()?.ToString() ?? string.Empty;
                    else
                        value = f.GetValue(null)?.ToString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PanelNames] Failed field {t.FullName}.{name}: {ex.Message}");
                }
                _panelNames[name] = value;
            }
        }

        /// <summary> Force a rebuild (eg. after domain reload if needed). </summary>
        public static void Refresh()
        {
            _built = false;
            BuildAll();
        }
    }
}