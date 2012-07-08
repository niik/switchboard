using System.Threading.Tasks;

namespace Switchboard.Server
{
    public interface ISwitchboardRequestHandler
    {
        Task<SwitchboardResponse> GetResponseAsync(SwitchboardContext context, SwitchboardRequest request);
    }
}
