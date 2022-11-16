using System;

namespace TypeExtensions;

public class Result<T, V>  {
    private T? Value {get; set;}
    private V? Error {get; set;}
    
    private bool isSuccessful = false;
    
    public static Result<T, V> Success(T result) {
        return new Result<T, V> {
            Value = result,
            isSuccessful = true
        };
    }
    
    public static Result<T, V> Failure(V error) {
        return new Result<T, V> {
            Error = error,
            isSuccessful = false
        };
    }
    
    public void Handle(Action<T?> success, Action<V?> failure)
    {
        if(isSuccessful) {
            success?.Invoke(Value);
        } else {
            failure?.Invoke(Error);
        }
    }

    public void Deconstruct(out T? value, out V? error) {
        value = Value;
        error = Error;
    }
}