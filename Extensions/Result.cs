using System;
using System.Data.Common;

namespace TypeExtensions;

public record Result;
public record Success<TValue> (TValue Value) : Result {
    static public Success<TValue> From(TValue value) => new Success<TValue>(value);
};
public record Failure<TError> (TError Message) : Result {
    static public Failure<TError> From(TError error) => new Failure<TError>(error);
};