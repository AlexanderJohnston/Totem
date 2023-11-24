using Totem.Map.Summary;

namespace Totem.Mvc.Controllers;

[Route("map")]
public sealed class MapController : ControllerBase
{
    readonly RuntimeMap _map;

    public MapController(RuntimeMap map) =>
        _map = map;

    [HttpGet]
    public RuntimeMapSummary GetMap() =>
        _map.Summary;
}
