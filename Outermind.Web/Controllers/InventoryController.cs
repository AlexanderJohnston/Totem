using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quantum.Queries.Clients;
using Totem;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  public class InventoryController : Controller
  {
    // Step 1: List pallets for a client
    // GET /api/inventory/pallets/NARA202416724
    [HttpGet("/api/inventory/pallets/{client}")]
    public Task<IActionResult> GetPallets(
      string client,
      [FromServices] IQueryServer queries) =>
      queries.Get<ClientPalletList>(Id.From(client));

    // Step 2: List boxes in a pallet
    // GET /api/inventory/boxes/NARA202416724:Pallet 10
    [HttpGet("/api/inventory/boxes/{id}")]
    public Task<IActionResult> GetBoxes(
      string id,
      [FromServices] IQueryServer queries) =>
      queries.Get<PalletBoxList>(Id.From(id));

    // Step 3: List rolls in a box
    // GET /api/inventory/rolls/NARA202416724:Pallet 10:Box 01
    [HttpGet("/api/inventory/rolls/{id}")]
    public async Task<IActionResult> GetBoxRolls(
      string id,
      [FromServices] IQueryDb queryDb,
      [FromServices] IOptions<JsonOptions> jsonOptions) =>
      await BoxRollListQueryResponder.Get(this, Id.From(id), queryDb, jsonOptions.Value);
  }
}
