using System;
using System.Collections.Concurrent;
using System.Text;

namespace TurnbasedConsole
{
    class QueuedConsoleLogger : IStringLogger
    {
        private ConcurrentQueue<Message> outbox = null;
        string entry = string.Empty;
        StringBuilder sb = new StringBuilder();

        public QueuedConsoleLogger(ConcurrentQueue<Message> outbox)
        {
            this.outbox = outbox;
        }

        public void Append(string entry)
        {
            sb.Append(entry + "\n");
        }

        public void Flush()
        {
            Message logEntry = new Message(MessageId.ConsoleMessage, sb.ToString());
            outbox.Enqueue(logEntry);
            sb.Clear();
        }
    }
}
