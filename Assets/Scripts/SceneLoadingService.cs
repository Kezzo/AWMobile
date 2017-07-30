using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Service class to provide helper methods to load scenes.
/// </summary>
public class SceneLoadingService : MonoBehaviour
{
    /// <summary>
    /// Loads a scene asynchronous and calls a callback, when the scene loading finished.
    /// </summary>
    /// <param name="sceneName">Name of the scene.</param>
    /// <param name="onSceneLoadingProgress">The on scene loading progress callback.</param>
    /// <param name="onSceneLoaded">The on scene loaded callback.</param>
    public void LoadSceneAsync(string sceneName, Action<float> onSceneLoadingProgress, Action onSceneLoaded)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        StartCoroutine(AsyncSceneLoadingCoroutine(asyncOperation, onSceneLoadingProgress, onSceneLoaded));
    }

    /// <summary>
    /// Unloads a scene asynchronous and calls a callback, when the scene unloading finished.
    /// </summary>
    /// <param name="sceneName">Name of the scene.</param>
    /// <param name="onSceneLoadingProgress">The on scene unloading progress callback.</param>
    /// <param name="onSceneLoaded">The on scene loaded callback.</param>
    public void UnloadSceneAsync(string sceneName, Action<float> onSceneLoadingProgress, Action onSceneLoaded)
    {
        AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneName);

        StartCoroutine(AsyncSceneLoadingCoroutine(asyncOperation, onSceneLoadingProgress, onSceneLoaded));
    }

    /// <summary>
    /// Asynchronous the scene loading coroutine.
    /// </summary>
    /// <param name="asyncOperation">The asynchronous operation.</param>
    /// <param name="onSceneLoadingProgress">The on scene loading progress.</param>
    /// <param name="onSceneLoaded">The on scene loaded.</param>
    /// <returns></returns>
    private IEnumerator AsyncSceneLoadingCoroutine(AsyncOperation asyncOperation, Action<float> onSceneLoadingProgress,
        Action onSceneLoaded)
    {
        while (true)
        {
            if (asyncOperation.isDone)
            {
                // Wait one frame to ensure the awake method of the scene was called and MonoBehaviours were registered.
                yield return null;

                if (onSceneLoaded != null)
                {
                    onSceneLoaded();
                    break;
                }
            }

            if (onSceneLoadingProgress != null)
            {
                onSceneLoadingProgress(asyncOperation.progress);
            }

            yield return null;
        }
    }
}
