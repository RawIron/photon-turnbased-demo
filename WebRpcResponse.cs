using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;

namespace TurnbasedConsole
{
    class WebRpcResponse
    {
        public string Name { get; private set; }
        public int ReturnCode { get; private set; }
        public string DebugMessage { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }

        public WebRpcResponse(OperationResponse response)
        {
            object value;
            response.Parameters.TryGetValue(ParameterCode.UriPath, out value);
            this.Name = value as string;

            response.Parameters.TryGetValue(ParameterCode.RpcCallRetData, out value);
            this.Parameters = value as Dictionary<string, object>;
            
            response.Parameters.TryGetValue(ParameterCode.RpcCallRetCode, out value);
            this.ReturnCode = response.ReturnCode;

            this.DebugMessage = response.DebugMessage;
        }
    }
}
