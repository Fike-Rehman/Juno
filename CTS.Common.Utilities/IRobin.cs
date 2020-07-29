using System.Threading.Tasks;

namespace CTS.Common.Utilities
{
    public interface IRobin
    {
        Task SpeakAsync(string text);
    }
}