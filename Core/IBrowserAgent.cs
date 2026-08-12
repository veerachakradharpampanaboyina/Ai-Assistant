using System.Threading.Tasks;

namespace AIAssistant.Core
{
    public interface IBrowserAgent
    {
        Task<string> ExecuteScriptAsync(string script);
        event System.EventHandler<string> MessageReceived;
    }
}
