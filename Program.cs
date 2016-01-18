#define NO_NAMESERVER

using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace TurnbasedConsole
{
    using ExitGames.Client.Photon.Lite;
    using ExitGames.Client.Photon.LoadBalancing;
    using System.Threading;


    class Program : LoadBalancingClient
    {
        private List<string> roomList = new List<string>();
        private List<int> actorList = new List<int>();

        static void Main(string[] args)
        {
            new Program().Run();
        }

        public Program()
        {
            this.OnStateChangeAction += this.OnStateChanged;

#if NO_NAMESERVER
            this.NameServerAddress = string.Empty;
            this.MasterServerAddress = "localhost:5055"; // change ip:port if required
#else
            this.NameServerAddress = "ns.exitgamescloud.com:5058";
            //this.NameServerAddress = "dev-app.exitgamescloud.com:5058";
            //this.NameServerAddress = "localhost:5058";
#endif
            this.AppId = "frontier"; // insert your own AppID

            this.PlayerName = MyPlayerName();

            this.loadBalancingPeer.DebugOut = DebugLevel.ALL; // set your prefered debug log level
        }

        private string MyPlayerName()
        {
            var cnt = System.Diagnostics.Process.GetProcessesByName(
                        System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count();
            return "MyPlayer" + cnt;
        }

        private string MyClan()
        {
            return "MyClan1";
        }


        private void ConnectToMaster()
        {
            if (!string.IsNullOrEmpty(this.NameServerAddress))
            {
                Console.WriteLine("connecting to:{0} as:{1}", this.NameServerAddress, this.PlayerName);

                if (!this.ConnectToRegionMaster("EU")) 
                {
                    this.DebugReturn(DebugLevel.ERROR, "Can't connect to NameServer: " + this.NameServerAddress);
                }
            }
            else
            {
                Console.WriteLine("connecting to:{0} as:{1}", this.MasterServerAddress, this.PlayerName);

                if (!this.Connect(this.MasterServerAddress, this.AppId))
                {
                    this.DebugReturn(DebugLevel.ERROR, "Can't connect to MasterServer: " + this.MasterServerAddress);
                }
            }
        }
         
        bool done = false;
        void Run()
        {
            const int FOR_500ms = 500;

            this.ConnectToMaster();
            Thread thread = new Thread(this.UpdateLoop); // windows forms are event based. we need a game loop despite this
            thread.IsBackground = true; // a background thread automatically ends when the app ends
            thread.Start();

            while (!done)
            {
                Thread.Sleep(FOR_500ms);                
            }
        }


        private void CreateRoomForMatchmaking(string roomname)
        {
            const int MAX_CLAN_SIZE = 30;

            string[] roomPropsInLobby = { "town_hall_level", "dollars_spent" };
            System.Collections.Hashtable customRoomProperties = new System.Collections.Hashtable() { { "town_hall_level", 1 } };

            this.OpCreateRoom(roomname, true, true, MAX_CLAN_SIZE, customRoomProperties, roomPropsInLobby, string.Empty, LobbyType.Default, Int32.MaxValue, 0);
        }

        class UIEvent
        {
            public string id = string.Empty;
            public string data = string.Empty;

            public UIEvent(string anId, string someData)
            {
                this.id = anId;
                this.data = someData;
            }
        }

        class CommandUI
        {
            string command = string.Empty;
            string data = string.Empty;
            string keybuffer = string.Empty;
            Queue _actions = null;

            public CommandUI(Queue actions)
            {
                this._actions = actions;
            }

            private void ReadKeys()
            {
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (Char.IsLetterOrDigit(key.KeyChar) || key.KeyChar == '\\' || key.Key == ConsoleKey.Enter)
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

            private void TriggerAction()
            {
                switch (command)
                {
                    case "":
                        break;
                    case @"\a":
                        string actorNr = data;
                        UIEvent trigger = new UIEvent("ChangeActor", actorNr);
                        this._actions.Enqueue(trigger);
                        break;
                    case @"\r":
                        if (!string.IsNullOrEmpty(data))
                        {
                            roomname = data;
                        }
                        Console.WriteLine("set to room:{0} actor#:{1}", roomname, this.actorNr);
                        break;
                    case @"\c":
                        if (this.State == ClientState.JoinedLobby)
                        {
                            this.CreateRoomForMatchmaking(roomname);
                        }
                        else
                        {
                            Console.WriteLine("CreateRoom only allowed when in Lobby");
                        }
                        break;
                    case @"\j":
                        if (this.State == ClientState.JoinedLobby)
                        {
                            if (!string.IsNullOrEmpty(data))
                            {
                                int.TryParse(data, out this.actorNr);
                            }
                            Console.WriteLine("joining room:{0} with actor#:{1}", roomname, this.actorNr);
                            this.OpJoinRoom(roomname, this.actorNr == 0 ? false : true, this.actorNr);
                        }
                        else
                        {
                            Console.WriteLine("JoinRoom only allowed when in Lobby");
                        }
                        break;
                    case @"\d":
                        this.loadBalancingPeer.Disconnect();
                        break;
                    case @"\l":
                        if (this.State == ClientState.Joined)
                        {
                            this.loadBalancingPeer.OpCustom(OperationCode.Leave, null, true);
                        }
                        break;
                    case @"\g":
                        if (this.State == ClientState.JoinedLobby || this.State == ClientState.Joined)
                        {
                            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new Dictionary<string, object>() { { "p1", "one" }, { "p2", "two" } });
                            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new int[] { 1, 2, 3 });
                            this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", null);
                        }
                        break;
                    case @"\q":
                        this.loadBalancingPeer.Disconnect();
                        Thread.Sleep(1000);
                        done = true;
                        break;
                    case @"\e":
                        if (this.State == ClientState.Joined && !string.IsNullOrEmpty(data))
                        {
                            var op = new OperationRequest()
                            {
                                OperationCode = OperationCode.RaiseEvent,
                                Parameters =
                                    new Dictionary<byte, object>()
                                {
                                    { ParameterCode.Code, (byte)0 },
                                    {
                                        ParameterCode.Data, data
                                    },
                                    { ParameterCode.ActorList, this.CurrentRoom.Players.Keys.ToArray() },
                                    { ParameterCode.Cache, EventCaching.DoNotCache },
                                    { (byte)ParameterCode.HttpForward, true },
                                }
                            };
                            this.loadBalancingPeer.OpCustom(op, true, 0, false);
                        }
                        break;
                    case @"\s":
                        int index = -1;
                        int.TryParse(data, out index);
                        if (index >= 0 && index < roomList.LongCount())
                        {
                            roomname = roomList[index];
                            this.actorNr = actorList[index];
                        }
                        Console.WriteLine("set to room:{0} actor#:{1}", roomname, this.actorNr);
                        break;
                    default:
                        break;
                }
            }

            public void ReadInput()
            {
                ReadKeys();
                TriggerAction();

                this.command = string.Empty;
                this.data = string.Empty;
            }
        }


        int actorNr = 0;
        private void UpdateLoop(object obj)
        {
            string roomname = MyClan();
            var command = string.Empty;
            var data = string.Empty;
            var keybuffer = string.Empty;
            const int FOR_50ms = 50;
            var command = new CommandUI();


            while (!done)
            {
                while (this.loadBalancingPeer.DispatchIncomingCommands())
                {
                    // You could count dispatch calls to limit them to X, if they take too much time of a single frame
                }

                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (Char.IsLetterOrDigit(key.KeyChar) || key.KeyChar == '\\' || key.Key == ConsoleKey.Enter)
                    {
                        if (key.Key == ConsoleKey.Enter)
                        {
                            if (keybuffer.StartsWith(@"\"))
                            {
                                command = keybuffer.Substring(0, 2);
                                data = keybuffer.Substring(2, keybuffer.Length - 2);
                            }
                            else
                            {
                                command = keybuffer;
                            }
                            keybuffer = string.Empty;
                            Console.WriteLine();
                            break;
                        }
                        keybuffer += key.KeyChar;
                    }
                }

                switch (command)
                {
                    case "":
                        break;
                    case @"\a":
                        int.TryParse(data, out this.actorNr);
                        Console.WriteLine("set to room:{0} actor#:{1}", roomname, this.actorNr);
                        break;
                    case @"\r":
                        if (!string.IsNullOrEmpty(data))
                        {
                            roomname = data;
                        }
                        Console.WriteLine("set to room:{0} actor#:{1}", roomname, this.actorNr);
                        break;
                    case @"\c":
                        if (this.State == ClientState.JoinedLobby)
                        {
                            this.CreateRoomForMatchmaking(roomname);
                        }
                        else
                        {
                            Console.WriteLine("CreateRoom only allowed when in Lobby");
                        }
                        break;
                    case @"\j":
                        if (this.State == ClientState.JoinedLobby)
                        {
                            if (!string.IsNullOrEmpty(data))
                            {
                                int.TryParse(data, out this.actorNr);
                            }
                            Console.WriteLine("joining room:{0} with actor#:{1}", roomname, this.actorNr);
                            this.OpJoinRoom(roomname, this.actorNr == 0 ? false : true, this.actorNr);
                        }
                        else
                        {
                            Console.WriteLine("JoinRoom only allowed when in Lobby");
                        }
                        break;
                    case @"\d":
                        this.loadBalancingPeer.Disconnect();
                        break;
                    case @"\l":
                        if (this.State == ClientState.Joined)
                        {
                            this.loadBalancingPeer.OpCustom(OperationCode.Leave, null, true);
                        }
                        break;
                    case @"\g":
                        if (this.State == ClientState.JoinedLobby || this.State == ClientState.Joined)
                        {
                            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new Dictionary<string, object>() { { "p1", "one" }, { "p2", "two" } });
                            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new int[] { 1, 2, 3 });
                            this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", null);
                        }
                        break;
                    case @"\q":
                        this.loadBalancingPeer.Disconnect();
                        Thread.Sleep(1000);
                        done = true;
                        break;
                    case @"\e":
                        if (this.State == ClientState.Joined && !string.IsNullOrEmpty(data))
                        {
                            var op = new OperationRequest()
                            {
                                OperationCode = OperationCode.RaiseEvent,
                                Parameters =
                                    new Dictionary<byte, object>()
                                {
                                    { ParameterCode.Code, (byte)0 },
                                    {
                                        ParameterCode.Data, data
                                    },
                                    { ParameterCode.ActorList, this.CurrentRoom.Players.Keys.ToArray() },
                                    { ParameterCode.Cache, EventCaching.DoNotCache },
                                    { (byte)ParameterCode.HttpForward, true },
                                }
                            };
                            this.loadBalancingPeer.OpCustom(op, true, 0, false);                   
                        }
                        break;
                    case @"\s":
                        int index = -1;
                        int.TryParse(data, out index);
                        if (index >= 0 && index < roomList.LongCount())
                        {
                            roomname = roomList[index];
                            this.actorNr = actorList[index];
                        }
                        Console.WriteLine("set to room:{0} actor#:{1}", roomname, this.actorNr);
                        break;
                    default:
                        break;
                }

                command = string.Empty;
                data = string.Empty;

                this.loadBalancingPeer.SendOutgoingCommands();

                Thread.Sleep(FOR_50ms);
            }
        }
        
        private void OnStateChanged(ClientState clientState)
        {
            var message = string.Format("State: {0}", clientState);
            Console.WriteLine(message);

            switch (clientState)
            {
                case ClientState.ConnectedToNameServer:
                    if (string.IsNullOrEmpty(this.CloudRegion))
                    {
                        this.OpGetRegions();
                    }
                    break;
                case ClientState.ConnectedToGameserver:
                    break;
                case ClientState.ConnectedToMaster:
                    break;
            }
        }

        public override void OnOperationResponse(OperationResponse operationResponse)
        {
            base.OnOperationResponse(operationResponse);  // important to call, to keep state up to date

            if (operationResponse.ReturnCode != ErrorCode.Ok)
            {
                this.DebugReturn(DebugLevel.ERROR, operationResponse.ToStringFull() + " " + this.State);
            }

            switch (operationResponse.OperationCode)
            {
                case OperationCode.Authenticate:
                    break;

                case OperationCode.JoinRandomGame:
                    break;

                case OperationCode.JoinGame:
                case OperationCode.CreateGame:
                    if (this.State == ClientState.Joined)
                    {
                        this.actorNr = (int)operationResponse.Parameters[ParameterCode.ActorNr];
                    }
                    break;

                case OperationCode.Rpc:

                    if (operationResponse.ReturnCode != 0)
                    {
                        DebugReturn(DebugLevel.ERROR, "WebRpc failed. Response: " + operationResponse.ToStringFull());
                    }
                    else
                    {
                        WebRpcResponse webResponse = new WebRpcResponse(operationResponse);
                        this.OnWebRpcResponse(webResponse);
                    }
                    break;
            }
        }

        public void OnWebRpcResponse(WebRpcResponse webResponse)
        {
            if (webResponse.ReturnCode != 0)
            {
                DebugReturn(DebugLevel.ERROR, "WebRpc '" + webResponse.Name + "' failed. Error: " + webResponse.ReturnCode + " Message: " + webResponse.DebugMessage);
                return;
            }

            switch (webResponse.Name)
            {
                case "GetGameList":
                    roomList = new List<string>();
                    actorList = new List<int>();

                    var data = webResponse.Parameters;
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            roomList.Add(item.Key);
                            actorList.Add(int.Parse((string)item.Value));
                            Console.WriteLine("Got room:{0} actor#:{1}", item.Key, item.Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine(@"N\A - empty list");
                    }
                    break;
            }
        }

        public override void OnEvent(EventData photonEvent)
        {
            //Console.WriteLine("\n---EventAction: " + photonEvent.Code);

            //// with the following two lines you can "see" what photon is sending to you
            //foreach (KeyValuePair<byte, object> parameter in photonEvent.Parameters)
            //{
            //    Console.WriteLine("   |" + parameter.Key + ":" + parameter.Value);
            //}

            // most events have a sender / origin (but not all) - let's find the player sending this
            int actorNr = 0;
            if (photonEvent.Parameters.ContainsKey(ParameterCode.ActorNr))
            {
                actorNr = (int)photonEvent[ParameterCode.ActorNr];  // actorNr (a.k.a. playerNumber / ID) of sending player
            }

            base.OnEvent(photonEvent);  // important to call, to keep state up to date

            switch (photonEvent.Code)
            {
                case 0:
                    Console.WriteLine("e[{0}]: {1}", actorNr, (string)photonEvent[ParameterCode.CustomEventContent]);
                    break;
                case LiteEventCode.Disconnect:
                    Console.WriteLine("a[{0}]: {1}", actorNr, "disconnected");
                    break;

                case LiteEventCode.Join:
                    Console.WriteLine("a[{0}]: {1}", actorNr, "joined");
                    break;
                case LiteEventCode.Leave:
                    Console.WriteLine("a[{0}]: {1}", actorNr, "left");
                    break;

                case EventCode.GameList:
                case EventCode.GameListUpdate:
                    foreach (var roominfo in this.RoomInfoList.Values)
                    {
                        Console.WriteLine("Room: {0} isOpen={1} IsVisible={2}", roominfo.Name, roominfo.IsOpen, roominfo.IsVisible);
                    }
                    
                    break;

                case EventCode.AppStats:
                    Console.WriteLine("PlayersInRoom#={0} Rooms#={1} PlayersOnMaster#={2}", this.PlayersInRoomsCount, this.RoomsCount, this.PlayersOnMasterCount);
                    break;
            }
        }
        
        public override void OnStatusChanged(StatusCode statusCode)
        {
            base.OnStatusChanged(statusCode);  // important to call, to keep state up to date

            Console.WriteLine("StatusCode: {0}", statusCode);

            if (statusCode == StatusCode.Disconnect && this.DisconnectedCause != DisconnectCause.None)
            {
                DebugReturn(DebugLevel.ERROR, this.DisconnectedCause + " caused a disconnect. State: " + this.State + " statusCode: " + statusCode + ".");
            }
        }

        public override void DebugReturn(DebugLevel level, string message)
        {
            base.DebugReturn(level, message);

            Console.WriteLine(message);
        }
    }
}
