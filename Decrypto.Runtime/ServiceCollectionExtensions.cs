using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decrypto.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Decrypto.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDecrypto(this IServiceCollection services)
        {
            if(services == null)
                throw new ArgumentNullException(nameof(services));

            return services
                .AddSingleton<IBlockRepository, BlockRepository>();
                //.AddHttpClient();
            //.AddSingleton<IVersionService, VersionService>()
        }
    }
}
