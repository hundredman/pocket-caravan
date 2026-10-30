using System;
using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>
    /// 맵 이동 화면 효과를 지역별로 갈아끼우는 자리. 떠나는 맵 또는 도착하는 맵에 맞는 효과가 등록돼 있으면 그것을 그리고,
    /// 없으면 GameManager의 기본 검은 페이드를 쓴다(기존 지역은 등록하지 않았으므로 그대로).
    /// 화면 표시만 바꾸며 이동·시간·저장 순서는 GameManager.Travel이 그대로 정한다.
    /// </summary>
    public abstract class TravelTransition
    {
        /// <summary>화면을 덮는 시간(초)</summary>
        public virtual float CoverSeconds => 0.12f;
        /// <summary>새 맵을 드러내는 시간(초)</summary>
        public virtual float RevealSeconds => 0.18f;
        /// <summary>다 덮인 뒤 최소한 머무는 시간(초). 씬을 불러오는 시간과 겹친다.</summary>
        public virtual float MinHoldSeconds => 0f;
        /// <summary>새 씬을 다 불러온 뒤 덮인 채 더 머무는 시간(초). 불러오는 동안 멈춰 있던 연출을 이어서 보여 줄 때 쓴다. 기본 0.</summary>
        public virtual float AfterLoadSeconds => 0f;

        /// <summary>이동을 시작할 때 한 번 부른다 (떠나는 맵, 도착 맵). 같은 효과 안에서 이동 종류별로 그림을 바꿀 때 쓴다. 기본은 아무것도 안 함.</summary>
        public virtual void Begin(string fromMap, string toMap) { }

        /// <summary>
        /// OnGUI에서 부른다. cover: 0 = 다 보임, 1 = 다 덮임. covering: 덮는 중이면 true, 드러내는 중이면 false.
        /// nextName: 도착 맵 이름. heldFor: 다 덮인 뒤 지난 시간(덮는 중에는 0).
        /// </summary>
        public abstract void Draw(float cover, bool covering, string nextName, float heldFor);

        static readonly List<(Func<string, bool> match, TravelTransition fx)> registry = new List<(Func<string, bool>, TravelTransition)>();
        static readonly List<(Func<string, string, bool> match, TravelTransition fx)> routes = new List<(Func<string, string, bool>, TravelTransition)>();

        /// <summary>출발·도착을 함께 제한하는 연출. 기존 지역 등록은 유지한다.</summary>
        public static void RegisterRoute(Func<string, string, bool> match, TravelTransition fx)
        {
            if (match != null && fx != null) routes.Add((match, fx));
        }

        /// <summary>mapId가 match에 맞는 맵을 떠나거나 도착할 때 fx를 쓴다.</summary>
        public static void Register(Func<string, bool> match, TravelTransition fx)
        {
            if (match != null && fx != null) registry.Add((match, fx));
        }

        /// <summary>떠나는 맵을 먼저, 없으면 도착 맵을 본다. 맞는 것이 없으면 null(기본 페이드).</summary>
        public static TravelTransition For(string fromMap, string toMap)
        {
            foreach (var (match, fx) in routes)
                if (match(fromMap, toMap)) return fx;
            foreach (var id in new[] { fromMap, toMap })
            {
                if (string.IsNullOrEmpty(id)) continue;
                foreach (var (match, fx) in registry)
                    if (match(id)) return fx;
            }
            return null;
        }
    }
}
