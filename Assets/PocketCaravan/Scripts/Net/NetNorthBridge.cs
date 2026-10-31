using System;
using System.Reflection;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 리액션(R)을 온라인에 알리는 연결 (2026-10-31, feature/online).
    /// 북유럽 코드(NorthSocial, 기우 담당)는 고치지 않는다. 리액션이 시작되는 것(acting이 -1 → 리액션 번호)을 읽기만 해서 NetEmote.Send로 보낸다.
    /// 이름으로 찾기 때문에 북유럽 코드에서 이름이 바뀌면 조용히 꺼진다(오류 없음).
    /// 기우 님이 StartReaction에 NetEmote.Send를 직접 넣으면 이 파일은 지워도 된다 (같은 리액션을 두 곳에서 알려도 NetEmote가 한 번만 보낸다).
    /// </summary>
    public class NetNorthBridge : MonoBehaviour
    {
        static readonly Type SocialType = Type.GetType("PocketCaravan.NorthSocial");
        static readonly FieldInfo ActingField = SocialType?.GetField("acting", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo ReactionsField = SocialType?.GetField("Reactions", BindingFlags.Static | BindingFlags.Public);

        UnityEngine.Object social;
        float nextFind;
        int lastActing = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (ActingField == null || ReactionsField == null || ActingField.FieldType != typeof(int)) return;
            if (FindFirstObjectByType<NetNorthBridge>() != null) return;
            var go = new GameObject("NetNorthBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<NetNorthBridge>();
        }

        void Update()
        {
            var net = NetClient.I;
            if (net == null || net.Status != NetClient.State.Online) { lastActing = -1; return; }
            if (social == null)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 2f;
                social = FindFirstObjectByType(SocialType);
                if (social == null) return;
            }
            int acting = (int)ActingField.GetValue(social);
            if (acting >= 0 && acting != lastActing) Announce(acting);
            lastActing = acting;
        }

        static void Announce(int i)
        {
            if (!(ReactionsField.GetValue(null) is Array list) || i >= list.Length) return;
            object r = list.GetValue(i);
            var t = r.GetType();
            string id = t.GetField("id")?.GetValue(r) as string;
            string name = t.GetField("name")?.GetValue(r) as string;
            if (!string.IsNullOrEmpty(id)) NetEmote.Send(id, name);
        }
    }
}
