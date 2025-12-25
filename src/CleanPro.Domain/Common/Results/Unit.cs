namespace CleanPro.Domain.Common.Results;

public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    public static readonly Unit Value = new();

    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    public static Result<Unit> Success => Result<Unit>.Success(Value);

    public override int GetHashCode() => 0;

    public override bool Equals(object? obj) => obj is Unit;

    public bool Equals(Unit other) => true;

    public int CompareTo(Unit other) => 0;

    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right) => true;

    public static bool operator !=(Unit left, Unit right) => false;
}
