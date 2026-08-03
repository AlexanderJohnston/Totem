using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Outermind.Controllers;
using Xunit;

namespace Quantum.Tests
{
  public class MicrofilmCanonicalRouteContractTests
  {
    [Fact]
    public void RowHttpContract_ContainsOnlyRollScopedRoutes()
    {
      var rowRoutes = typeof(MicrofilmController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
        .Select(attribute => attribute.Template)
        .Where(template => template != null && template.Contains("rows", StringComparison.Ordinal))
        .OrderBy(template => template, StringComparer.Ordinal)
        .ToArray();

      Assert.Equal(new[]
      {
        "rolls/{rollId}/custom-rows",
        "rolls/{rollId}/custom-rows/{rowId}/cells/{columnId}",
        "rolls/{rollId}/rows",
        "rolls/{rollId}/rows",
        "rolls/{rollId}/rows/{rowId}",
        "rolls/{rollId}/rows/{rowId}/cells/{columnId}",
        "rolls/{rollId}/rows/{rowId}/operation-context"
      }, rowRoutes);

      Assert.DoesNotContain(rowRoutes, route => route.Contains("{clientId}", StringComparison.Ordinal));
    }
  }
}
