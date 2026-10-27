using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 고른 출신(GameManager.Player.origin)의 주인공 그림을 어느 지역 맵에서든 똑같이 입힌다.
    /// 맵마다 플레이어가 새로 생기고 지역 씬마다 기본 그림이 달라서(너구리·고양이 등) 넘어갈 때 바뀌어 보이던 문제를 막는다.
    /// 출신에 playerFrames가 없으면 씬에 있는 그림을 그대로 둔다. 씬이나 GameManager에 붙이지 않고 저절로 생긴다.
    /// </summary>
    public class PlayerAppearance : MonoBehaviour
    {
        PlayerController applied;
        OriginData appliedOrigin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("PlayerAppearance");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerAppearance>();
        }

        // 씬 로드·출신 변경 순서에 기대지 않도록 매 프레임 "플레이어나 출신이 바뀌었는지"만 본다
        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null) return;
            var origin = gm.Player.origin;
            if (applied != null && applied && origin == appliedOrigin) return;
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            applied = pc;
            appliedOrigin = origin;
            Apply(pc, origin);
        }

        public static bool HasFrames(OriginData origin)
        {
            if (origin == null || origin.playerFrames == null || origin.playerFrames.Length < 16) return false;
            for (int i = 0; i < 16; i++) if (origin.playerFrames[i] == null) return false;
            return true;
        }

        static void Apply(PlayerController pc, OriginData origin)
        {
            if (pc.anim == null || !HasFrames(origin)) return;
            pc.anim.frames = (Sprite[])origin.playerFrames.Clone();
            var sr = pc.anim.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = origin.playerFrames[0];
        }
    }
}
