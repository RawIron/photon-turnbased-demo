namespace TurnbasedConsole
{
    using ExitGames.Client.Photon.LoadBalancing;
    using ExitGames.Client.Photon.Lite;
    using System.Collections.Generic;

    public static class OperationCodeLookup
    {
        public static Dictionary<byte, string> NameOf = new Dictionary<byte, string> 
        {
            { OperationCode.Authenticate, "Authenticate" },
            { OperationCode.ChangeGroups, "ChangeGroups" },
            { OperationCode.CreateGame, "CreateGame" },
            { OperationCode.FindFriends, "FindFriends" },
            { OperationCode.GetProperties, "GetProperties" },
            { OperationCode.GetRegions, "GetRegions" },
            { OperationCode.JoinGame, "JoinGame" },
            { OperationCode.JoinLobby, "JoinLobby" },
            { OperationCode.JoinRandomGame, "JoinRandomGame" },
            { OperationCode.Leave, "Leave" },
            { OperationCode.LeaveLobby, "LeaveLobby" },
            { OperationCode.RaiseEvent, "RaiseEvent" },
            { OperationCode.Rpc, "Rpc" },
            { OperationCode.SetProperties, "SetProperties" },
        };
    }

    public static class EventCodeLookup
    {
        public static Dictionary<byte, string> NameOf = new Dictionary<byte, string> 
        {
            { EventCode.AppStats, "AppStats" },
            { EventCode.AzureNodeInfo, "AzureNodeInfo" },
            { EventCode.Disconnect, "Disconnect" },
            { EventCode.GameList, "GameList" },
            { EventCode.GameListUpdate, "GameListUpdate" },
            { EventCode.Join, "Join" },
            { EventCode.Leave, "Leave" },
            { EventCode.Match, "Match" },
            { EventCode.PropertiesChanged, "PropertiesChanged" },
            { EventCode.QueueState, "QueueState" },
            //{ EventCode.SetProperties, "SetProperties" },
            //{ EventCode.TurnbasedLeave, "TurnbasedLeave" },
        };
    }

    public static class LiteEventCodeLookup
    {
        public static Dictionary<byte, string> NameOf = new Dictionary<byte, string> 
        {
            { LiteEventCode.Disconnect, "Disconnect" },
            { LiteEventCode.Join, "Join" },
            { LiteEventCode.Leave, "Leave" },
            { LiteEventCode.PropertiesChanged, "PropertiesChanged" },
        };
    }

}