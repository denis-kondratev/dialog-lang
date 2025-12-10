using System;

namespace GameDialog.Lang
{
    /// <summary>
    /// A request that is sent from the dialog to the outside to obtain some information.
    /// </summary>
    public abstract record RuntimeRequest<TOutput, TInput> : RuntimeItem where TOutput : RuntimeValue
    {
        /// <summary>
        /// Sets the result of the request execution.
        /// </summary>
        /// <param name="result">The result of the request execution.</param>
        public abstract void Request(TInput result);

        /// <summary>
        /// Creates a new request with the specified location.
        /// </summary>
        internal RuntimeRequest(Location location)
        {
            _location = location;
        }

        /// <summary>
        /// Returns the response value of the request.
        /// </summary>
        internal TOutput GetResult()
        {
            return _result ?? throw new RuntimeError("Request has not been fulfilled yet.", _location);
        }

        /// <summary>
        /// The result of the request execution.
        /// </summary>
        protected TOutput? _result;

        /// <summary>
        /// The location of the request in the source code.
        /// </summary>
        private readonly Location _location;

        /// <summary>
        /// The display name for request types.
        /// </summary>
        public new static string DisplayName => "request";
    }

    /// <summary>
    /// A request to obtain any RuntimeValue during execution.
    /// </summary>
    public sealed record RuntimeValueRequest : RuntimeRequest<RuntimeValue, object>
    {
        /// <summary>
        /// Copy constructor for cloning the request.
        /// </summary>
        internal RuntimeValueRequest(Location location) : base(location)
        {
        }

        /// <summary>
        /// Fulfills the request with the specified value.
        /// </summary>
        public override void Request(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Result cannot be null.");
            }

            // For demonstration purposes, we will just wrap the string in a RuntimeString.
            _result = Utils.ObjectToRuntimeValue(value);
        }

        /// <summary>
        /// Gets the display name of the item type.
        /// </summary>
        public override string GetTypeName() => DisplayName;

        /// <summary>
        /// The display name for value request types.
        /// </summary>
        public new static string DisplayName => "values request";
    }
}