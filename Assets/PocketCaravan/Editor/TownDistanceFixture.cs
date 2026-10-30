using System;
using System.Reflection;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 검사 전용: 거리 설정을 잠시 예전 방식(current, 마을 좌표 맨해튼)으로 바꾼다.
    /// R2 가격 기준값(2026-10-25)은 current 거리로 만든 것이라, 가격 규칙 자체를 확인할 때 쓴다.
    /// 본편은 M4(2026-10-28)부터 layoutD. Dispose하면 원래 설정으로 돌아간다.
    /// </summary>
    public sealed class TownDistanceFixture : IDisposable
    {
        static readonly FieldInfo Cache = typeof(TownDistance).GetField("cached", BindingFlags.Static | BindingFlags.NonPublic);
        public static TownDistanceFixture Current()
        {
            var settings = JsonUtility.FromJson<TownDistance.Settings>(Resources.Load<TextAsset>("TownRoutes").text);
            settings.mode = "current"; Cache.SetValue(null, settings);
            return new TownDistanceFixture();
        }
        public void Dispose() { Cache.SetValue(null, null); }
    }
}
