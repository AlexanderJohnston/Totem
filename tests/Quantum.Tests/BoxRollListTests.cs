using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Outermind;
using Quantum.Queries.Clients;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class BoxRollListQueryTests : QueryTests<BoxRollList>
  {
    [Fact]
    public async System.Threading.Tasks.Task NewRollDiscovered_StoresCompactStateForFreshReplays()
    {
      var route = Id.From("Client A:Pallet 1:Box 9");
      var prefix = @"\\server\share\Client A\Pallet 1\Box 9\";

      await Append(new NewRollDiscovered("Client A", "Pallet 1", "Box 9", "0001", $"{prefix}0001"));
      await Append(new NewRollDiscovered("Client A", "Pallet 1", "Box 9", "0002", $"{prefix}0002"));

      var query = await GetQuery(route);

      Assert.Equal("Client A", query.Client);
      Assert.Equal("Pallet 1", query.Pallet);
      Assert.Equal("Box 9", query.Box);
      Assert.Empty(query.Rolls);
      Assert.Equal(prefix, query.RollsPrefix);
      Assert.Equal(new[] { "0001", "0002" }, query.RollsCompact.Keys.ToArray());

      var response = BoxRollListResponse.From(query);
      var rollNames = response.Rolls.Select(roll => roll.Roll).OrderBy(name => name).ToArray();

      Assert.Equal(new[] { "0001", "0002" }, rollNames);
      Assert.All(response.Rolls, roll => Assert.StartsWith(prefix, roll.FullPath, StringComparison.Ordinal));
    }
  }

  public class BoxRollListMigrationTests
  {
    static readonly MethodInfo GivenNewRollDiscovered = typeof(BoxRollList).GetMethod(
      "Given",
      BindingFlags.Instance | BindingFlags.NonPublic,
      null,
      new[] { typeof(NewRollDiscovered) },
      null);

    [Fact]
    public void NewRollDiscovered_DrainsLegacyRollsInConfiguredBatches()
    {
      var prefix = @"\\server\share\Client A\Pallet 1\Box 9\";
      var query = new BoxRollList
      {
        Client = "Client A",
        Pallet = "Pallet 1",
        Box = "Box 9",
        Rolls = new HashSet<RollEntry>()
      };

      for(var i = 1; i <= 150; i++)
      {
        query.Rolls.Add(new RollEntry
        {
          Roll = i.ToString("D4"),
          FullPath = $"{prefix}{i:D4}",
          FirstSeenUtc = new DateTimeOffset(2026, 3, 9, 18, 56, 25, TimeSpan.FromHours(-4)).AddMinutes(i)
        });
      }

      Apply(query, new NewRollDiscovered("Client A", "Pallet 1", "Box 9", "0151", $"{prefix}0151"));

      Assert.Equal(prefix, query.RollsPrefix);
      Assert.Equal(50, query.Rolls.Count);
      Assert.Equal(101, query.RollsCompact.Count);
      Assert.Contains("0151", query.RollsCompact.Keys);
      Assert.All(query.RollsCompact.Keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
    }

    [Fact]
    public void ResponseProjection_PreservesLegacyWireShapeForMixedState()
    {
      var prefix = @"\\server\share\Client A\Pallet 1\Box 9\";
      var legacyTime = new DateTimeOffset(2026, 3, 9, 18, 56, 25, TimeSpan.FromHours(-4));
      var compactTime = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
      var externalPath = @"\\other\share\Client A\Pallet 1\Box 9\0003";

      var query = new BoxRollList
      {
        Client = "Client A",
        Pallet = "Pallet 1",
        Box = "Box 9",
        RollsPrefix = prefix,
        Rolls = new HashSet<RollEntry>
        {
          new() { Roll = "0001", FullPath = $"{prefix}0001", FirstSeenUtc = legacyTime }
        },
        RollsCompact = new Dictionary<string, long>
        {
          ["0001"] = compactTime.UtcDateTime.Ticks,
          ["0002"] = compactTime.UtcDateTime.Ticks,
          [externalPath] = compactTime.UtcDateTime.Ticks
        }
      };

      var response = BoxRollListResponse.From(query);
      var serialized = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      Assert.Equal("Client A", response.Client);
      Assert.Equal("Pallet 1", response.Pallet);
      Assert.Equal("Box 9", response.Box);
      Assert.Equal(3, response.Rolls.Count);
      Assert.Contains(response.Rolls, roll => roll.Roll == "0001" && roll.FullPath == $"{prefix}0001" && roll.FirstSeenUtc == legacyTime);
      Assert.Contains(response.Rolls, roll => roll.Roll == "0002" && roll.FullPath == $"{prefix}0002" && roll.FirstSeenUtc == compactTime);
      Assert.Contains(response.Rolls, roll => roll.Roll == "0003" && roll.FullPath == externalPath && roll.FirstSeenUtc == compactTime);
      Assert.Contains("\"client\":\"Client A\"", serialized, StringComparison.Ordinal);
      Assert.Contains("\"rolls\":[", serialized, StringComparison.Ordinal);
      Assert.DoesNotContain("rollsPrefix", serialized, StringComparison.Ordinal);
      Assert.DoesNotContain("rollsCompact", serialized, StringComparison.Ordinal);
    }

    static void Apply(BoxRollList query, NewRollDiscovered @event)
    {
      Assert.NotNull(GivenNewRollDiscovered);
      GivenNewRollDiscovered!.Invoke(query, new object[] { @event });
    }
  }
}
