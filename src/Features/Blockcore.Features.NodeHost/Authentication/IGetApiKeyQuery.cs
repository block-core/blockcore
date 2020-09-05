using System.Threading.Tasks;

namespace Blockcore.Features.NodeHost.Authentication
{
    public interface IGetApiKeyQuery
    {
        Task<ApiKey> Execute(string providedApiKey);
    }
}
