using System;
using System.Collections.Generic;
using System.Text;

namespace LeilaoTempoReal.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }

    private Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Fail(string message) => new(false, message);
}