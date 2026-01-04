using Olympus.Models;
using Xunit;

namespace Olympus.Tests.Models;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var result = Result<int>.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure("Error");

        Assert.Throws<System.InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Success(42);

        Assert.Throws<System.InvalidOperationException>(() => _ = result.Error);
    }

    [Fact]
    public void TryGetValue_OnSuccess_ReturnsTrueAndValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_OnFailure_ReturnsFalse()
    {
        var result = Result<int>.Failure("Error");

        Assert.False(result.TryGetValue(out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ReturnsValue()
    {
        var result = Result<int>.Success(42);

        Assert.Equal(42, result.GetValueOrDefault(0));
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ReturnsDefault()
    {
        var result = Result<int>.Failure("Error");

        Assert.Equal(99, result.GetValueOrDefault(99));
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var result = Result<int>.Success(42);

        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsSuccess);
        Assert.Equal("42", mapped.Value);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        var result = Result<int>.Failure("Error");

        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsFailure);
        Assert.Equal("Error", mapped.Error);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsOperation()
    {
        var result = Result<int>.Success(42);

        var bound = result.Bind(x => Result<string>.Success($"Value: {x}"));

        Assert.True(bound.IsSuccess);
        Assert.Equal("Value: 42", bound.Value);
    }

    [Fact]
    public void Bind_OnSuccess_CanReturnFailure()
    {
        var result = Result<int>.Success(-1);

        var bound = result.Bind(x => x >= 0
            ? Result<string>.Success($"Valid: {x}")
            : Result<string>.Failure("Negative value"));

        Assert.True(bound.IsFailure);
        Assert.Equal("Negative value", bound.Error);
    }

    [Fact]
    public void Bind_OnFailure_PropagatesError()
    {
        var result = Result<int>.Failure("Initial error");

        var bound = result.Bind(x => Result<string>.Success($"Value: {x}"));

        Assert.True(bound.IsFailure);
        Assert.Equal("Initial error", bound.Error);
    }

    [Fact]
    public void OnSuccess_OnSuccess_ExecutesAction()
    {
        var result = Result<int>.Success(42);
        var executed = false;

        result.OnSuccess(_ => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void OnSuccess_OnFailure_DoesNotExecuteAction()
    {
        var result = Result<int>.Failure("Error");
        var executed = false;

        result.OnSuccess(_ => executed = true);

        Assert.False(executed);
    }

    [Fact]
    public void OnFailure_OnFailure_ExecutesAction()
    {
        var result = Result<int>.Failure("Error");
        string? capturedError = null;

        result.OnFailure(e => capturedError = e);

        Assert.Equal("Error", capturedError);
    }

    [Fact]
    public void OnFailure_OnSuccess_DoesNotExecuteAction()
    {
        var result = Result<int>.Success(42);
        var executed = false;

        result.OnFailure(_ => executed = true);

        Assert.False(executed);
    }

    [Fact]
    public void Match_OnSuccess_CallsSuccessFunction()
    {
        var result = Result<int>.Success(42);

        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e}");

        Assert.Equal("Success: 42", matched);
    }

    [Fact]
    public void Match_OnFailure_CallsFailureFunction()
    {
        var result = Result<int>.Failure("Error");

        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e}");

        Assert.Equal("Failure: Error", matched);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToString_OnSuccess_ReturnsSuccessString()
    {
        var result = Result<int>.Success(42);

        Assert.Equal("Success(42)", result.ToString());
    }

    [Fact]
    public void ToString_OnFailure_ReturnsFailureString()
    {
        var result = Result<int>.Failure("Error");

        Assert.Equal("Failure(Error)", result.ToString());
    }
}

public class ResultVoidTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var result = Result.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success();

        Assert.Throws<System.InvalidOperationException>(() => _ = result.Error);
    }

    [Fact]
    public void OnSuccess_OnSuccess_ExecutesAction()
    {
        var result = Result.Success();
        var executed = false;

        result.OnSuccess(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void OnSuccess_OnFailure_DoesNotExecuteAction()
    {
        var result = Result.Failure("Error");
        var executed = false;

        result.OnSuccess(() => executed = true);

        Assert.False(executed);
    }

    [Fact]
    public void Match_OnSuccess_CallsSuccessFunction()
    {
        var result = Result.Success();

        var matched = result.Match(
            onSuccess: () => "Success",
            onFailure: e => $"Failure: {e}");

        Assert.Equal("Success", matched);
    }

    [Fact]
    public void Match_OnFailure_CallsFailureFunction()
    {
        var result = Result.Failure("Error");

        var matched = result.Match(
            onSuccess: () => "Success",
            onFailure: e => $"Failure: {e}");

        Assert.Equal("Failure: Error", matched);
    }

    [Fact]
    public void ToString_OnSuccess_ReturnsSuccessString()
    {
        var result = Result.Success();

        Assert.Equal("Success", result.ToString());
    }

    [Fact]
    public void ToString_OnFailure_ReturnsFailureString()
    {
        var result = Result.Failure("Error");

        Assert.Equal("Failure(Error)", result.ToString());
    }
}
