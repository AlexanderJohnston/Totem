namespace Totem.Map.Builder;

internal abstract class RuntimeMethodParameterBuilder<TParameter> : RuntimeBuilder<TParameter>
    where TParameter : RuntimeMethodParameter
{
    protected RuntimeMethodParameterBuilder(ParameterInfo info) =>
        Info = info;

    internal ParameterInfo Info { get; }

    public override string ToString() =>
        $"{Info.ParameterType} {Info.Name}";

    public RuntimeMapError? CollectError() =>
        HasValue ? null : new RuntimeMapError(Info, Error, ErrorDetails);
}
