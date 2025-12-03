using FluentResults;

namespace Pinventory.Pins.Application.Results;

public static class ResultDtoExtensions
{
    public static ResultDto ToResultDto(this Result result)
    {
        return result.IsSuccess
            ? ResultDto.Ok()
            : new ResultDto(TransformErrors(result.Errors), false);
    }

    public static ResultDto<TValue> ToResultDto<TValue>(this Result<TValue> result)
    {
        return result.IsSuccess
            ? new ResultDto<TValue>(result.ValueOrDefault, [])
            : new ResultDto<TValue>(result.ValueOrDefault, TransformErrors(result.Errors), false);
    }

    private static IEnumerable<ErrorDto> TransformErrors(IEnumerable<IError> errors)
    {
        return errors.Select(x => new ErrorDto(x.Message, x.Reasons.Select(r => r.Message), x.GetType().FullName));
    }
}