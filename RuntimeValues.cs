namespace BitPatch.DialogLang
{
    /// <summary>
    /// Represents a runtime value in the interpreter.
    /// This is a discriminated union of all possible value types.
    /// </summary>
    public abstract record RuntimeValue : RuntimeItem
    {
        /// <summary>
        /// Gets the underlying value as an object.
        /// </summary>
        /// <returns>The value as an object.</returns>
        public abstract object GetValue();

        /// <summary>
        /// The display name for value types.
        /// </summary>
        public new static string DisplayName => "value";
    }

    /// <summary>
    /// Base class for numeric runtime values.
    /// </summary>
    public abstract record RuntimeNumber : RuntimeValue
    {
        /// <summary>
        /// Gets a value indicating whether this number represents a nil/zero value.
        /// </summary>
        public abstract bool IsNil { get; }

        /// <summary>
        /// Gets the floating-point representation of this number.
        /// </summary>
        public abstract float FloatValue { get; }

        /// <summary>
        /// The display name for number types.
        /// </summary>
        public new static string DisplayName => "number";
    }

    /// <summary>
    /// Integer runtime value.
    /// </summary>
    public sealed record RuntimeInteger(int Value) : RuntimeNumber
    {
        /// <summary>
        /// Gets a value indicating whether this integer is zero.
        /// </summary>
        public override bool IsNil => Value is 0;

        /// <summary>
        /// Gets the floating-point representation of this integer.
        /// </summary>
        public override float FloatValue => Value;

        /// <summary>
        /// Returns the integer value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public override string GetTypeName() => DisplayName;

        /// <summary>
        /// The display name for integer types.
        /// </summary>
        public new static string DisplayName => "integer";

        /// <summary>
        /// Returns the string representation of this integer.
        /// </summary>
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Float runtime value.
    /// </summary>
    public sealed record RuntimeFloat(float Value) : RuntimeNumber
    {
        /// <summary>
        /// Gets a value indicating whether this float is zero.
        /// </summary>
        public override bool IsNil => Value is 0.0f;

        /// <summary>
        /// Gets the floating-point representation of this float.
        /// </summary>
        public override float FloatValue => Value;

        /// <summary>
        /// Returns the float value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public override string GetTypeName() => DisplayName;

        /// <summary>
        /// The display name for float types.
        /// </summary>
        public new static string DisplayName => "float";

        /// <summary>
        /// Returns the string representation of this float.
        /// </summary>
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// String runtime value.
    /// </summary>
    public sealed record RuntimeString(string Value) : RuntimeValue
    {
        /// <summary>
        /// Returns the string value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public override string GetTypeName() => DisplayName;

        /// <summary>
        /// The display name for string types.
        /// </summary>
        public new static string DisplayName => "string";

        /// <summary>
        /// Returns the string representation of this string.
        /// </summary>
        public override string ToString() => Value;
    }

    /// <summary>
    /// Boolean runtime value.
    /// </summary>
    public sealed record RuntimeBoolean(bool Value) : RuntimeValue
    {
        /// <summary>
        /// Returns the boolean value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public override string GetTypeName() => DisplayName;

        /// <summary>
        /// The display name for boolean types.
        /// </summary>
        public new static string DisplayName => "boolean";

        /// <summary>
        /// Returns the string representation of this boolean.
        /// </summary>
        public override string ToString() => Value ? "true" : "false";
    }
}
