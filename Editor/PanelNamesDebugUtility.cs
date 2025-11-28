using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using BattleTurn.UI_Panel.Runtime.Attributes;

namespace BattleTurn.UI_Panel.Editor
{
    internal static class PanelNamesDebugUtility
    {
        [MenuItem("Tools/PanelNames/Log PanelNames Dictionary")] 
        private static void LogDict()
        {
            try
            {
                var dict = PanelNamesAttribute.PanelNames;
                Debug.Log("[PanelNamesDebug] Count=" + dict.Count + " -> " + string.Join(", ", dict.Select(kv => kv.Key+":"+kv.Value)));
            }
            catch (Exception ex)
            {
                Debug.LogError("[PanelNamesDebug] Error: " + ex);
            }
        }

        [MenuItem("Tools/PanelNames/Inspect PanelManager PanelInfo Fields")] 
        private static void InspectPanelInfo()
        {
            var t = Type.GetType("SimplePanel.PanelManager+PanelInfo, Assembly-CSharp");
            if (t == null) { Debug.LogWarning("[PanelNamesDebug] PanelInfo type not found"); return; }
            foreach (var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                var attrs = f.GetCustomAttributes(true).Select(a=>a.GetType().Name).ToArray();
                Debug.Log($"[PanelNamesDebug] Field {f.Name} Attrs=[{string.Join(",", attrs)}] Serialized={(f.IsPublic || f.GetCustomAttribute<SerializeField>()!=null)}");
            }
        }
    }
}
