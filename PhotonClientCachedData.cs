using System.Collections.Generic;
using System.Collections;


namespace TurnbasedConsole
{
    class PhotonClientCachedData
    {
        private List<string> rooms = new List<string>();
        private string roomname = string.Empty;
        private List<int> actors = new List<int>();
        private int actorNr = 0;
        private List<string> games = new List<string>();

        public PhotonClientCachedData()
        {
        }

        public List<string> Rooms { get; set; }
        public string RoomName { get; set; }
        public List<int> Actors { get; set; }
        public int ActorNr { get; set; }
        public List<string> Games { get; set; }
    }
}