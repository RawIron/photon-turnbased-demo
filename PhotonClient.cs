#define NO_NAMESERVER

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;


namespace TurnbasedConsole
{
    using Photon.SocketServer;    
    using ExitGames.Client.Photon;
    using ExitGames.Client.Photon.Lite;
    using ExitGames.Client.Photon.LoadBalancing;

    class PhotonTurnbasedClient : LoadBalancingClient
    {
        private IStringLogger logger = null;
        private PhotonWebRpcHandler webrpcHandler = null;
        private PhotonOperationResponseHandler opResponseHandler = null;

        PhotonClientCachedData cache = null;

        // config
        const string MASTER_SERVER = "localhost:5055";
        const string APP_ID = "frontier";
        const int MAX_ROOM_SIZE = 30;

        const string RPC_GET_GAMELIST = "GetGameList";
        const string RPC_GET_GAMES = "GetGames";        
        const string RPC_GET_PLAYER = "PlayerLoad";
        const string RPC_POST_PLAYER = "PlayerSave";


        public int ActorNr
        {
            get { return cache.ActorNr; }
        }
        public string RoomName
        {
            get { return cache.RoomName; }
        }

        public PhotonTurnbasedClient(string myClan, string myPlayerName,
            PhotonClientCachedData cache,
            PhotonWebRpcHandler webrpc, PhotonOperationResponseHandler opresponse, IStringLogger logger)
        {
            this.cache = cache;
            this.cache.RoomName = myClan;
            this.PlayerName = myPlayerName;

            this.logger = logger;
            this.webrpcHandler = webrpc;
            this.opResponseHandler = opresponse;
            
            this.Setup();
            this.ConnectToMaster();
        }

        public PhotonTurnbasedClient(string myPlayerName,
            PhotonClientCachedData cache,
            PhotonWebRpcHandler webrpc, PhotonOperationResponseHandler opresponse, IStringLogger logger)
        {
            this.cache = cache;
            this.PlayerName = myPlayerName;
            this.logger = logger;
            this.webrpcHandler = webrpc;
            this.opResponseHandler = opresponse;

            this.Setup();
            this.ConnectToMaster();
        }

        private void Setup()
        {
            this.OnStateChangeAction += this.OnStateChanged;

#if NO_NAMESERVER
            this.NameServerAddress = string.Empty;
            this.MasterServerAddress = MASTER_SERVER;
#else
            this.NameServerAddress = "ns.exitgamescloud.com:5058";
            //this.NameServerAddress = "dev-app.exitgamescloud.com:5058";
            //this.NameServerAddress = "localhost:5058";
#endif
            this.AppId = APP_ID;
            this.cache.ActorNr = 0;

            this.loadBalancingPeer.DebugOut = DebugLevel.ALL; // set your prefered debug log level
        }

     
        private void OnStateChanged(ClientState clientState)
        {
            logger.Append(String.Format("---StateChanged:"));
            logger.Append(String.Format("   |State: {0}", clientState));

            switch (clientState)
            {
                case ClientState.JoinedLobby:
                    logger.Append(String.Format("   |CurrentLobby: {0}", this.CurrentLobbyName));
                    break;
                case ClientState.Joined:
                    logger.Append(String.Format("   |CurrentRoom: {0}", this.CurrentRoom));
                    break;
                case ClientState.Left:
                    logger.Append(String.Format("   |CurrentRoom: {0}", this.CurrentRoom));
                    break;
                                
                case ClientState.ConnectedToNameServer:
                    if (string.IsNullOrEmpty(this.CloudRegion))
                    {
                        this.OpGetRegions();
                    }
                    break;
                case ClientState.ConnectedToGameserver:
                    logger.Append(String.Format("   |GameServer: {0}", this.GameServerAddress));
                    break;
                case ClientState.ConnectedToMaster:
                    logger.Append(String.Format("   |MasterServer: {0}", this.MasterServerAddress));
                    break;
            }

            logger.Flush();
        }

        public override void OnOperationResponse(OperationResponse operationResponse)
        {
            base.OnOperationResponse(operationResponse);  // important to call, to keep state up to date

            if (operationResponse.ReturnCode != ErrorCode.Ok)
            {
                this.DebugReturn(DebugLevel.ERROR, operationResponse.ToStringFull() + " " + this.State);
            }

            logger.Append(String.Format("---OperationResponse:"));
            logger.Append(String.Format("   |Code: {0}", OperationCodeLookup.NameOf[operationResponse.OperationCode]));
            logger.Append(String.Format("   |ReturnCode: {0}", operationResponse.ReturnCode));

            switch (operationResponse.OperationCode)
            {
                case OperationCode.Authenticate:
                    break;

                case OperationCode.JoinLobby:
                    break;

                case OperationCode.JoinRandomGame:
                    break;

                case OperationCode.JoinGame:
                case OperationCode.CreateGame:
                    if (this.State == ClientState.Joined)
                    {
                        this.cache.ActorNr = (int)operationResponse.Parameters[ParameterCode.ActorNr];
                    }
                    break;

                case OperationCode.GetProperties:
                    logger.Append(operationResponse.ToStringFull());
                    break;

                case OperationCode.SetProperties:
                    //logger.Append(operationResponse.ToStringFull());
                    opResponseHandler.OnSetPropertiesResponse();
                    break;

                case OperationCode.RaiseEvent:
                    break;


                case OperationCode.Rpc:
                    WebRpcResponse webResponse = new WebRpcResponse(operationResponse);
                    if (operationResponse.ReturnCode != 0)
                    {
                        DebugReturn(DebugLevel.ERROR, "WebRpc failed. Response: " + operationResponse.ToStringFull());
                        DebugReturn(DebugLevel.ERROR, "WebRpc '" + webResponse.Name + "' failed. Error: " + webResponse.ReturnCode + " Message: " + webResponse.DebugMessage);
                    }
                    else
                    {
                        webrpcHandler.OnWebRpcResponse(webResponse);
                    }
                    break;
            }

            logger.Flush();
        }


        public override void OnEvent(EventData photonEvent)
        {
            logger.Append(String.Format("---EventAction:"));

            if (EventCodeLookup.NameOf.ContainsKey(photonEvent.Code))
            {
                logger.Append(String.Format("   |Code: {0}", EventCodeLookup.NameOf[photonEvent.Code]));
            }
            else 
            { 
                logger.Append(String.Format("   |Code: {0}", "Unknown"));
            }
            //// with the following two lines you can "see" what photon is sending to you
            //foreach (KeyValuePair<byte, object> parameter in photonEvent.Parameters)
            //{
            //    logger.Append(String.Format("   |" + parameter.Key + ":" + parameter.Value));
            //}

            // most events have a sender / origin (but not all) - let's find the player sending this
            int actorNr = 0;
            if (photonEvent.Parameters.ContainsKey(ParameterCode.ActorNr))
            {
                actorNr = (int)photonEvent[ParameterCode.ActorNr];  // actorNr (a.k.a. playerNumber / ID) of sending player
            }
            logger.Append(String.Format("   |Sender: {0}", actorNr));

            
            base.OnEvent(photonEvent);  // important to call, to keep state up to date

            switch (photonEvent.Code)
            {
                case 0:
                    logger.Append(String.Format("e[{0}]: {1}", actorNr, (string)photonEvent[ParameterCode.CustomEventContent]));
                    break;
                
                case LiteEventCode.Disconnect:
                    logger.Append(String.Format("a[{0}]: {1}", actorNr, "disconnected"));
                    break;

                case LiteEventCode.Join:
                    logger.Append(String.Format("a[{0}]: {1}", actorNr, "joined"));
                    break;
                case LiteEventCode.Leave:
                    logger.Append(String.Format("a[{0}]: {1}", actorNr, "left"));
                    break;

                case LiteEventCode.PropertiesChanged:
                    if (photonEvent.Parameters.ContainsKey(ParameterCode.GameProperties))
                    {
                        logger.Append(String.Format("a[{0}]", (string) photonEvent[ParameterCode.GameProperties].ToString()));
                    }
                    break;

                case EventCode.GameList:
                case EventCode.GameListUpdate:
                    foreach (var roominfo in this.RoomInfoList.Values)
                    {
                        logger.Append(String.Format("Room: {0} isOpen={1} IsVisible={2}", roominfo.Name, roominfo.IsOpen, roominfo.IsVisible));
                    }                  
                    break;

                case EventCode.AppStats:
                    logger.Append(String.Format("PlayersInRoom#={0} Rooms#={1} PlayersOnMaster#={2}", this.PlayersInRoomsCount, this.RoomsCount, this.PlayersOnMasterCount));
                    break;
            }

            logger.Flush();
        }
        
        public override void OnStatusChanged(StatusCode statusCode)
        {
            base.OnStatusChanged(statusCode);  // important to call, to keep state up to date

            logger.Append(String.Format("StatusCode: {0}", statusCode));
                       
            if (statusCode == StatusCode.Disconnect && this.DisconnectedCause != DisconnectCause.None)
            {
                DebugReturn(DebugLevel.ERROR, this.DisconnectedCause + " caused a disconnect. State: " + this.State + " statusCode: " + statusCode + ".");
            }

            logger.Flush();
        }

        public override void DebugReturn(DebugLevel level, string message)
        {
            base.DebugReturn(level, message);
            logger.Append(String.Format("{0}", message));
            logger.Flush();
        }


        public void ConnectToMaster()
        {
            if (!string.IsNullOrEmpty(this.NameServerAddress))
            {
                logger.Append(String.Format("connecting to:{0} as:{1}", this.NameServerAddress, this.PlayerName));

                if (!this.ConnectToRegionMaster("EU"))
                {
                    this.DebugReturn(DebugLevel.ERROR, "Can't connect to NameServer: " + this.NameServerAddress);
                }
            }
            else
            {
                logger.Append(String.Format("connecting to:{0} as:{1}", this.MasterServerAddress, this.PlayerName));

                if (!this.Connect(this.MasterServerAddress, this.AppId))
                {
                    this.DebugReturn(DebugLevel.ERROR, "Can't connect to MasterServer: " + this.MasterServerAddress);
                }
            }

            logger.Flush();
        }

        public void JoinRoom()
        {
            this.OpJoinRoom(this.cache.RoomName, this.cache.ActorNr == 0 ? false : true, this.cache.ActorNr);
        }

        public void LeaveRoom()
        {
            this.loadBalancingPeer.OpCustom(OperationCode.Leave, null, true);
        }

        public bool ChangeRoomName(string toName)
        {
            string previousName = this.cache.RoomName;
            this.cache.RoomName = toName;
            if (!this.cache.RoomName.Equals(previousName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CreateRoomForMatchmaking(string roomname)
        {
            string[] roomPropsInLobby = { "town_hall_level", "dollars_spent" };
            Hashtable customRoomProperties = new Hashtable() { { "town_hall_level", "1"} };

            this.OpCreateRoom(roomname, true, true, MAX_ROOM_SIZE, customRoomProperties, roomPropsInLobby, string.Empty, LobbyType.Default, Int32.MaxValue, 0);
        }

        public bool ChangeActor(int actorNr)
        {
            if (actorNr > 0)
            {
                this.cache.ActorNr = actorNr;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetRoomAndActorBy(int index)
        {
            if (index >= 0 && index < cache.Rooms.LongCount())
            {
                this.cache.RoomName = cache.Rooms[index];
                this.cache.ActorNr = cache.Actors[index];
                return true;
            }
            else
            {
                return false;
            }
        }


        public void GetGameList()
        {
            this.OpWebRpc(RPC_GET_GAMES, null);
        }

        public void GetMyGameList()
        {
            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new Dictionary<string, object>() { { "p1", "one" }, { "p2", "two" } });
            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", new int[] { 1, 2, 3 });
            //this.OpWebRpc("GetGameList?getpar1=1&getpar2=xyz", null);            
            this.OpWebRpc(RPC_GET_GAMELIST, null);
        }

        public void GetGameState()
        {

        }

        public void UpdateGameState()
        {
            Hashtable properties = new Hashtable() { {"another", "new values"} };
            CurrentRoom.SetCustomProperties(properties);
        }

        public void GetMyPlayer()
        {
            this.OpWebRpc(RPC_GET_PLAYER, null);
        }

        public void SaveMyPlayer(Player player)
        {
            string playerJson = JsonConvert.SerializeObject(player);
            this.OpWebRpc(RPC_POST_PLAYER, new Dictionary<string, object>() { { "LocalPlayer", playerJson }, { "GameId", "two" } });
        }


        public void OpRequest(string data)
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

        public void QuitRoom()
        {
            this.loadBalancingPeer.Disconnect();
        }
    }
}