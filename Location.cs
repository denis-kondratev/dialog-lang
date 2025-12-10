using System;

namespace GameDialog.Lang
{
    /// <summary>
    /// Represents a position range in the source code.
    /// </summary>
    internal readonly struct Location
    {
        /// <summary>
        /// The source code input.
        /// </summary>
        public Source Source { get; }

        /// <summary>
        /// Line number (1-based).
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Start column (1-based).
        /// </summary>
        public int Initial { get; }

        /// <summary>
        /// End column (1-based, exclusive).
        /// </summary>
        public int Final { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct.
        /// </summary>
        public Location(Source source, int line, int position) : this(source, line, position, position + 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct.
        /// </summary>
        public Location(Source source, int line, int initial, int final)
        {
            if (final <= initial)
            {
                throw new ArgumentException("Final position must be greater than initial position.");
            }

            Source = source;
            Line = line;
            Initial = initial;
            Final = final;
        }

        /// <summary>
        /// Combines two locations into a single range spanning from the start of the first to the end of the second.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the sources or lines of the two locations differ.</exception>
        public static Location operator +(Location left, Location right)
        {
            if (left.Source != right.Source)
            {
                throw new ArgumentException($"Mismatched sources. Left source: {left.Source}, Right source: {right.Source}");
            }

            if (left.Line != right.Line)
            {
                throw new ArgumentException($"Mismatched lines. Left line: {left.Line}, Right line: {right.Line}");
            }

            return new Location(left.Source, left.Line, left.Initial, right.Final);
        }

        /// <summary>
        /// Extends the location to a new final position based on the current position of the reader.
        /// </summary>
        /// <param name="location">The base location.</param>
        /// <param name="reader">The reader providing the current position.</param>
        /// <returns>
        /// A new location starting at <c>Initial</c> and ending at the reader's current
        /// column on the same line and source.
        /// </returns>
        public static Location operator |(Location location, Reader reader)
        {
            if (reader.Source != location.Source)
            {
                throw new ArgumentException($"Mismatched sources, location: {location.Source}, reader: {reader.Source}");
            }

            if (reader.Line != location.Line)
            {
                throw new ArgumentException($"Mismatched lines, location: {location.Line}, reader: {reader.Line}");
            }

            if (reader.Column < location.Initial)
            {
                throw new ArgumentException($"Final position too small, ({location}) | ({reader.Column})");
            }
            
            if (location.Final >= reader.Column)
            {
                return location;
            }

            return new Location(location.Source, location.Line, location.Initial, reader.Column);
        }

        /// <summary>
        /// Extends the location to a new final position.
        /// </summary>
        /// <param name="location">The base location.</param>
        /// <param name="final">The new final position.</param>
        /// <returns>
        /// A new location starting at <c>Initial</c> and ending at the specified final position
        /// on the same line and source.
        /// </returns>
        public static Location operator |(Location location, int final)
        {
            if (final < location.Initial)
            {
                throw new ArgumentException($"Final position too small, ({location}) | ({final})");
            }
            
            if (location.Final >= final)
            {
                return location;
            }

            return new Location(location.Source, location.Line, location.Initial, final);
        }

        /// <summary>
        /// Returns a string representation of the location.
        /// </summary>
        public override string ToString()
        {
            var source = Source.Type switch
            {
                SourceType.Inline => "",
                SourceType.File => $"{Source}, ",
                SourceType.None => "",
                _ => throw new NotSupportedException($"Unknown source type: {Source.Type}"),
            };

            return Initial == Final - 1
                ? source + $"Line {Line}, Col {Initial}"
                : source + $"Line {Line}, Col {Initial}-{Final - 1}";
        }
    }
}
