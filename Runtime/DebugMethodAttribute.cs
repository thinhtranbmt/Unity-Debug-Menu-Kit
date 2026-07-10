using System;

namespace DebugMenuKit
{
    /// <summary>
    /// Marks a method as a debug-menu button. Put it on any MonoBehaviour placed on the
    /// DebugMenu prefab instance (or its children) and it shows up automatically:
    /// category = first-level button, name = second-level button.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class DebugMethodAttribute : Attribute
    {
        public readonly string Category;
        public readonly string Name;

        public DebugMethodAttribute(string category = "", string name = "")
        {
            Category = category;
            Name = name;
        }
    }
}
