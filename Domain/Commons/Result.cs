namespace Domain.Commons;

public class Result<T>
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public T? Data { get; }

    private Result(bool isSuccess, T? data, string? error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data) => new Result<T>(true, data, null);
    public static Result<T> Success() => new Result<T>(true, default, null); // Parameterless success
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
}


