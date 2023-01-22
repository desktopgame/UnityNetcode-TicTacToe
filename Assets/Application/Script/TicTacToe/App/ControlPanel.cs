using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace TicTacToe.App
{
    public enum ControlPanelMode
    {
        None = 0,
        Wait,
        Play
    }

    public class ControlPanel : MonoBehaviour
    {
        [SerializeField] Button m_joinButton;
        [SerializeField] Button m_exitButton;

        public ControlPanelMode Mode { private set; get; }
        public IObservable<Unit> OnJoinButtonPush => m_joinButton.OnClickAsObservable();
        public IObservable<Unit> OnExitButtonPush => m_exitButton.OnClickAsObservable();

        private void Awake()
        {
            ChangeMode(ControlPanelMode.Wait);
        }

        public void ChangeMode(ControlPanelMode mode)
        {
            if(mode == Mode)
            {
                return;
            }
            Mode = mode;
            switch(mode)
            {
                case ControlPanelMode.Wait:
                    m_joinButton.gameObject.SetActive(true);
                    m_exitButton.gameObject.SetActive(false);
                    break;
                case ControlPanelMode.Play:
                    m_joinButton.gameObject.SetActive(false);
                    m_exitButton.gameObject.SetActive(true);
                    break;
            }
        }
    }
}