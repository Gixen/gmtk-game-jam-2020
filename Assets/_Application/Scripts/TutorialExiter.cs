﻿using GMTK2020.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK2020
{
    public class TutorialExiter : MonoBehaviour
    {
        [SerializeField] private string levelSceneName = null;
        [SerializeField] private BoardRenderer boardRenderer = null;

        private void Start()
        {
        }

        public void LoadLevel()
        {
            FindObjectOfType<SoundManager>()?.PlayEffect(SoundManager.Effect.CLICK);
            boardRenderer.CancelAnimation();
            SceneManager.LoadScene(levelSceneName);
        }
    }
}