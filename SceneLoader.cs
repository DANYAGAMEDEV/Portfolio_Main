namespace Code.Infrastructure.Common {
    using System;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class SceneLoader {
        private static readonly GameLogger Logger = GameLogger.Instantiate();

        public static async UniTask<AsyncOperation> LoadLevelAsynchronously(
            string sceneName,
            Action<AsyncOperation> onReadyListener) {
            try {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null) {
                    throw new InvalidOperationException($"Failed to start async loading for scene {sceneName}");
                }

                operation.allowSceneActivation = false;

                await UniTask.WaitUntil(() => operation.progress >= 0.9f);

                onReadyListener?.Invoke(operation);

                await UniTask.WaitUntil(() => operation.isDone);

                return operation;
            }
            catch (Exception exception) {
                Logger.Error(exception);
                throw;
            }
        }
    }
}