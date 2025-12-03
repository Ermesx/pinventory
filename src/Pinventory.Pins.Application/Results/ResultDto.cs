using FluentResults;

namespace Pinventory.Pins.Application.Results;

public record ResultDto(IEnumerable<ErrorDto> Errors, bool IsSuccess = true)
{
    public bool IsFailed => !IsSuccess;
    public static ResultDto Ok() => new([]);

    public bool HasError<TError>() where TError : IError => Errors.Any(x => x.Type == typeof(TError).FullName);
}

public record ResultDto<TValue>(TValue? Value, IEnumerable<ErrorDto> Errors, bool IsSuccess = true) : ResultDto(Errors, IsSuccess);