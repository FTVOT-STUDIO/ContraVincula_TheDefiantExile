using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public enum SceneType { Title, Lobby, Main }
    public SceneType sceneType;

    public void LoadNextScene()
    {
        StartCoroutine(LoadSceneRoutine(sceneType.ToString()));
    }

    private IEnumerator LoadSceneRoutine(string target)
    {
        SceneManager.LoadScene(target);
        yield return null;
    }
}
