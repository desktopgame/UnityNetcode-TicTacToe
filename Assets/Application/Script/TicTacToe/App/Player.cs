using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UniRx;
using Cysharp.Threading.Tasks;

namespace TicTacToe.App
{
    public enum PlayerState
    {
        Wait,
        MyTurn,
        Win,
        Lose,
        Draw
    }
    public class Player : NetworkBehaviour
    {
        public PlayerState State => m_state.Value;
        public bool IsFinished => State == PlayerState.Win || State == PlayerState.Lose || State == PlayerState.Draw;
        public IObservable<PlayerState> OnStateChanged => m_onStateChangedSubject;
        private Subject<PlayerState> m_onStateChangedSubject = new Subject<PlayerState>();
        private NetworkVariable<PlayerState> m_state = new NetworkVariable<PlayerState>(PlayerState.Wait, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Vector2Int Position => m_position.Value;
        public IObservable<Vector2Int> OnPositionChanged => m_onPositionChangedSubject;
        private Subject<Vector2Int> m_onPositionChangedSubject = new Subject<Vector2Int>();
        private NetworkVariable<Vector2Int> m_position = new NetworkVariable<Vector2Int>(new Vector2Int(-1, -1));

        public CellType CellType => m_cellType.Value;
        private NetworkVariable<CellType> m_cellType = new NetworkVariable<CellType>(CellType.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Dictionary<Vector2Int, CellType> m_cellData = new Dictionary<Vector2Int, CellType>();

        public void Setup(CellType cellType)
        {
            m_cellType.Value = cellType;
        }

        public void SetPlayable()
        {
            m_state.Value = PlayerState.MyTurn;
        }

        [ClientRpc]
        public void SyncClientRpc(Vector2Int pos, CellType type)
        {
            m_cellData[pos] = type;
            Board.Instance.Put(pos, type);
        }

        //
        // Event
        //

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_state.OnValueChanged += HandleStateChanged;
            m_position.OnValueChanged += HandlePositionChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_state.OnValueChanged -= HandleStateChanged;
            m_position.OnValueChanged -= HandlePositionChanged;
        }

        private void HandleStateChanged(PlayerState oldValue, PlayerState newValue)
        {
            m_onStateChangedSubject.OnNext(newValue);
            // 自分のターンになったら入力待ち
            if(newValue == PlayerState.MyTurn && IsLocalPlayer)
            {
                WaitForInput().Forget();
            }
        }

        private void HandlePositionChanged(Vector2Int oldValue, Vector2Int newValue)
        {
            m_onPositionChangedSubject.OnNext(newValue);
        }

        private async UniTask WaitForInput()
        {
            // 勝敗判定
            if (TryJudge()) { return; }
            // マルかバツを置く
            Vector2Int? cp = null;
            while(cp == null || m_cellData.ContainsKey(cp.Value))
            {
                cp = await Board.Instance.WaitForInput(m_cellType.Value);
            }
            m_cellData[cp.Value] = m_cellType.Value;
            Board.Instance.Put(cp.Value, m_cellType.Value);
            SetPosition_ServerRpc(cp.Value);
            // 勝敗判定
            if (!TryJudge())
            {
                SetState_ServerRpc(PlayerState.Wait);
            }
        }

        private bool TryJudge()
        {
            var state = Judge();
            if(state.HasValue)
            {
                SetState_ServerRpc(state.Value);
            }
            return state.HasValue;
        }

        private PlayerState? Judge()
        {
            PlayerState? ret = null;
            foreach(int col in Enumerable.Range(0, 3))
            {
                var chunk = Enumerable.Range(0, 3)
                          .Select(row => new Vector2Int(col, row))
                          .Where(p => m_cellData.ContainsKey(p))
                          .Select(p => m_cellData[p]);
                ret ??= Judge(chunk);
            }
            foreach (int row in Enumerable.Range(0, 3))
            {
                var chunk = Enumerable.Range(0, 3)
                          .Select(col => new Vector2Int(col, row))
                          .Where(p => m_cellData.ContainsKey(p))
                          .Select(p => m_cellData[p]);
                ret ??= Judge(chunk);
            }
            if (!ret.HasValue)
            {
                var chunk = Enumerable.Range(0, 3)
                          .Select(i => new Vector2Int(i, i))
                          .Where(p => m_cellData.ContainsKey(p))
                          .Select(p => m_cellData[p]);
                ret ??= Judge(chunk);
            }
            if (!ret.HasValue)
            {
                var chunk = Enumerable.Range(0, 3)
                          .Select(i => new Vector2Int(2 - i, i))
                          .Where(p => m_cellData.ContainsKey(p))
                          .Select(p => m_cellData[p]);
                ret ??= Judge(chunk);
            }
            if (m_cellData.Count == 3 * 3)
            {
                ret ??= PlayerState.Draw;
            }
            return ret;
        }

        private PlayerState? Judge(IEnumerable<CellType> chunk)
        {
            if(chunk.Count() < 3)
            {
                return null;
            }
            chunk = chunk.Distinct();
            if (chunk.Count() == 1 && chunk.First() != CellType.Empty)
            {
                if (chunk.First() == m_cellType.Value)
                {
                    return PlayerState.Win;
                }
                else
                {
                    return PlayerState.Lose;
                }
            }
            return null;
        }

        //
        // Rpc
        //

        [ServerRpc(RequireOwnership = false)]
        private void SetState_ServerRpc(PlayerState state)
        {
            m_state.Value = state;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPosition_ServerRpc(Vector2Int pos)
        {
            m_position.Value = pos;
        }
    }
}