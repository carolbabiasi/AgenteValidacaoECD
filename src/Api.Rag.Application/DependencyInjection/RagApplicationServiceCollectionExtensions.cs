using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Application.DependencyInjection;

public static class RagApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddRagApplication(this IServiceCollection services)
    {
        return services;
    }
}
