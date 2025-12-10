using System;
using System.IO;

namespace GameDialog.Lang
{
    /// <summary>
    /// Represents a source code input for the Game Dialog Script language.
    /// </summary>
    public readonly struct Source
    {
        /// <summary>
        /// The type of source code input.
        /// </summary>
        public readonly SourceType Type;

        /// <summary>
        /// The value associated with the source (either the inline code or the file path).
        /// </summary>
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Source"/> struct.
        /// </summary>
        private Source(SourceType type, string value)
        {
            Type = type;
            _value = value;
        }

        /// <summary>
        /// Creates a new <see cref="Source"/> instance from an inline string.
        /// </summary>
        public static Source Inline(string value)
        {
            return new Source(SourceType.Inline, value);
        }

        /// <summary>
        /// Creates a new <see cref="Source"/> instance from a file path.
        /// </summary>
        public static Source FromFile(string filePath)
        {
            return new Source(SourceType.File, filePath);
        }

        /// <summary>
        /// Creates an empty <see cref="Source"/> instance.
        /// </summary>
        public static Source Empty()
        {
            return new Source(SourceType.None, string.Empty);
        }

        /// <summary>
        /// Gets a TextReader for reading the source code.
        /// </summary>
        public TextReader CreateReader()
        {
            return Type switch
            {
                SourceType.None => throw new InvalidOperationException("Cannot create reader for empty source."),
                SourceType.Inline => new StringReader(_value),
                SourceType.File => new StreamReader(File.OpenRead(_value)),
                _ => throw new NotSupportedException($"Unknown source type: {Type}"),
            };
        }

        /// <summary>
        /// Equality operator for Source struct.
        /// </summary>
        public static bool operator ==(Source left, Source right)
        {
            return left.Type == right.Type && left._value == right._value;
        }

        /// <summary>
        /// Inequality operator for Source struct.
        /// </summary>
        public static bool operator !=(Source left, Source right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Source.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is Source other)
            {
                return this == other;
            }
            return false;
        }

        /// <summary>
        /// Returns a string representation of the source.
        /// </summary>
        public override string ToString()
        {
            return Type switch
            {
                SourceType.None => "Empty",
                SourceType.Inline => "Inline",
                SourceType.File => $"File \"{_value}\"",
                _ => "<unknown source>",
            };
        }

        /// <summary>
        /// Gets the hash code for the Source struct.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, _value);
        }
    }
}