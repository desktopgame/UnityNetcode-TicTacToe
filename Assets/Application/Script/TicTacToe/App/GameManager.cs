using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using UniRx;
using UniRx.Triggers;
using UniRx.Diagnostics;
using Cysharp.Threading.Tasks;
using TicTacToe.Lib.Util;

namespace TicTacToe.App
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] ControlPanel m_controlPanel;
        [SerializeField] Text m_statusText;

        // Start is called before the first frame update
        void Start()
        {
#if UNITY_SERVER
            NetworkManager.Singleton.StartServer();
            Debug.Log("start server.");
#else
            m_controlPanel.OnJoinButtonPush.Subscribe(OnJoinButton).AddTo(this);
            m_controlPanel.OnExitButtonPush.Subscribe(OnExitButton).AddTo(this);
            Board.Instance.Hide();
#endif
            Observable.FromEvent<ulong>(
                h => NetworkManager.Singleton.OnClientConnectedCallback += h,
                h => NetworkManager.Singleton.OnClientConnectedCallback -= h
            ).Subscribe(OnClientConnected).AddTo(this);
            Observable.FromEvent<ulong>(
                h => NetworkManager.Singleton.OnClientDisconnectCallback += h,
                h => NetworkManager.Singleton.OnClientDisconnectCallback -= h
            ).Subscribe(OnClientDisconnected).AddTo(this);
        }

        //
        // Server
        //

        private void OnClientConnected(ulong id)
        {
#if UNITY_SERVER
            if (NetworkManager.Singleton.ConnectedClients.Count <= 2)
            {
                // 二人揃ったらゲーム開始
                if(NetworkManager.Singleton.ConnectedClients.Count == 2)
                {
                    var p1 = NetworkManager.Singleton.ConnectedClients
                        .Select((kvp) => kvp.Value.PlayerObject.GetComponent<Player>())
                        .OrderBy((_) => Guid.NewGuid())
                        .First();
                    var p2 = NetworkManager.Singleton.ConnectedClients
                        .Select((kvp) => kvp.Value.PlayerObject.GetComponent<Player>())
                        .First((p) => p != p1);
                    p1.Setup(CellType.Maru);
                    p1.OnPositionChanged
                        .Subscribe(pos =>
                        {
                            p2.SyncClientRpc(pos, CellType.Maru);
                        });
                    p2.Setup(CellType.Batu);
                    p2.OnPositionChanged
                        .Subscribe(pos =>
                        {
                            p1.SyncClientRpc(pos, CellType.Batu);
                        });
                    Turn(p1, p2);
                }
            }
            else
            {
                NetworkManager.Singleton.DisconnectClient(id);
            }
#else   
            if (id == NetworkManager.Singleton.LocalClientId)
            {
                // 表示切り替え
                m_controlPanel.ChangeMode(ControlPanelMode.Play);
                Board.Instance.Setup();
                Board.Instance.Show();
                Debug.Log("join server.");
                // 切断時の処理、ステータスに応じた表示切り替え
                var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
                m_statusText.text = player.State.ToString();
                player.OnDestroyAsObservable()
                      .Subscribe(_ => OnExit());
                player.OnStateChanged.Subscribe(state =>
                {
                    m_statusText.text = player.State.ToString();
                }).AddTo(this);
            }
#endif
        }

        private void OnClientDisconnected(ulong id)
        {
#if UNITY_SERVER
            if (NetworkManager.Singleton.ConnectedClientsIds.Count == 2)
            {
                Debug.LogWarning("force exit.");
                NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.ConnectedClientsIds.First(e => e != id));
            }
#else
#endif
        }

        private void Turn(Player p1, Player p2)
        {
            if(!p1.IsFinished)
            {
                p1.SetPlayable();
                p1.OnStateChanged
                    .Where(state => state != PlayerState.MyTurn)
                    .Subscribe(state =>
                    {
                        Turn(p2, p1);
                    }).AddTo(this);
            }
        }

        //
        // Client
        //

        private void OnJoinButton(Unit _)
        {
            if(!NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.LogWarning("join failed.");
            }
        }

        private void OnExitButton(Unit _)
        {
            if(NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                OnExit();
            }
            else
            {
                Debug.LogWarning("exit failed.");
            }
        }

        private void OnExit()
        {
            m_controlPanel.ChangeMode(ControlPanelMode.Wait);
            Board.Instance.Hide();
            m_statusText.text = string.Empty;
        }
    }
}