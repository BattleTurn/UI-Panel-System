using UnityEditor;
using BattleTurn.UI_Panel.Runtime.Attributes;

namespace BattleTurn.UI_Panel.Editor
{
    /// <summary>
    /// Ensures the panel names dictionary is built early after a domain reload so first inspector draw has data.
    /// </summary>
    [InitializeOnLoad]
    internal static class PanelNamesBootstrap
    {
        static PanelNamesBootstrap()
        {
            // Touch the dictionary to trigger lazy build.
            _ = PanelNamesAttribute.PanelNames;
        }
    }
}