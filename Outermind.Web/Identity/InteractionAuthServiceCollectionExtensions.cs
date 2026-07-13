using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Quantum.Web.Identity
{
  public static class InteractionAuthServiceCollectionExtensions
  {
    public static IServiceCollection AddInteractionAuth(
      this IServiceCollection services,
      IConfiguration configuration,
      IWebHostEnvironment environment)
    {
      var configuredOptions = configuration.GetSection("InteractionAuth").Get<InteractionAuthOptions>() ?? new InteractionAuthOptions();

      services.Configure<InteractionAuthOptions>(configuration.GetSection("InteractionAuth"));
      services.Configure<CsrfOptions>(configuration.GetSection("InteractionAuth:Csrf"));
      services.AddSingleton<IApplicationUserStore, FileApplicationUserStore>();
      services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
      services.AddSingleton<IApplicationUserManager, ApplicationUserManager>();
      services.AddSingleton<ICsrfTokenService, CsrfTokenService>();
      services.AddScoped<CookieBackedCsrfFilter>();
      services.Configure<MvcOptions>(options => options.Filters.AddService<CookieBackedCsrfFilter>());

      services
        .AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = InteractionAuthDefaults.AuthenticationScheme;
          options.DefaultSignInScheme = InteractionAuthDefaults.AuthenticationScheme;
        })
        .AddCookie(InteractionAuthDefaults.AuthenticationScheme, options =>
        {
          options.Cookie.Name = string.IsNullOrWhiteSpace(configuredOptions.CookieName) ? "Totem.Auth" : configuredOptions.CookieName;
          options.Cookie.HttpOnly = true;
          options.Cookie.IsEssential = true;
          options.Cookie.Path = "/";
          options.Cookie.SameSite = ParseSameSite(configuredOptions.SameSite);
          options.Cookie.SecurePolicy = configuredOptions.RequireSecureCookies || !environment.IsDevelopment()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
          options.ExpireTimeSpan = TimeSpan.FromHours(configuredOptions.CookieLifetimeHours > 0 ? configuredOptions.CookieLifetimeHours : 8);
          options.SlidingExpiration = configuredOptions.SlidingExpiration;
          options.LoginPath = PathString.Empty;
          options.AccessDeniedPath = PathString.Empty;
          options.Events = new CookieAuthenticationEvents
          {
            OnRedirectToLogin = context =>
            {
              context.Response.StatusCode = StatusCodes.Status401Unauthorized;
              return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
              context.Response.StatusCode = StatusCodes.Status403Forbidden;
              return Task.CompletedTask;
            }
          };
        });

      return services;
    }

    static SameSiteMode ParseSameSite(string value) =>
      string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase)
        ? SameSiteMode.Strict
        : string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
          ? SameSiteMode.None
          : SameSiteMode.Lax;
  }
}
