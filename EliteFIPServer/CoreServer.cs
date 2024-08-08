using EliteAPI.Abstractions.Events;
using EliteFIPServer.Logging;
using System.Collections.Concurrent;


namespace EliteFIPServer
{
    public enum GameEventType {
        Empty,
        Status,
        Target,
        Location,
        Navigation,
        PreviousNavRoute,
        Jump,
        DockingGranted,
        DockingDenied,
        DockingTimeout,
        DockingCancelled,
        LoadGame,
        Loadout,
        RefuelAll,
        RefuelPartial,
        ReservoirReplenished,
        FuelScoop,
        ShipyardSwap,
        ShipyardNew,
        ShipyardBuy,
        ApproachBody
    }
    public struct GameEventTrigger {
        public IEvent GameEvent { get; set; }

        public GameEventTrigger(IEvent gameEvent) {
            GameEvent = gameEvent;
        }
    }

    public class CoreServer {

        // Reference to Primary UI 
        private ServerConsole ServerConsole;

        // Server States
        public ComponentState CurrentState { get; private set; }

        // Game Event Worker
        private CancellationTokenSource GameDataWorkerCTS;
        private Task GameDataTask;
        private RunState GameDataWorkerState { get; set; }
        BlockingCollection<GameEventTrigger> GameDataQueue = new BlockingCollection<GameEventTrigger>(Constants.MaxGameDataQueueSize);

        public EliteAPIIntegration EliteAPIIntegration { get; private set; }

        public EDCPClient EDCPClient { get; private set; }

        public CoreServer(ServerConsole serverConsole) {
            ServerConsole = serverConsole;
            CurrentState = new ComponentState();

            EliteAPIIntegration = new EliteAPIIntegration(this);
            EDCPClient = new EDCPClient();
        }

        public void Start() {
            Log.Instance.Info("Server Core starting");
            CurrentState.Set(RunState.Starting);            

            // Start Game Data Worker Thread
            Log.Instance.Info("Starting Game data worker");
            GameDataWorkerCTS = new CancellationTokenSource();
            GameDataTask = new Task(new Action(GameDataWorkerThread), GameDataWorkerCTS.Token);
            GameDataTask.ContinueWith(GameDataWorkerThreadEnded);
            GameDataTask.Start();

            EliteAPIIntegration.Start();

 
            CurrentState.Set(RunState.Started);
            //ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core started");            
        }

        public void Stop() {
            Log.Instance.Info("Server Core stopping");            
            CurrentState.Set(RunState.Stopping);
            EliteAPIIntegration.Stop();

            // Isssue the cancel to signal worker threads to end
            GameDataWorkerCTS.Cancel();

            // Stop Panel Server
            EDCPClient.Stop();

            GameDataQueue.CompleteAdding();
            CurrentState.Set(RunState.Stopped);                                   
            Log.Instance.Info("Server Core stopped");
        }


        private void GameDataWorkerThread() {

            GameDataWorkerState = RunState.Started;
            Log.Instance.Info("Game Data Worker Thread started");

            //DateTime lastSuccessfulUpdate = DateTime.Today;
            DateTime lastSuccessfulUpdate = DateTime.UtcNow;

            CancellationToken cToken = GameDataWorkerCTS.Token;


            while (cToken.IsCancellationRequested == false && !GameDataQueue.IsCompleted) {

                GameEventTrigger gameEventTrigger = new GameEventTrigger(null);
                try {
                    gameEventTrigger = GameDataQueue.Take(cToken);
                } catch (InvalidOperationException) { }

                if (gameEventTrigger.GameEvent != null) {
                    // Log.Instance.Info("Updating {statetype} data", gameEventTrigger.GameEvent.ToString());
                    EDCPClient.UpdateGameState(gameEventTrigger.GameEvent);
                }
                //Log.Instance.Info("Game Data Worker Thread waiting for new work");
            }
            Log.Instance.Info("Game Data Worker Thread ending");
        }

        private void GameDataWorkerThreadEnded(Task task) {
            GameDataWorkerState = RunState.Stopped;
            if (task.Exception != null) {
                Log.Instance.Info("GameData Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            Log.Instance.Info("GameData Worker Thread ended");
        }

        public void GameDataEvent(IEvent evt)
        {
            if (evt == null) return;

            GameEventTrigger newEvent = new GameEventTrigger(evt);
            CancellationToken cToken = GameDataWorkerCTS.Token;
            GameDataQueue.Add(newEvent, cToken);
        }
    }
}
