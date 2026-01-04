using System;
using System.Diagnostics.CodeAnalysis;

namespace Olympus.Models;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// Use this pattern instead of exceptions for expected failure cases.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Only valid when IsSuccess is true.
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Cannot access Value on failed result: {_error}");

    /// <summary>
    /// Gets the error message. Only valid when IsFailure is true.
    /// </summary>
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error on successful result");

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Attempts to get the value, returning true if successful.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Gets the value if successful, or the default value if failed.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Maps the success value to a new type using the provided function.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(_value!)) : Result<TNew>.Failure(_error!);

    /// <summary>
    /// Chains another operation that returns a Result.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder) =>
        IsSuccess ? binder(_value!) : Result<TNew>.Failure(_error!);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(System.Action<T> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result<T> OnFailure(System.Action<string> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    /// <summary>
    /// Implicit conversion from value to successful Result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}

/// <summary>
/// Represents the result of an operation that can succeed or fail without returning a value.
/// </summary>
public readonly struct Result
{
    private readonly string? _error;

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message. Only valid when IsFailure is true.
    /// </summary>
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error on successful result");

    private Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(System.Action action)
    {
        if (IsSuccess)
            action();
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result OnFailure(System.Action<string> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({_error})";
}
