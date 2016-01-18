using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using System.Collections;

namespace TurnbasedConsole
{
    class PhotonOperationResponseHandler
    {
        PhotonClientCachedData cache = null;

        // delegates, events
        const string OP_SET_PROPERTIES = "SetCustomProperties";
        public delegate void OnSetPropertiesDelegate();
        public event OnSetPropertiesDelegate OnSetProperties;

        public PhotonOperationResponseHandler(PhotonClientCachedData cache)
        {
            this.cache = cache;
        }

        public void OnSetPropertiesResponse()
        {
            OnSetProperties();
        }
    }
}