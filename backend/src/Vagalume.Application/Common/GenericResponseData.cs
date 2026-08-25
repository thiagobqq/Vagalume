namespace Vagalume.Application.Common;

public class GenericResponseData<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static GenericResponseData<T> Ok(T data, string message = "Operação realizada com sucesso")
        => new() { Success = true, Message = message, Data = data };

    public static GenericResponseData<T> Fail(string message)
        => new() { Success = false, Message = message, Data = default };
}
