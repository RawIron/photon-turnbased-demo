using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TurnbasedConsole
{
    using System.Threading;

    class Program
    {
        private string PlayerName = string.Empty;
        private string Clan = string.Empty;

        private ConcurrentQueue<Message> mainInbox = new ConcurrentQueue<Message>();

        static void Main(string[] args)
        {
            new Program().Run();
        }

        public Program()
        {
            this.PlayerName = MyPlayerName();
            this.Clan = MyClan();
        }

        private string MyPlayerName()
        {
            var cnt = System.Diagnostics.Process.GetProcessesByName(
                        System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count();
            return "MyPlayer" + cnt;
        }

        private string MyClan()
        {
            return "Homeless";
        }

        private void Run()
        {
            ConcurrentQueue<Message> brokerInbox = new ConcurrentQueue<Message>();
            ConcurrentQueue<Message> photonInbox = new ConcurrentQueue<Message>();
            ConcurrentQueue<Message> gameInbox = new ConcurrentQueue<Message>();
            ConcurrentQueue<Message> uiInbox = new ConcurrentQueue<Message>();


            Broker broker = new Broker(brokerInbox, photonInbox, gameInbox, uiInbox, outbox: mainInbox);


            PhotonClientCachedData cache = new PhotonClientCachedData();

            PhotonWebRpcHandler webrpc = new PhotonWebRpcHandler(cache);
            PhotonOperationResponseHandler opresponse = new PhotonOperationResponseHandler(cache);

            PhotonTurnbasedClient photonClient = new PhotonTurnbasedClient(Clan, PlayerName, 
                cache, webrpc, opresponse, 
                new QueuedConsoleLogger(brokerInbox));
            PhotonActor photonActor = new PhotonActor(photonInbox, brokerInbox, photonClient);

            //create delegate instances and bind them
            //to the observer's OnGameList, OnGames, .. method
            PhotonWebRpcHandler.OnGameListDelegate gameListDelegate = new
               PhotonWebRpcHandler.OnGameListDelegate(photonActor.OnGameList);
            webrpc.OnGameList += gameListDelegate;

            PhotonWebRpcHandler.OnGamesDelegate gamesDelegate = new
               PhotonWebRpcHandler.OnGamesDelegate(photonActor.OnGames);
            webrpc.OnGames += gamesDelegate;

            PhotonWebRpcHandler.OnPlayerSaveDelegate playerSaveDelegate = new
               PhotonWebRpcHandler.OnPlayerSaveDelegate(photonActor.OnPlayerSave);
            webrpc.OnPlayerSave += playerSaveDelegate;

            PhotonWebRpcHandler.OnPlayerLoadDelegate playerLoadDelegate = new
               PhotonWebRpcHandler.OnPlayerLoadDelegate(photonActor.OnPlayerLoad);
            webrpc.OnPlayerLoad += playerLoadDelegate;


            PhotonOperationResponseHandler.OnSetPropertiesDelegate setPropertiesDelegate = new
               PhotonOperationResponseHandler.OnSetPropertiesDelegate(photonActor.OnSetProperties);
            webrpc.OnPlayerLoad += playerLoadDelegate;            


            CommandUI commandUI = new CommandUI(uiInbox, brokerInbox, photonClient);
            Game game = new Game(gameInbox, brokerInbox);


            Thread brokerThread = new Thread(new ThreadStart(broker.UpdateLoop));
            brokerThread.IsBackground = true;
            brokerThread.Start();

            Thread gameThread = new Thread(new ThreadStart(game.UpdateLoop));
            gameThread.IsBackground = true;
            gameThread.Start();

            Thread uiThread = new Thread(new ThreadStart(commandUI.UpdateLoop));
            uiThread.IsBackground = true;
            uiThread.Start();

            Thread photonThread = new Thread(new ThreadStart(photonActor.UpdateLoop));
            photonThread.IsBackground = true;
            photonThread.Start();

            Actor joinActor = new Actor(mainInbox, null);
            joinActor.UpdateLoop();
        }
    }
}