using System;
using System.Collections.Generic;


namespace TurnbasedConsole
{
    public enum MessageId
    {
        ChangeActor,
        GetUser,
        SetRoomAndActor,
        ShowRoomActor,
        
        ChangeRoomName,
        CreateRoom,
        JoinRandomRoom,
        JoinRoom,
        ListRooms,
        LeaveRoom,

        OperationRequest,
        RaiseEvent,

        GetGameList,
        GetMyGameList,
        GetGameState,
        UpdateGameState,
        
        PlayerSave,
        PlayerSaved,
        PlayerLoad,
        PlayerLoaded,
        PlayerBackup,
        PlayerRestore,

        PlayerSerialize,
        PlayerSerialized,
        PlayerDeserialize,
        PlayerDeserialized,

        RoomStateLoad,

        ConsoleMessage,

        Connect,
        Disconnect,
        Quit,

        CurrentState,
        Undefined,
    }

    class Message
    {
        public MessageId id = MessageId.Undefined;
        public string data = string.Empty;

        public Message() {}

        public Message(MessageId anId, string someData)
        {            
            this.id = anId;
            this.data = someData;
        }
    }
}