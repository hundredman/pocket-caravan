using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 2026-11-03 여관 통합: 마을마다 여관 사람(이름에 여관·숙소·객주) 하나씩 두기로 하고, 없던 21개 맵 JSON에 talker를 더했다.
    /// 그 맵들만 JSON → 씬으로 다시 만든다 (다른 담당 씬은 재직렬화하지 않게 BuildSelected).
    /// 이미 여관 사람이 있던 맵: north_birka, north_herd, north_c_gatetown, north_c_crossing, Gaegyeong, venice_rialto, venice_c_post, arabia_ahsa, arabia_farmroad.
    /// </summary>
    public static class IntegrationInnRebuild
    {
        public static readonly string[] Maps =
        {
            "north_hunter", "north_laugar", "north_naru",
            "north_c_creek", "north_c_snowwood",
            "Dakmok", "Naru", "goryeo_jangdan", "goryeo_songak",
            "venice_burano", "venice_murano", "venice_chioggia", "venice_santerasmo",
            "venice_c_landing", "venice_c_vineyard",
            "arabia_jeddah", "arabia_sanaa", "arabia_taiz", "arabia_zabid",
            "transit_ferry", "transit_woodtown",
        };

        [MenuItem("Pocket Caravan/통합/여관 사람 넣은 맵 21개 다시 만들기")]
        public static void Rebuild()
        {
            MapJsonBuilder.BuildSelected(Maps);
            Debug.Log($"[통합] 여관 사람 맵 {Maps.Length}개를 다시 만들었습니다. 각 마을 장터 근처에서 '여관 주인/숙소 주인/객주 주인'을 찾아 말을 걸어 보세요.");
        }
    }
}
