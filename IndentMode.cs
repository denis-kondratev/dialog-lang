namespace BitPatch.DialogLang
{
    /// <summary>
    /// Indentation modes for handling different indentation states.
    /// </summary>
    internal enum IndentMode
    {
        /// <summary>
        /// The default indentation mode.
        /// </summary>
        Default,

        /// <summary>
        /// Indentation needs to be fixed.
        /// </summary>
        NeenToFix,

        /// <summary>
        /// Indentation is fixed.
        /// </summary>
        Fixed
    }
}