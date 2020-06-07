using BlazorModal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorModal
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddBlazorModal(this IServiceCollection services)
		{
			return services.AddScoped<ModalService>();
		}
	}
}