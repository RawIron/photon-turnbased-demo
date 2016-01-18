using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TurnbasedConsole
{
    using System.Threading;

    class Game : Actor
    {
        public Game(ConcurrentQueue<Message> inbox, ConcurrentQueue<Message> outbox) : base(inbox, outbox) {}
    }
}