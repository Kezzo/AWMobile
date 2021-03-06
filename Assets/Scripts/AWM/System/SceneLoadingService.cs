﻿using System;
using System.Collections;
using AWM.Controls;
using AWM.MapTileGeneration;
using AWM.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWM.System
{
    /// <summary>
    /// Service class to provide helper methods to load scenes.
    /// </summary>
    public class SceneLoadingService : MonoBehaviour
    {
        public bool IsInLevelSelection { get; private set; }

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

        /// <summary>
        /// If invoked will load to the level selection.
        /// </summary>
        /// <param name="onLoadedToLevelSelection">Invoked when the level selection was loaded.</param>
        public void LoadToLevelSelection(Action onLoadedToLevelSelection)
        {
            LoadToLevel("LevelSelection", onLoadedToLevelSelection, true);
        }

        /// <summary>
        /// If invoked will load to the level with the given name.
        /// </summary>
        /// <param name="levelName">Name of the level.</param>
        /// <param name="onLoadedToLevel">Invoked when the level is loaded.</param>
        /// <param name="isInLevelSelection">
        /// Determines if the current level loaded is the level selection. 
        /// Required to properly hide specific UIs and handle input differently.
        /// </param>
        public void LoadToLevel(string levelName, Action onLoadedToLevel, bool isInLevelSelection = false)
        {
            IsInLevelSelection = isInLevelSelection;

            //TODO: Implement loading progress display.
            LoadSceneAsync("BattleUI", null, () =>
            {
                LoadSceneAsync("Battleground", null, () =>
                {
                    MapTileGeneratorEditor mapTileGeneratorEditor;

                    if (!CC.MBR.TryGet(out mapTileGeneratorEditor))
                    {
                        Debug.Log("MapTileGeneratorEditor can't be retrieved.");
                        return;
                    }

                    MapGenerationData mapGenerationData = mapTileGeneratorEditor.LoadMapGenerationData(levelName);
                    CC.BSC.IntializeBattle(mapGenerationData.m_Teams, levelName);

                    CameraControls cameraControls;
                    if (!CC.MBR.TryGet(out cameraControls))
                    {
                        Debug.Log("CameraControls can't be retrieved.");
                        return;
                    }

                    cameraControls.SetMinMaxCameraPositions(mapGenerationData.m_MaxCameraPosition, mapGenerationData.m_MinCameraPosition);

                    mapTileGeneratorEditor.LoadExistingMap(mapGenerationData);
                    CC.BSC.StartBattle();

                    if (onLoadedToLevel != null)
                    {
                        onLoadedToLevel();
                    }
                });
            });
        }

        /// <summary>
        /// Unloads the existing scenes.
        /// </summary>
        /// <param name="onScenesUnloaded">Invoked when the existing scene are unloaded.</param>
        public void UnloadExistingScenes(Action onScenesUnloaded)
        {
            UnloadSceneAsync("BattleUI", null, () =>
            {
                UnloadSceneAsync("Battleground", null, () =>
                {
                    CC.Reset();

                    if (onScenesUnloaded != null)
                    {
                        onScenesUnloaded();
                    }
                });
            });
        }
    }
}
