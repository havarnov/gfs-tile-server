using System.Net;

public class ErrorResponse
{
    public required HttpStatusCode StatusCode { get; init; }
    public required string Message { get; init; }
}