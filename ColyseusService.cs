namespace Code.Infrastructure.Services.Colyseus {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Code.Infrastructure.Services.Auth;
    using Code.Infrastructure.Services.Colyseus.Schema;
    using Cysharp.Threading.Tasks;
    using global::Colyseus;

    public class ColyseusService : IColyseusService {
        private const string GameRoomName = "room";

        private readonly ColyseusClient client;
        private readonly IAuthService authService;

        public ColyseusService(ColyseusSettings settings, IAuthService authService) {
            this.client = new ColyseusClient(settings);
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async UniTask<IGameRoomService> JoinGameRoom(CancellationToken cancellationToken = default) {
            try {
                using var timeoutCts = new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCts.Token);

                var token = await this.authService.GetTokenAsync();

                if (token.IsEmpty) {
                    throw new InvalidOperationException("Token is null or empty. Cannot join game room.");
                }

                this.client.Auth.Token = token.Value;

                timeoutCts.CancelAfter(30000); 

                var colyseusRoom = await this.client.JoinOrCreate<GameRoomState>(GameRoomName)
                                             .AsUniTask()
                                             .AttachExternalCancellation(linkedCts.Token);

                return new GameRoomService(colyseusRoom);
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested) {
                    throw new OperationCanceledException("Join game room operation was cancelled.");
                }

                throw new TimeoutException($"Join game room operation timed out after 30000ms.");
            }
            catch (Exception ex) {
                throw new Exception($"Failed to join game room: {ex.Message}", ex);
            }
        }
    }
}