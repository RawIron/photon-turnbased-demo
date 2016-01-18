using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TurnbasedConsole
{
    using System.Threading;

    class Broker : Actor
    {
        private ConcurrentQueue<Message> photonInbox = null;
        private ConcurrentQueue<Message> gameInbox = null;
        private ConcurrentQueue<Message> uiInbox = null;

        public Broker(ConcurrentQueue<Message> inbox, 
            ConcurrentQueue<Message> photonInbox,
            ConcurrentQueue<Message> gameInbox, 
            ConcurrentQueue<Message> uiInbox, 
            ConcurrentQueue<Message> outbox) : base(inbox, outbox)
        {
            this.photonInbox = photonInbox;
            this.gameInbox = gameInbox;
            this.uiInbox = uiInbox;

            DefineRoutes();
        }


        private Dictionary<MessageId, List<ConcurrentQueue<Message>>>
            routes = new Dictionary<MessageId, List<ConcurrentQueue<Message>>>();

        // TODO pattern matching would be awesome
        private void DefineRoutes()
        {
            var routeUIOnly = new List<ConcurrentQueue<Message>>();
            routeUIOnly.Add(this.uiInbox);

            var routePhotonOnly = new List<ConcurrentQueue<Message>>();
            routePhotonOnly.Add(this.photonInbox);

            var routeBroadcast = new List<ConcurrentQueue<Message>>();
            routeBroadcast.Add(this.uiInbox);
            routeBroadcast.Add(this.gameInbox);
            routeBroadcast.Add(this.photonInbox);
            routeBroadcast.Add(this.outbox);

            routes.Add(MessageId.ConsoleMessage, routeUIOnly);
            routes.Add(MessageId.Quit, routeBroadcast);
        }


        public override void DoInLoop()
        {
            Message message = null;
            
            while (inbox.TryDequeue(out message))
            {
                this.HandleMessage(message);
                route(message);
            }
        }

        public override void HandleMessage(Message message)
        {
            switch (message.id)
            {
                case MessageId.PlayerBackup:
                    Message playerSerialize = new Message(MessageId.PlayerSerialize, string.Empty);
                    inbox.Enqueue(playerSerialize);
                    break;
                case MessageId.PlayerSerialized:
                    Message playerSave = new Message(MessageId.PlayerSave, message.data);
                    inbox.Enqueue(playerSave);
                    break;

                case MessageId.PlayerRestore:
                    Message playerLoad = new Message(MessageId.PlayerLoad, string.Empty);
                    inbox.Enqueue(playerLoad);
                    break;
                case MessageId.PlayerLoaded:
                    Message playerDeserialized = new Message(MessageId.PlayerDeserialized, message.data);
                    inbox.Enqueue(playerDeserialized);
                    break;
                default:
                    break;
            }
            base.HandleMessage(message);
        }

        private void route(Message message)
        { 
            if (routes.ContainsKey(message.id))
            {
                foreach (var route in routes[message.id])
                {
                    route.Enqueue(message);
                }
            }
            else // default route
            {
                photonInbox.Enqueue(message);
            }
        }
    }
}