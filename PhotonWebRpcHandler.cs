using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using System.Collections;

namespace TurnbasedConsole
{
    class PhotonWebRpcHandler
    {
        PhotonClientCachedData cache = null;

        // delegates, events
        const string RPC_GET_GAMELIST = "GetGameList";
        public delegate void OnGameListDelegate(List<string> aGameList);
        public event OnGameListDelegate OnGameList;

        const string RPC_GET_GAMES = "GetGames";
        public delegate void OnGamesDelegate(List<string> aGames);
        public event OnGamesDelegate OnGames;

        const string RPC_GET_PLAYER = "PlayerLoad";
        public delegate void OnPlayerLoadDelegate(string aPlayer);
        public event OnPlayerLoadDelegate OnPlayerLoad;

        const string RPC_POST_PLAYER = "PlayerSave";
        public delegate void OnPlayerSaveDelegate();
        public event OnPlayerSaveDelegate OnPlayerSave;

        public PhotonWebRpcHandler(PhotonClientCachedData cache)
        {
            this.cache = cache;
        }

        public void OnWebRpcResponse(WebRpcResponse webResponse)
        {
            switch (webResponse.Name)
            {
                case RPC_GET_GAMELIST:
                    OnGetGameListResponse(webResponse.Parameters);
                    break;

                case RPC_GET_GAMES:
                    OnGetGamesResponse(webResponse.Parameters);
                    break;

                case RPC_GET_PLAYER:
                    OnPlayerLoadResponse(webResponse.Parameters);
                    break;

                case RPC_POST_PLAYER:
                    OnPlayerSaveResponse();
                    break;
            }
        }

        private void OnGetGameListResponse(Dictionary<string, object> data)
        {
            cache.Rooms = new List<string>();
            cache.Actors = new List<int>();

            if (data != null)
            {
                foreach (var item in data)
                {
                    cache.Rooms.Add(item.Key);
                    cache.Actors.Add(int.Parse((string)item.Value));
                }
            }
            OnGameList(cache.Rooms);
        }

        private void OnGetGamesResponse(Dictionary<string, object> game)
        {
            cache.Games = new List<string>();

            if (game != null)
            {
                foreach (var item in game)
                {
                    cache.Games.Add(item.Key);
                }
            }
            OnGames(cache.Games);
        }

        private void OnPlayerSaveResponse()
        {
            OnPlayerSave();
        }

        private void OnPlayerLoadResponse(Dictionary<string, object> playerState)
        {
            string playerJson = string.Empty;

            if (playerState != null)
            {
                foreach (var item in playerState)
                {
                    playerJson = (string)item.Value;
                }
            }
            OnPlayerLoad(playerJson);
        }
    }
}