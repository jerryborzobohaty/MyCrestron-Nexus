using System;

namespace Nexus.Utils
{
    /// <summary>
    /// Provides helper methods for working with nested exceptions.
    /// </summary>
    public static class GetInnerErr
    {
        /// <summary>
        /// Returns the deepest (innermost) exception in an exception chain.
        /// </summary>
        /// <param name="ex">The root exception.</param>
        /// <returns>
        /// The innermost <see cref="Exception"/> in the <paramref name="ex"/> hierarchy.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="ex"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// This method walks the <see cref="Exception.InnerException"/> chain until no further
        /// inner exceptions exist, making it useful for extracting the original cause of an error.
        /// </remarks>
        public static Exception GetInnermostException(Exception ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            while (ex.InnerException != null)
                ex = ex.InnerException;

            return ex;
        }

    }
}
