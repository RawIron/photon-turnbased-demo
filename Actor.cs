using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TurnbasedConsole
{
    using System.Threading;

    class Actor
    {
        protected ConcurrentQueue<Message> inbox = null;
        protected ConcurrentQueue<Message> outbox = null;
        protected bool done = false;

        public Actor(ConcurrentQueue<Message> inbox, ConcurrentQueue<Message> outbox)
        {
            this.inbox = inbox;
            this.outbox = outbox;
        }

        public virtual void UpdateLoop()
        {
            const int FOR_50ms = 50;
            const int FOR_500ms = 500;

            while (!done)
            {
                DoInLoop();
                Thread.Sleep(FOR_50ms);
            }

            Thread.Sleep(FOR_500ms);
        }
        public virtual void DoInLoop()
        {
            Message message = null;
            while (inbox.TryDequeue(out message))
            {
                this.HandleMessage(message);
            }
        }
        public virtual void HandleMessage(Message message)
        {
            switch (message.id)
            {
                case MessageId.Quit:
                    done = true;
                    break;
                default:
                    break;
            }
        }
    }
}