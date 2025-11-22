namespace BitPatch.DialogLang
{
    /// <summary>
    /// Base type for all runtime items in the interpreter.
    /// Provides a foundation for extensibility with future types like None, List, Index, etc.
    /// </summary>
    public abstract record RuntimeItem;

    /// <summary>
    /// Represents a runtime value in the interpreter.
    /// This is a discriminated union of all possible value types.
    /// </summary>
    public abstract record Value : RuntimeItem
    {
        /// <summary>
        /// Gets the underlying value as an object.
        /// </summary>
        /// <returns>The value as an object.</returns>
        public abstract object GetValue();
    }

    /// <summary>
    /// Base class for numeric runtime values.
    /// </summary>
    public abstract record Number : Value
    {
        /// <summary>
        /// Gets a value indicating whether this number represents a nil/zero value.
        /// </summary>
        public abstract bool IsNil { get; }

        /// <summary>
        /// Gets the floating-point representation of this number.
        /// </summary>
        public abstract float FloatValue { get; }
    }

    /// <summary>
    /// Integer runtime value.
    /// </summary>
    public sealed record Integer(int Value) : Number
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
        /// Returns the string representation of this integer.
        /// </summary>
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Float runtime value.
    /// </summary>
    public sealed record Float(float Value) : Number
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
        /// Returns the string representation of this float.
        /// </summary>
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// String runtime value.
    /// </summary>
    public sealed record String(string Value) : Value
    {
        /// <summary>
        /// Returns the string value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Returns the string representation of this string.
        /// </summary>
        public override string ToString() => Value;
    }

    /// <summary>
    /// Boolean runtime value.
    /// </summary>
    public sealed record Boolean(bool Value) : Value
    {
        /// <summary>
        /// Returns the boolean value itself.
        /// </summary>
        public override object GetValue() => Value;

        /// <summary>
        /// Returns the string representation of this boolean.
        /// </summary>
        public override string ToString() => Value ? "true" : "false";
    }
}
