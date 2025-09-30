namespace Code.Gameplay.Game.Behaviour
{
    using System;
    using System.Threading;
    using Code.Gameplay.Game.Behaviour.Dto;
    using Cysharp.Threading.Tasks;
    using JetBrains.Annotations;
    using Unity.Cinemachine;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public class CameraBehaviour : ICameraBehaviour {
        private static readonly GameLogger Logger = GameLogger.Instantiate();

        private readonly Vector3 defaultCameraPosition = Vector3.zero;
        private readonly Vector3 defaultCameraFollowOffset = new Vector3(0, 45, -38);
        private readonly Quaternion defaultCameraRotation = Quaternion.Euler(50, 0, 0);

        private CinemachineCamera camera;
        private CinemachineFollow cinemachineFollow;

        private CancellationTokenSource? cameraCts;
        private CancellationToken token;

        public void InitializeCamera([NotNull] CinemachineCamera cam) {
            this.SetNewCameraToken();

            if (this.camera == null) {
                this.camera = cam;
                this.cinemachineFollow = this.camera.GetComponent<CinemachineFollow>();
            }
        }

        public void CameraDefault() {
            if (this.camera == null) {
                return;
            }

            this.SetNewCameraToken();
            this.CameraDefaultAsync().Forget();
        }

        public void CameraMove(PlayerData playerData) {
            if (this.camera == null) {
                return;
            }

            this.SetNewCameraToken();
            this.CameraMoveAsync(playerData).Forget();
        }

        private void SetNewCameraToken()
        {
            this.cameraCts?.Cancel();
            this.cameraCts?.Dispose();
            this.cameraCts = new CancellationTokenSource();
            this.token = this.cameraCts.Token;
        }

        private async UniTask CameraDefaultAsync() {
            Logger.Log("Camera default behaviour started");
            var duration = 3f;
            var elapsed = 0f;

            Vector3 startOffset = this.cinemachineFollow.FollowOffset;
            var targetOffset = this.defaultCameraFollowOffset;

            Quaternion startRot = this.camera.transform.rotation;
            Quaternion endRot = this.defaultCameraRotation;

            Vector3 startPos = this.camera.transform.localPosition;
            Vector3 endPos = this.defaultCameraPosition;

            try {
                while (!this.token.IsCancellationRequested && elapsed < duration) {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    this.cinemachineFollow.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);
                    this.camera.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
                    this.camera.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, this.token);
                }
            }
            catch(OperationCanceledException) {
                Logger.Log("Camera default behaviour canceled");
            }

            Logger.Log("Camera default behaviour finished");
        }

        private async UniTask CameraMoveAsync(PlayerData playerData) {
            Logger.Log("Camera move behaviour started");

            this.camera.transform.rotation = this.defaultCameraRotation;

            try {
                while (!this.token.IsCancellationRequested && playerData.MoveSpeedCurrent >= 0.1f) {
                    float percent = playerData.MoveSpeedCurrent * 100f / playerData.MoveSpeedMax;
                    float targetOffsetY = this.defaultCameraFollowOffset.y + (20f * (percent / 100f));

                    var smoothSpeed = 2f;

                    float currentOffsetY = this.cinemachineFollow.FollowOffset.y;
                    float y = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.deltaTime * smoothSpeed);
                    var smoothedOffset = new Vector3(this.defaultCameraFollowOffset.x, y, this.defaultCameraFollowOffset.z);

                    this.cinemachineFollow.FollowOffset = smoothedOffset;
                    this.camera.transform.localPosition += this.camera.transform.forward * percent / 3f;
                    await UniTask.Yield(PlayerLoopTiming.Update, this.token);
                }
            }
            catch (OperationCanceledException) {
                Logger.Log("Camera move behaviour canceled");
            }

            Logger.Log("Camera move behaviour finished");

            if (playerData.MoveSpeedCurrent == 0) {
                this.CameraDefault();
            }
        }
    }
}
