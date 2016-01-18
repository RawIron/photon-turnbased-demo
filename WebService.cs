using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TurnbasedConsole
{
    using System.Threading;

    class WebService : Actor
    {
        public WebService(ConcurrentQueue<Message> inbox, ConcurrentQueue<Message> outbox) : base(inbox, outbox) {}
    }
}