namespace Totem.Map.Builder;

internal interface IErrorCollector
{
    IEnumerable<RuntimeMapError> CollectErrors();
}
