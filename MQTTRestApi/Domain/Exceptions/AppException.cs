namespace MQTTRestApi.Domain.Exceptions;

public abstract class AppException : Exception
{
    public int ErrorCode { get; }
    public AppException(string message, int errorCode) : base(message) => ErrorCode = errorCode;
}

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);
public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);
public class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);
public class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);
public class UnauthorizedException(string message) : AppException(message, StatusCodes.Status401Unauthorized);