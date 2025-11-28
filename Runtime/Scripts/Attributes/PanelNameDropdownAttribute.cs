using System;
using UnityEngine;

namespace BattleTurn.UI_Panel.Runtime.Attributes
{
    /// <summary>
    /// Attribute to specify a panel name for a field in the Inspector.
    /// </summary>
    /// <remarks>
    /// This attribute is used to link a string field to a panel name from a database.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class PanelNameDropdownAttribute : PropertyAttribute
    {
    }
}