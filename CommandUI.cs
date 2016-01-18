using System;
using System.Collections.Concurrent;


namespace TurnbasedConsole
{
    using ExitGames.Client.Photon.LoadBalancing;
    using System.Threading;

    class CommandUI : Actor
    {
        LoadBalancingClient client = null;

        private string command = string.Empty;
        private string data = string.Empty;
        private string keybuffer = string.Empty;


        public CommandUI(ConcurrentQueue<Message> inbox, ConcurrentQueue<Message> outbox, LoadBalancingClient client)
            : base(inbox, outbox)
        {
            this.client = client;
        }

        public override void DoInLoop()
        {
            Message outMessage = null;
            Message inMessage = null;

            ReadKeys();

            if ((outMessage = TriggerEvent(this.client.State)) != null)
            {
                this.outbox.Enqueue(outMessage);
            }

            while (inbox.TryDequeue(out inMessage))
            {
                HandleMessage(inMessage);
            }

            this.command = string.Empty;
            this.data = string.Empty;
        }

        public override void HandleMessage(Message message)
        {
            switch (message.id)
            {
                case MessageId.ConsoleMessage:
                    Console.WriteLine(message.data);
                    break;
                default:
                    break;
            }
            base.HandleMessage(message);
        }

        private void ReadKeys()
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey();
                if (Char.IsLetterOrDigit(key.KeyChar) || key.KeyChar == '-' || key.KeyChar == '\\' || key.Key == ConsoleKey.Enter)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (keybuffer.StartsWith(@"\"))
                        {
                            this.command = keybuffer.Substring(0, 2);
                            this.data = keybuffer.Substring(2, keybuffer.Length - 2);
                        }
                        else
                        {
                            this.command = keybuffer;
                        }
                        this.keybuffer = string.Empty;
                        Console.WriteLine();
                        break;
                    }
                    keybuffer += key.KeyChar;
                }
            }
        }

        private Message TriggerEvent(ClientState State)
        {
            Message trigger = null;

            switch (command)
            {
                case "":
                    break;

                case @"\a":
                    string actorNr = data;
                    trigger = new Message(MessageId.ChangeActor, actorNr);
                    break;
                case @"\u":
                    string userId = data;
                    trigger = new Message(MessageId.GetUser, userId);
                    break;
                case @"\t":
                    trigger = new Message(MessageId.CurrentState, string.Empty);
                    break;
                case @"\i":
                    trigger = new Message(MessageId.ShowRoomActor, string.Empty);
                    break;


                case @"\r":
                    string roomname = string.Empty;
                    if (!string.IsNullOrEmpty(data))
                    {
                        roomname = data;
                        trigger = new Message(MessageId.ChangeRoomName, roomname);
                    }
                    else
                    {
                        Console.WriteLine("missing room name");
                    }
                    break;
                case @"\s":
                    string index = data;
                    trigger = new Message(MessageId.SetRoomAndActor, index);
                    break;

                case @"\o":
                    if (State == ClientState.JoinedLobby)
                    {
                        trigger = new Message(MessageId.ListRooms, string.Empty);
                    }
                    break;

                case @"\c":
                    if (State == ClientState.JoinedLobby)
                    {
                        trigger = new Message(MessageId.CreateRoom, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("CreateRoom only allowed when in Lobby");
                    }
                    break;
                case @"\j":
                    if (State == ClientState.JoinedLobby)
                    {
                        trigger = new Message(MessageId.JoinRoom, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("JoinRoom only allowed when in Lobby");
                    }
                    break;

                case @"\l":
                    if (State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.LeaveRoom, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("LeaveRoom only allowed when in a Room");
                    }
                    break;

                case @"\n":
                    trigger = new Message(MessageId.Connect, string.Empty);
                    break;
                case @"\d":
                    if (State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.Disconnect, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("Disconnect only allowed when in a Room. Use Quit.");
                    }                 
                    break;
                case @"\q":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.Quit, string.Empty);
                        this.done = true;
                    }
                    break;


                case @"\m":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.GetMyGameList, string.Empty);
                    }
                    break;

                case @"\g":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.GetGameList, string.Empty);
                    }
                    break;

                case @"\p":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.PlayerSave, string.Empty);
                    }
                    break;

                case @"\v":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.PlayerLoad, string.Empty);
                    }
                    break;

                case @"\x":
                    if (State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.UpdateGameState, string.Empty);
                    }
                    else
                    {
                        Console.WriteLine("UpdateGameState only allowed when in a Room");
                    }
                    break;

                case @"\y":
                    if (State == ClientState.JoinedLobby || State == ClientState.Joined)
                    {
                        trigger = new Message(MessageId.GetGameState, string.Empty);
                    }
                    break;


                case @"\e":
                    if (State == ClientState.Joined && !string.IsNullOrEmpty(data))
                    {
                        trigger = new Message(MessageId.OperationRequest, data);
                    }
                    break;


                case @"\h":
                    PrintHelp();
                    break;

                default:
                    break;
            }

            return trigger;
        }

        private void PrintHelp()
        {
string help = @"
    client> \u - show my user id
    client> \r<name> - set room to <name>: (<name>, actor)
    client> \a<id> - set actor to <id>: (room, <id>)
    client> \t - show client state
    client> \i - show current (room, actor)
    client> \n - connect master server and join lobby
    client> \s<index> - set (room, actor) from <index>
    
    lobby> \o - show open rooms
    lobby> \c - create room
    lobby> \j - connect game server and join room
                (room, actor)
    lobby> \d - disconnect master server
    lobby> \q - disconnect master server

    room> \l - leave room and join lobby
               turnbased: leave game!!
    room> \d - disconnect game server and join lobby
               turnbased: stop/pause playing
    room> \q - disconnect game and master server
               turnbased: stop/pause playing    
    room> \e<event> - broadcast event

    server> \g - show game list
    server> \m - show my game list
    server> \p - save player's game (property)
    server> \v - load player's game (property)
    server> \x - save game state (town)
    server> \y - load game state (town)

    game>    
";
Console.WriteLine(help);
        }
    }
}