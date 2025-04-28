using System.Net;

namespace GFSTileServer;

/// <summary>
/// The error response module used by this application.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The status code of the response that returns this <see cref="ErrorResponse"/>.
    /// </summary>
    public required HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// A descriptive message to guide/help the caller of this application.
    /// </summary>
    public required string Message { get; init; }
}