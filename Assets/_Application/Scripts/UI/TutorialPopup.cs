﻿using System;
using TMPro;
using UnityEngine;

namespace GMTK2020.UI
{
    public class TutorialPopup : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tutorialText = null;

        public event Action Dismissed;

        public void ShowMessage(string message)
        {
            tutorialText.text = message;
            gameObject.SetActive(true);
        }

        public void Dismiss()
        {
            gameObject.SetActive(false);
            Dismissed?.Invoke();
        }
    }
}