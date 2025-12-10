using System;
using System.Collections.Generic;

namespace GameDialog.Lang
{
    /// <summary>
    /// Base type for all runtime items in the interpreter.
    /// Provides a foundation for extensibility with future types like None, List, Index, etc.
    /// </summary>
    public abstract record RuntimeItem
    {
        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public abstract string GetTypeName();

        /// <summary>
        /// The display name for runtime item types.
        /// </summary>
        public static string DisplayName => "item";

        /// <summary>
        /// Mapping of runtime item types to their display names.
        /// </summary>
        private static readonly Dictionary<Type, string> TypeDisplayNames = new()
        {
            { typeof(RuntimeItem), RuntimeItem.DisplayName },
            { typeof(RuntimeValue), RuntimeValue.DisplayName },
            { typeof(RuntimeNumber), RuntimeNumber.DisplayName },
            { typeof(RuntimeInteger), RuntimeInteger.DisplayName },
            { typeof(RuntimeFloat), RuntimeFloat.DisplayName },
            { typeof(RuntimeString), RuntimeString.DisplayName },
            { typeof(RuntimeBoolean), RuntimeBoolean.DisplayName },
        };

        /// <summary>
        /// Gets the display name for a specific runtime item type.
        /// </summary>
        public static string GetDisplayName<T>() where T : RuntimeItem
        {
            return TypeDisplayNames.TryGetValue(typeof(T), out var displayName)
                ? displayName
                : throw new NotSupportedException($"Type {typeof(T).Name} is not a supported RuntimeItem type.");
        }
    }
}