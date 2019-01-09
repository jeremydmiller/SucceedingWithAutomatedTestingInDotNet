using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Controllers;

namespace AlbaSamples
{
    public class RecordingQueue : IQueue
    {
        public readonly IList<object> MessagesReceived = new List<object>();
        
        public Task Send<T>(T message)
        {
            MessagesReceived.Add(message);
            return Task.CompletedTask;
        }
    }
}