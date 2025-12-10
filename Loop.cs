namespace GameDialog.Lang
{
    /// <summary>
    /// Information about loop iterations for preventing infinite loops.
    /// </summary>
    internal class Loop
    {
        /// <summary>
        /// The line number where the loop is defined.
        /// </summary>
        public int Line => _location.Line;

        /// <summary>
        /// The number of iterations executed so far.
        /// </summary>
        private int _iterationCount;

        /// <summary>
        /// The location of the loop in the source code.
        /// </summary>
        private readonly Location _location;

        /// <summary>
        /// Initializes a new instance of the Loop class.
        /// </summary>
        /// <param name="location">The location of the loop in the source code.</param>
        public Loop(Location location)
        {
            _location = location;
            _iterationCount = 0;
        }

        /// <summary>
        /// Increments the iteration count by one.
        /// </summary>
        /// <returns>The current Loop instance.</returns>
        public Loop Increment()
        {
            _iterationCount++;

            return this;
        }

        /// <summary>
        /// Asserts that the iteration count does not exceed the specified maximum.
        /// </summary>
        /// <param name="maxIterations">The maximum allowed iterations.</param>
        /// <returns>The current Loop instance.</returns>
        public Loop Assert(int maxIterations)
        {
            if (_iterationCount > maxIterations)
            {
                throw new RuntimeError($"More than {maxIterations} iterations exceeded at line {Line}, possible infinite loop", _location);
            }

            return this;
        }
    }
}