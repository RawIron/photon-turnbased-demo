#define NO_NAMESERVER

using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
using System.Linq;
using System.Text;

namespace TurnbasedConsole
{
    using ExitGames.Client.Photon.Lite;
    using ExitGames.Client.Photon.LoadBalancing;
    using System.Threading;

    class PhotonActor : Actor
    {
        private PhotonTurnbasedClient photonClient;

        public PhotonActor(ConcurrentQueue<Message> inbox, ConcurrentQueue<Message> outbox, PhotonTurnbasedClient photonClient)
            : base(inbox, outbox)
        {
            this.photonClient = photonClient;
        }

        public override void DoInLoop()
        {
            Message message = null;

            while (photonClient.loadBalancingPeer.DispatchIncomingCommands())
            {
                // You could count dispatch calls to limit them to X, if they take too much time of a single frame
            }

            while (inbox.TryDequeue(out message))
            {
                this.HandleMessage(message);
            }

            photonClient.loadBalancingPeer.SendOutgoingCommands();
        }

        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);

            int actorNr = 0;
            string roomname = string.Empty;

            switch (message.id)
            {
                case MessageId.ListRooms:
                    PrintOpenRoomList();
                    break;

                case MessageId.ChangeRoomName:
                    roomname = message.data;
                    if (photonClient.ChangeRoomName(roomname))
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("changed to room {0}", photonClient.RoomName)));
                    }
                    else
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("unchanged room {0}", photonClient.RoomName)));
                    }
                    break;

                case MessageId.CreateRoom:
                    roomname = message.data;
                    photonClient.CreateRoomForMatchmaking(roomname);
                    break;

                case MessageId.JoinRoom:
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("joining room:{0} with actor#:{1}", photonClient.RoomName, photonClient.ActorNr)));
                    photonClient.JoinRoom();
                    break;

                case MessageId.LeaveRoom:
                    photonClient.LeaveRoom();
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("leaving room:{0}", photonClient.CurrentRoom)));
                    break;


                case MessageId.GetMyGameList:
                    photonClient.GetMyGameList();
                    break;

                case MessageId.GetGameList:
                    photonClient.GetGameList();
                    break;

                case MessageId.GetGameState:
                    photonClient.GetGameState();
                    break;

                case MessageId.UpdateGameState:
                    photonClient.UpdateGameState();
                    break;

                case MessageId.PlayerSave:
                    photonClient.SaveMyPlayer(photonClient.LocalPlayer);
                    break;

                case MessageId.PlayerLoad:
                    photonClient.GetMyPlayer();
                    break;

                case MessageId.OperationRequest:
                    photonClient.OpRequest(message.data);
                    break;


                case MessageId.ChangeActor:
                    int.TryParse(message.data, out actorNr);
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("actorNr: {0}", actorNr)));
                    if (photonClient.ChangeActor(actorNr))
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("set to actor#: {0}", photonClient.ActorNr)));
                    }
                    else
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("stay as actor#: {0}", photonClient.ActorNr)));
                    }
                    break;

                case MessageId.GetUser:
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("user: {0}", photonClient.PlayerName)));
                    break;

                case MessageId.ShowRoomActor:
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("room: {0}, actor#: {1}", photonClient.RoomName, photonClient.ActorNr)));
                    break;

                case MessageId.SetRoomAndActor:
                    int index = -1;
                    int.TryParse(message.data, out index);
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("index: {0}", index)));
                    if (photonClient.SetRoomAndActorBy(index))
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("set to room:{0} actor#:{1}", photonClient.RoomName, photonClient.ActorNr)));
                    }
                    else
                    {
                        outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("stay in room:{0} actor#:{1}", photonClient.RoomName, photonClient.ActorNr)));
                    }
                    break;

                case MessageId.CurrentState:
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("state: {0}", photonClient.State)));
                    break;


                case MessageId.Quit:
                    photonClient.QuitRoom();
                    break;
                case MessageId.Connect:
                    photonClient.ConnectToMaster();
                    break;
                case MessageId.Disconnect:
                    photonClient.Disconnect();
                    break;

                default:
                    outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("photon received unknown message: {0}", message.id.ToString())));
                    break;
            }
        }

        public void OnGameList(List<string> games)
        {
            if (games.Count > 0)
            {
                StringBuilder rooms = new StringBuilder();
                foreach (var room in games)
                {
                    rooms.Append(String.Format("Got room:{0}\n", room));
                }
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(rooms.ToString())));
            }
            else
            {
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(@"N\A - empty list")));
            }
        }

        public void OnPlayerLoad(string playerJson)
        {
            outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("PlayerState: {0}", playerJson)));
        }

        public void OnPlayerSave()
        {
            outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("PlayerState saved")));
        }

        public void OnGames(List<string> games)
        {
            if (games.Count > 0)
            {
                StringBuilder rooms = new StringBuilder();
                foreach (var room in games)
                {
                    rooms.Append(String.Format("Got room:{0}\n", room));
                }
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(rooms.ToString())));
            }
            else
            {
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(@"N\A - empty list")));
            }
        }

        public void OnSetProperties()
        {
            PrintRoomCustomProperties();
        }

        private void PrintOpenRoomList()
        {
            if (photonClient.RoomInfoList.Count == 0)
            {
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("no open rooms -> create a room")));
            }
            else
            {
                StringBuilder rooms = new StringBuilder();
                foreach (KeyValuePair<string, RoomInfo> room in photonClient.RoomInfoList)
                {
                    rooms.Append(String.Format("room:{0} info:{1}", room.Key, ""));
                }
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(rooms.ToString())));
            }
        }

        private void PrintRoomCustomProperties()
        {
            if (photonClient.CurrentRoom.CustomProperties.Count == 0)
            {
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format("no custom properties")));
            }
            else
            {
                StringBuilder properties = new StringBuilder();
                foreach (DictionaryEntry property in photonClient.CurrentRoom.CustomProperties)
                {
                    properties.Append(String.Format("key: {0} value: {1}\n", property.Key, property.Value));
                }
                outbox.Enqueue(new Message(MessageId.ConsoleMessage, String.Format(properties.ToString())));
            }
        }
    }
}