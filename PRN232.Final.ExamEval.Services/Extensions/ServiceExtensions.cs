using Mapster;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureMapsters(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            services.AddMapster();
        }
    }
}
