namespace Intellidevstore.Libs.Types;

public abstract record Result<T, TErr>
{
    public bool IsSuccess => this is Ok<T, TErr>;
    public bool IsFailure => this is Err<T, TErr>;

    public T? ValueOrDefault => this is Ok<T, TErr> ok ? ok.Value : default;
    public TErr? ErrorOrDefault => this is Err<T, TErr> err ? err.Error : default;

    public T ValueOrThrow() =>
        this is Ok<T, TErr> ok
            ? ok.Value
            : throw new InvalidOperationException(
                $"Result is in error state: {((Err<T, TErr>)this).Error}"
            );

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TErr, TResult> onFailure) =>
        this is Ok<T, TErr> ok ? onSuccess(ok.Value) : onFailure(((Err<T, TErr>)this).Error);

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<TErr, Task<TResult>> onFailure
    ) =>
        this is Ok<T, TErr> ok
            ? await onSuccess(ok.Value)
            : await onFailure(((Err<T, TErr>)this).Error);

    public Result<TNew, TErr> Map<TNew>(Func<T, TNew> mapper) =>
        this is Ok<T, TErr> ok
            ? new Ok<TNew, TErr>(mapper(ok.Value))
            : new Err<TNew, TErr>(((Err<T, TErr>)this).Error);

    public Result<TNew, TErr> Bind<TNew>(Func<T, Result<TNew, TErr>> binder) =>
        this is Ok<T, TErr> ok ? binder(ok.Value) : new Err<TNew, TErr>(((Err<T, TErr>)this).Error);

    public Result<T, TErr> OnSuccess(Action<T> action)
    {
        if (this is Ok<T, TErr> ok)
            action(ok.Value);
        return this;
    }

    public Result<T, TErr> OnFailure(Action<TErr> action)
    {
        if (this is Err<T, TErr> err)
            action(err.Error);
        return this;
    }

    // Implicit conversions for cleaner syntax
    public static implicit operator Result<T, TErr>(T value) => new Ok<T, TErr>(value);
}

public sealed record Ok<T, TErr>(T Value) : Result<T, TErr>
{
    public void Deconstruct(out T value) => value = Value;
}

public sealed record Err<T, TErr>(TErr Error) : Result<T, TErr>
{
    public void Deconstruct(out TErr error) => error = Error;
}

// Convenience factory methods for string errors (most common case)
public static class Result
{
    public static Result<T, string> Success<T>(T value) => new Ok<T, string>(value);

    public static Result<T, string> Failure<T>(string error) => new Err<T, string>(error);
}

// Unit type for void-returning operations
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
