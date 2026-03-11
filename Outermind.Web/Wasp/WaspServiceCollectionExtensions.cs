using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quantum.Web.Wasp.Controllers;

namespace Quantum.Web.Wasp;

public static class WaspServiceCollectionExtensions
{
    private const string DefaultBaseUrl = "https://thecrowleycompany.waspassetcloud.com";
    private const string TokenFileName = "token.txt";

    /// <summary>
    /// Registers all WASP API client services.
    /// Defaults to the Crowley Company WASP instance and reads the bearer token from token.txt in the app directory.
    /// Override via "Wasp:BaseUrl" and "Wasp:Token" in IConfiguration.
    /// </summary>
    public static IServiceCollection AddWaspApi(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Wasp:BaseUrl"] ?? DefaultBaseUrl;
        var token = configuration["Wasp:Token"] ?? ReadTokenFile();

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                $"WASP bearer token not found. Provide \"Wasp:Token\" in configuration or place a {TokenFileName} file in the application directory.");

        Action<HttpClient> configureClient = client =>
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        };

        services.AddHttpClient<AddressController>(configureClient);
        services.AddHttpClient<AssetController>(configureClient);
        services.AddHttpClient<AssetTypeController>(configureClient);
        services.AddHttpClient<AttachmentController>(configureClient);
        services.AddHttpClient<ContractController>(configureClient);
        services.AddHttpClient<CustomerController>(configureClient);
        services.AddHttpClient<DepartmentController>(configureClient);
        services.AddHttpClient<EmployeeController>(configureClient);
        services.AddHttpClient<FundingController>(configureClient);
        services.AddHttpClient<LocationController>(configureClient);
        services.AddHttpClient<ManufacturerController>(configureClient);
        services.AddHttpClient<PhoneController>(configureClient);
        services.AddHttpClient<PurchaseOrderController>(configureClient);
        services.AddHttpClient<SiteController>(configureClient);
        services.AddHttpClient<SystemInfoController>(configureClient);
        services.AddHttpClient<TransactionController>(configureClient);
        services.AddHttpClient<VendorController>(configureClient);

        return services;
    }

    private static string ReadTokenFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, TokenFileName);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }
}
