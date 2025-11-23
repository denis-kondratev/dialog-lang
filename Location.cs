using System;

namespace BitPatch.DialogLang
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
        /// End column (1-based, inclusive).
        /// </summary>
        public int Final { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct.
        /// </summary>
        public Location(Source source, int line, int position) : this(source, line, position, position)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct.
        /// </summary>
        public Location(Source source, int line, int initial, int final)
        {
            Source = source;
            Line = line;
            Initial = initial;
            Final = final;
        }

        /// <summary>
        /// Returns a new location immediately after the current one.
        /// </summary>
         public Location After()
        {
            return new Location(Source, Line, Final, Final + 1);
        }

        /// <summary>
        /// Combines two locations into a single range spanning from the start of the first to the end of the second.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the sources or lines of the two locations differ.</exception>
        public static Location operator |(Location left, Location right)
        {
            if (left.Source != right.Source)
            {
                throw new ArgumentException($"Cannot combine locations from different sources. Left source: {left.Source}], Right source: {right.Source}");
            }

            if (left.Line != right.Line)
            {
                throw new ArgumentException($"Cannot combine locations from different lines. Left line: {left.Line}, Right line: {right.Line}");
            }

            return new Location(left.Source, left.Line, left.Initial, right.Final);
        }

        /// <summary>
        /// Returns a string representation of the location.
        /// </summary>
        public override string ToString()
        {
            return Initial == Final
                ? $"Source {Source}, Line {Line}, Col {Initial}"
                : $"Source {Source}, Line {Line}, Col {Initial}-{Final}";
        }
    }
}
