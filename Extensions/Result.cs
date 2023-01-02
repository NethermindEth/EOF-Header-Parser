using System;
using System.Data.Common;

namespace TypeExtensions;

public record Result () {
    public static Result operator &(Result left, Result right) {
        if (left is Failure<string> failureL) {
            return failureL;
        }
        if (right is Failure<string> failureR) {
            return failureR;
        }
        return new Success<string>("OK");
    }

    public static Result operator |(Result left, Result right) {
        if (left is Success<string> successL) {
            return successL;
        }
        if (right is Success<string> successR) {
            return successR;
        }
        return new Failure<Result[]>(new[] {
            left,
            right
        });
    }
}

public record Success<TValue> (TValue Value) : Result {
    static public Success<TValue> From(TValue value) => new Success<TValue>(value);
};
public record Failure<TError> (TError Message) : Result {
    static public Failure<TError> From(TError error) => new Failure<TError>(error);
};