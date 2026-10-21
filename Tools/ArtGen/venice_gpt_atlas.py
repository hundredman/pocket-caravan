"""GPT로 생성한 베네치아 에셋 시트를 16px 격자 아틀라스로 바꾼다 (임시 아트, 팀 아트 승인 전).
    사용: python Tools/ArtGen/venice_gpt_atlas.py [원본 시트 폴더]
    - 원본 시트(저장소 밖, 기본 ~/Downloads/venice_gpt)는 커밋하지 않는다.
    - 결과: Tools/ArtGen/venice_atlas.png + venice_atlas.json (커밋 대상, MapGen이 읽음)
    물체마다 목표 칸 수에 맞춰 비율을 유지해 줄이고, 칸 안의 최빈색으로 픽셀을 고른다."""
import json, os, sys
from collections import Counter
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.expanduser("~"), "Downloads", "venice_gpt")

# (이름, 시트 파일, 원본 상자 x0 y0 x1 y1, 칸 너비, 칸 높이)
OBJECTS = [
    ("house_ochre", "sheet1_houses.png", (84, 60, 676, 508), 4, 3),
    ("house_rose", "sheet1_houses.png", (768, 96, 1360, 508), 4, 3),
    ("house_altana", "sheet1_houses.png", (76, 576, 676, 1000), 4, 3),
    ("house_brick", "sheet1_houses.png", (768, 604, 1368, 1000), 4, 3),
    ("campanile", "sheet2_landmarks.png", (80, 20, 272, 736), 2, 6),
    ("glass_workshop", "sheet2_landmarks.png", (364, 324, 868, 728), 4, 3),
    ("fondaco", "sheet2_landmarks.png", (944, 348, 1572, 716), 5, 3),
    ("salt_hut", "sheet2_landmarks.png", (1628, 412, 1936, 724), 3, 3),
    ("barge_loaded", "sheet3_boats.png", (844, 128, 1736, 440), 5, 2),
    ("gondola", "sheet3_boats.png", (40, 212, 792, 400), 4, 1),
    ("barge_empty", "sheet3_boats.png", (16, 496, 900, 804), 5, 2),
    ("fishing_boat", "sheet3_boats.png", (940, 568, 1428, 752), 3, 1),
    ("poles", "sheet3_boats.png", (1472, 560, 1596, 772), 1, 2),
    ("pole_striped", "sheet3_boats.png", (1644, 536, 1740, 776), 1, 2),
    ("stall_red", "sheet4b_stalls_muted.png", (12, 76, 468, 528), 2, 2),
    ("stall_blue", "sheet4b_stalls_muted.png", (476, 76, 900, 536), 2, 2),
    ("stall_fish", "sheet4b_stalls_muted.png", (912, 112, 1508, 560), 3, 2),
    ("stall_spice", "sheet4b_stalls_muted.png", (736, 572, 1180, 980), 2, 2),
    ("stall_glass", "sheet4b_stalls_muted.png", (252, 696, 688, 972), 2, 2),
    ("well", "sheet5_props.png", (56, 92, 440, 480), 2, 2),
    ("banner", "sheet5_props.png", (520, 28, 748, 480), 1, 2),
    ("sack_salt", "sheet5_props.png", (800, 264, 1004, 476), 1, 1),
    ("sack_grain", "sheet5_props.png", (1044, 260, 1244, 476), 1, 1),
    ("crate_glass", "sheet5_props.png", (1284, 204, 1488, 476), 1, 1),
    ("crate_fish", "sheet5_props.png", (64, 524, 296, 740), 1, 1),
    ("crate_lemon", "sheet5_props.png", (368, 512, 596, 740), 1, 1),
    ("barrel", "sheet5_props.png", (652, 508, 864, 756), 1, 1),
    ("jars", "sheet5_props.png", (920, 500, 1172, 756), 1, 1),
    ("crate", "sheet5_props.png", (1228, 532, 1484, 740), 1, 1),
    ("flower_pot", "sheet5_props.png", (236, 752, 452, 988), 1, 1),
    ("bench", "sheet5_props.png", (572, 808, 964, 984), 2, 1),
    ("rope", "sheet5_props.png", (1068, 812, 1324, 1000), 1, 1),
    ("salt_mound", "sheet6_salt.png", (284, 160, 756, 496), 2, 2),
    ("salt_rake", "sheet6_salt.png", (800, 140, 988, 496), 1, 2),
    ("salt_heap", "sheet6_salt.png", (28, 292, 264, 472), 1, 1),
    ("salt_barrow", "sheet6_salt.png", (1016, 216, 1524, 480), 2, 1),
    ("net", "sheet6_salt.png", (452, 536, 844, 892), 2, 2),
    ("sacks_row", "sheet6_salt.png", (16, 640, 424, 884), 2, 1),
    ("planks", "sheet6_salt.png", (876, 672, 1260, 904), 2, 1),
    ("anchor", "sheet6_salt.png", (1280, 620, 1516, 892), 1, 1),
    ("cypress", "sheet9_plants.png", (100, 8, 280, 496), 1, 3),
    ("olive", "sheet9_plants.png", (344, 116, 712, 488), 3, 3),
    ("tree", "sheet9_plants.png", (736, 120, 1076, 488), 3, 3),
    ("lemon_pot", "sheet9_plants.png", (1140, 112, 1392, 492), 1, 2),
    ("flower_box", "sheet9_plants.png", (44, 568, 352, 756), 2, 1),
    ("vine", "sheet9_plants.png", (492, 512, 652, 840), 1, 2),
    ("trellis", "sheet9_plants.png", (764, 500, 1016, 824), 2, 2),
    ("hedge", "sheet9_plants.png", (1116, 592, 1404, 748), 2, 1),
    ("pot_plant", "sheet9_plants.png", (104, 804, 272, 1056), 1, 1),
    ("planter", "sheet9_plants.png", (380, 852, 708, 1056), 2, 1),
    ("reeds", "sheet9_plants.png", (796, 820, 1052, 1068), 1, 1),
    ("flower_pot2", "sheet9_plants.png", (1180, 828, 1356, 1060), 1, 1),
    ("flower_cart", "sheet10_props2.png", (20, 88, 316, 332), 2, 2),
    ("lemon_tree", "sheet10_props2.png", (352, 24, 604, 332), 2, 2),
    ("bench_towel", "sheet10_props2.png", (960, 148, 1236, 320), 2, 1),
    ("flower_arch", "sheet10_props2.png", (20, 348, 308, 632), 2, 2),
    ("shop_sign", "sheet10_props2.png", (356, 364, 592, 624), 1, 1),
    ("amphora", "sheet10_props2.png", (984, 380, 1224, 632), 1, 1),
    ("laundry", "sheet10_props2.png", (628, 400, 936, 620), 2, 2),
    ("lemon_crate", "sheet10_props2.png", (28, 704, 292, 904), 2, 1),
    ("flower_shelf", "sheet10_props2.png", (324, 660, 616, 904), 2, 2),
    ("luggage", "sheet10_props2.png", (632, 716, 936, 908), 2, 1),
    ("awning_table", "sheet10_props2.png", (944, 680, 1248, 904), 2, 2),
    ("ladder_rope", "sheet10_props2.png", (632, 972, 940, 1200), 2, 2),
    ("barrow_sacks", "sheet10_props2.png", (944, 964, 1236, 1216), 2, 2),
    ("stone_steps", "sheet12_stone_nature.png", (60, 136, 380, 352), 2, 2),
    ("wall_corner", "sheet12_stone_nature.png", (788, 116, 1016, 352), 2, 2),
    ("wall_low", "sheet12_stone_nature.png", (444, 204, 724, 348), 2, 1),
    ("reeds_tall", "sheet12_stone_nature.png", (128, 412, 324, 708), 1, 2),
    ("tuft", "sheet12_stone_nature.png", (500, 564, 656, 700), 1, 1),
    ("rocks", "sheet12_stone_nature.png", (756, 568, 1064, 704), 2, 1),
    ("seaweed", "sheet12_stone_nature.png", (1196, 564, 1372, 704), 1, 1),
    ("net_rack", "sheet12_stone_nature.png", (1128, 756, 1404, 1012), 2, 2),
    ("stone_planter", "sheet12_stone_nature.png", (56, 812, 392, 996), 2, 1),
    ("pot_succulent", "sheet12_stone_nature.png", (512, 816, 676, 1000), 1, 1),
    ("stepping_stones", "sheet12_stone_nature.png", (776, 864, 1056, 1004), 2, 1),
    # 두 번째 건물·항구 시트: 원본 약 6px = 게임 1px. 비율 맞춤 대신 같은 축척(6.5)으로 줄여 문·창 픽셀 크기를 맞춘다.
    ("house_narrow", "sheet13_buildings2.png", (124, 64, 324, 512), 2, 5, 6.5),
    ("house_ell", "sheet13_buildings2.png", (468, 84, 972, 520), 5, 5, 6.5),
    ("row_houses", "sheet13_buildings2.png", (1040, 180, 1488, 500), 5, 4, 6.5),
    ("inn", "sheet13_buildings2.png", (1068, 576, 1508, 944), 5, 4, 6.5),
    ("loggia", "sheet13_buildings2.png", (48, 600, 516, 940), 5, 4, 6.5),
    ("workshop2", "sheet13_buildings2.png", (592, 600, 1004, 936), 4, 4, 6.5),
    ("boat_shed", "sheet14_harbor.png", (24, 92, 568, 476), 6, 4, 6.5),
    ("fisher_house", "sheet14_harbor.png", (612, 72, 1024, 476), 4, 4, 6.5),
    ("pergola", "sheet14_harbor.png", (1072, 120, 1508, 476), 5, 4, 6.5),
    ("garden_wall_l", "sheet14_harbor.png", (36, 568, 268, 908), 3, 4, 6.5),
    ("garden_wall_r", "sheet14_harbor.png", (308, 568, 520, 908), 3, 4, 6.5),
    ("dock_ell", "sheet14_harbor.png", (1076, 568, 1492, 908), 4, 4, 6.5),
    ("footbridge", "sheet14_harbor.png", (560, 636, 1016, 880), 5, 3, 6.5),
    ("m_slab0", "sheet11_terrain.png", (4, 4, 310, 310), 2, 2),
    ("m_slab1", "sheet11_terrain.png", (318, 4, 623, 310), 2, 2),
    ("m_slab2", "sheet11_terrain.png", (631, 4, 936, 310), 2, 2),
    ("m_herring", "sheet11_terrain.png", (944, 4, 1250, 310), 2, 2),
    ("m_mosaic", "sheet11_terrain.png", (631, 318, 936, 623), 2, 2),
    ("m_sand", "sheet11_terrain.png", (944, 318, 1250, 623), 2, 2),
    # ---- 지역 시트 (Venice_World_Claude_Pack/assets, 분홍 배경 제거본 district/*_keyed.png) ----
    # 건물·큰 구조물은 같은 축척(6.5px→1px)으로 상대 크기를 맞추고, 소품은 제안 칸 수 안에 맞춘다.
    ("cap_hall", "district/01_capital_keyed.png", (28, 116, 596, 580), 6, 5, 6.5),
    ("cap_tower", "district/01_capital_keyed.png", (696, 20, 840, 600), 2, 6, 6.5),
    ("cap_residence", "district/01_capital_keyed.png", (948, 96, 1528, 624), 6, 6, 6.5),
    ("cap_well", "district/01_capital_keyed.png", (172, 740, 356, 944), 2, 2),
    ("cap_arcade", "district/01_capital_keyed.png", (580, 660, 956, 944), 4, 3, 6.5),
    ("cap_board", "district/01_capital_keyed.png", (1116, 676, 1368, 944), 2, 2),
    ("gls_workshop", "district/02_glassworks_keyed.png", (40, 44, 608, 524), 6, 5, 6.5),
    ("gls_kiln", "district/02_glassworks_keyed.png", (652, 132, 1032, 524), 4, 4, 6.5),
    ("gls_artisan", "district/02_glassworks_keyed.png", (1116, 56, 1492, 536), 4, 5, 6.5),
    ("gls_bench", "district/02_glassworks_keyed.png", (88, 620, 520, 944), 3, 2),
    ("gls_cullet", "district/02_glassworks_keyed.png", (692, 696, 876, 912), 1, 1),
    ("gls_furnace", "district/02_glassworks_keyed.png", (1056, 596, 1480, 952), 3, 3),
    ("orc_house", "district/03_orchard_keyed.png", (32, 136, 488, 444), 5, 3, 6.5),
    ("orc_shed", "district/03_orchard_keyed.png", (528, 144, 1044, 444), 5, 3, 6.5),
    ("orc_cottage", "district/03_orchard_keyed.png", (1076, 56, 1500, 472), 5, 4, 6.5),
    ("orc_lemon", "district/03_orchard_keyed.png", (88, 560, 388, 936), 2, 3),
    ("orc_pergola", "district/03_orchard_keyed.png", (544, 608, 960, 916), 4, 3, 6.5),
    ("orc_cart", "district/03_orchard_keyed.png", (1084, 680, 1492, 920), 3, 2),
    ("fsh_house", "district/04_fishing_keyed.png", (36, 88, 540, 496), 5, 4, 6.5),
    ("fsh_netshed", "district/04_fishing_keyed.png", (580, 124, 924, 488), 4, 4, 6.5),
    ("fsh_shelter", "district/04_fishing_keyed.png", (972, 92, 1524, 496), 6, 4, 6.5),
    ("fsh_rack", "district/04_fishing_keyed.png", (112, 620, 416, 864), 2, 2),
    ("fsh_dinghy", "district/04_fishing_keyed.png", (564, 676, 1000, 840), 3, 1),
    ("fsh_jetty", "district/04_fishing_keyed.png", (1156, 580, 1388, 928), 2, 3),
    ("prt_warehouse", "district/05_port_keyed.png", (28, 116, 648, 444), 6, 4, 6.5),
    ("prt_gate", "district/05_port_keyed.png", (672, 52, 1056, 444), 4, 4, 6.5),
    ("prt_guild", "district/05_port_keyed.png", (1092, 96, 1524, 460), 5, 4, 6.5),
    ("prt_crane", "district/05_port_keyed.png", (40, 532, 512, 928), 3, 3),
    ("prt_cargo", "district/05_port_keyed.png", (596, 632, 940, 904), 2, 2),
    ("prt_pier", "district/05_port_keyed.png", (1016, 560, 1504, 948), 5, 4, 6.5),
    # 기존 집의 지역 색 변형 (모양은 같고 지붕·벽 재료색만 지역 규칙으로): 수도=장밋빛 지붕, 유리공방=짙은 점토 지붕+청록 벽, 어촌=청회 벽
    ("cap_narrow", "sheet13_buildings2.png", (124, 64, 324, 512), 2, 5, 6.5, "rose"),
    ("cap_row", "sheet13_buildings2.png", (1040, 180, 1488, 500), 5, 4, 6.5, "rose"),
    ("cap_ell", "sheet13_buildings2.png", (468, 84, 972, 520), 5, 5, 6.5, "rose"),
    ("cap_inn", "sheet13_buildings2.png", (1068, 576, 1508, 944), 5, 4, 6.5, "rose"),
    ("gls_narrow", "sheet13_buildings2.png", (124, 64, 324, 512), 2, 5, 6.5, "glass"),
    ("gls_row", "sheet13_buildings2.png", (1040, 180, 1488, 500), 5, 4, 6.5, "glass"),
    ("fsh_small", "sheet14_harbor.png", (612, 72, 1024, 476), 4, 4, 6.5, "fish"),
    # ---- 지역별 바닥 질감 (sheet15): 칸 하나 = 2×2타일 무늬. 도트 정리(tidy)는 하지 않는다 ----
    ("g_cap_slab0", "sheet15_ground.png", (28, 21, 265, 258), 2, 2),
    ("g_cap_slab1", "sheet15_ground.png", (305, 21, 542, 258), 2, 2),
    ("g_cap_inlay", "sheet15_ground.png", (580, 21, 817, 258), 2, 2),
    ("g_cap_edge", "sheet15_ground.png", (857, 21, 1094, 258), 2, 2),
    ("g_gls_brick0", "sheet15_ground.png", (22, 282, 271, 531), 2, 2),
    ("g_gls_brick1", "sheet15_ground.png", (299, 282, 548, 531), 2, 2),
    ("g_gls_soot", "sheet15_ground.png", (574, 282, 823, 531), 2, 2),
    ("g_gls_earth", "sheet15_ground.png", (851, 282, 1100, 531), 2, 2),
    ("g_orc_grass0", "sheet15_ground.png", (24, 556, 269, 801), 2, 2),
    ("g_orc_grass1", "sheet15_ground.png", (301, 556, 546, 801), 2, 2),
    ("g_orc_petal", "sheet15_ground.png", (576, 556, 821, 801), 2, 2),
    ("g_orc_dirt", "sheet15_ground.png", (853, 556, 1098, 801), 2, 2),
    ("g_fsh_cobble0", "sheet15_ground.png", (23, 825, 270, 1072), 2, 2),
    ("g_fsh_cobble1", "sheet15_ground.png", (300, 825, 547, 1072), 2, 2),
    ("g_fsh_wetsand", "sheet15_ground.png", (575, 825, 822, 1072), 2, 2),
    ("g_fsh_deck", "sheet15_ground.png", (852, 825, 1099, 1072), 2, 2),
    ("g_prt_granite0", "sheet15_ground.png", (21, 1112, 272, 1363), 2, 2),
    ("g_prt_granite1", "sheet15_ground.png", (298, 1112, 549, 1363), 2, 2),
    ("g_prt_ruts", "sheet15_ground.png", (573, 1112, 824, 1363), 2, 2),
    ("g_prt_stain", "sheet15_ground.png", (850, 1112, 1101, 1363), 2, 2),
    # ---- 수도 랜드마크·장식 (sheet16·17) ----
    ("cp_cathedral", "district/sheet16_keyed.png", (840, 112, 1516, 844), 7, 7),
    ("cp_tower", "district/sheet16_keyed.png", (1580, 12, 1744, 852), 2, 7),
    ("cp_palace", "district/sheet16_keyed.png", (36, 256, 804, 856), 8, 6),
    ("cp_lion_column", "district/sheet17_keyed.png", (344, 16, 496, 352), 1, 3),
    ("cp_saint_column", "district/sheet17_keyed.png", (712, 16, 824, 352), 1, 3),
    ("cp_well", "district/sheet17_keyed.png", (1004, 128, 1232, 352), 2, 2),
    ("cp_silk_stalls", "district/sheet17_keyed.png", (84, 356, 716, 572), 6, 2),
    ("cp_silk_table", "district/sheet17_keyed.png", (792, 408, 1128, 572), 2, 1),
    ("cp_tapestry", "district/sheet17_keyed.png", (1204, 356, 1456, 572), 2, 2),
    ("cp_brazier", "district/sheet17_keyed.png", (224, 580, 340, 776), 1, 2),
    ("cp_planter", "district/sheet17_keyed.png", (592, 612, 852, 760), 2, 1),
    ("cp_bench", "district/sheet17_keyed.png", (1004, 624, 1320, 760), 2, 1),
    ("cp_pennant", "district/sheet17_keyed.png", (1096, 744, 1408, 1000), 2, 2),
    ("cp_mooring", "district/sheet17_keyed.png", (240, 784, 316, 1000), 1, 2),
    ("cp_bridge", "district/sheet17_keyed.png", (544, 792, 916, 980), 3, 2),
    # ---- 생활 소품 팩 (Venice_District_Expansion_Pack): 캐릭터 키(24px) 기준 실제 크기로 칸 수를 정하고 그 안에 맞춘다 ----
    ("px_cap_fountain", "district/01_capital_props_keyed.png", (72, 124, 440, 464), 2, 2),
    ("px_cap_flower_cart", "district/01_capital_props_keyed.png", (508, 116, 964, 472), 2, 2),
    ("px_cap_bunting", "district/01_capital_props_keyed.png", (1020, 136, 1484, 436), 3, 2),
    ("px_cap_stone_bench", "district/01_capital_props_keyed.png", (72, 616, 512, 872), 2, 1),
    ("px_cap_bread_stall", "district/01_capital_props_keyed.png", (592, 544, 964, 900), 2, 2),
    ("px_cap_flag_red", "district/01_capital_props_keyed.png", (1104, 476, 1256, 912), 1, 3),
    ("px_cap_flag_blue", "district/01_capital_props_keyed.png", (1272, 476, 1424, 908), 1, 3),
    ("px_gls_bottle_rack", "district/02_glass_props_keyed.png", (32, 104, 516, 460), 2, 2),
    ("px_gls_cooling", "district/02_glass_props_keyed.png", (544, 188, 976, 460), 2, 2),
    ("px_gls_sand_cart", "district/02_glass_props_keyed.png", (1024, 112, 1516, 500), 2, 2),
    ("px_gls_ornaments", "district/02_glass_props_keyed.png", (40, 536, 504, 924), 2, 2),
    ("px_gls_craft_bench", "district/02_glass_props_keyed.png", (548, 568, 1016, 920), 2, 2),
    ("px_gls_molds", "district/02_glass_props_keyed.png", (1072, 572, 1496, 928), 1, 1),
    ("px_orc_lemon_tree2", "district/03_orchard_props_keyed.png", (64, 40, 456, 476), 2, 3),
    ("px_orc_ladder_basket", "district/03_orchard_props_keyed.png", (564, 60, 984, 480), 2, 2),
    ("px_orc_beehive", "district/03_orchard_props_keyed.png", (1112, 72, 1460, 468), 1, 1),
    ("px_orc_hedge_gate", "district/03_orchard_props_keyed.png", (24, 544, 476, 972), 3, 3),
    ("px_orc_drying_rack", "district/03_orchard_props_keyed.png", (536, 576, 1012, 960), 2, 2),
    ("px_orc_press", "district/03_orchard_props_keyed.png", (1116, 532, 1460, 992), 1, 2),
    ("px_fsh_fish_stall", "district/04_fishing_props_keyed.png", (60, 68, 484, 480), 2, 2),
    ("px_fsh_traps", "district/04_fishing_props_keyed.png", (552, 104, 964, 464), 2, 1),
    ("px_fsh_net_mend", "district/04_fishing_props_keyed.png", (1036, 148, 1492, 444), 2, 1),
    ("px_fsh_catch", "district/04_fishing_props_keyed.png", (36, 584, 508, 944), 2, 1),
    ("px_fsh_fish_dry", "district/04_fishing_props_keyed.png", (548, 540, 1000, 924), 2, 2),
    ("px_fsh_mooring", "district/04_fishing_props_keyed.png", (1092, 592, 1480, 940), 2, 1),
    ("px_prt_scale", "district/05_port_props_keyed.png", (36, 32, 480, 472), 2, 2),
    ("px_prt_cargo_cart", "district/05_port_props_keyed.png", (524, 60, 1012, 452), 2, 2),
    ("px_prt_sailcloth", "district/05_port_props_keyed.png", (1040, 104, 1516, 460), 2, 1),
    ("px_prt_gangplank", "district/05_port_props_keyed.png", (32, 564, 512, 968), 2, 2),
    ("px_prt_trade_flag", "district/05_port_props_keyed.png", (576, 484, 960, 992), 2, 3),
    ("px_prt_hull_cradle", "district/05_port_props_keyed.png", (1024, 524, 1516, 992), 3, 2),
    # ---- 마을별 새 건물·소품 (sheet18~21, 목록 끝에 붙여 기존 칸 위치 유지) ----
    ("n_gls_workshop", "district/sheet18_glass_keyed.png", (36, 36, 632, 512), 5, 4, 7.5),
    ("n_gls_shop", "district/sheet18_glass_keyed.png", (656, 28, 968, 504), 3, 4, 7.5),
    ("n_gls_kilnshed", "district/sheet18_glass_keyed.png", (1000, 84, 1500, 504), 5, 4, 7.5),
    ("n_gls_sand", "district/sheet18_glass_keyed.png", (112, 572, 444, 744), 2, 1),
    ("n_gls_wood", "district/sheet18_glass_keyed.png", (620, 552, 912, 756), 2, 1),
    ("n_gls_cullet", "district/sheet18_glass_keyed.png", (1108, 552, 1352, 764), 1, 1),
    ("n_gls_blowbench", "district/sheet18_glass_keyed.png", (76, 788, 480, 988), 2, 1),
    ("n_gls_bowls", "district/sheet18_glass_keyed.png", (608, 788, 956, 972), 2, 1),
    ("n_gls_cups", "district/sheet18_glass_keyed.png", (1120, 788, 1428, 984), 2, 1),
    ("n_orc_farmhouse", "district/sheet19_orchard_keyed.png", (12, 36, 624, 380), 6, 3, 7.5),
    ("n_orc_cottage", "district/sheet19_orchard_keyed.png", (644, 56, 1016, 376), 4, 3, 7.5),
    ("n_orc_shed", "district/sheet19_orchard_keyed.png", (1048, 60, 1524, 372), 4, 3, 7.5),
    ("n_orc_fig", "district/sheet19_orchard_keyed.png", (104, 396, 520, 736), 3, 3),
    ("n_orc_olive", "district/sheet19_orchard_keyed.png", (596, 404, 976, 744), 3, 3),
    ("n_orc_lemon", "district/sheet19_orchard_keyed.png", (1064, 400, 1428, 744), 3, 3),
    ("n_orc_wall", "district/sheet19_orchard_keyed.png", (132, 812, 392, 932), 2, 1),
    ("n_orc_wallcorner", "district/sheet19_orchard_keyed.png", (500, 800, 672, 948), 1, 1),
    ("n_orc_vegbed", "district/sheet19_orchard_keyed.png", (800, 756, 1100, 984), 2, 2),
    ("n_orc_hay", "district/sheet19_orchard_keyed.png", (1212, 804, 1388, 960), 1, 1),
    ("n_fsh_house", "district/sheet20_fishing_keyed.png", (12, 16, 540, 420), 5, 4, 7.5),
    ("n_fsh_netshed", "district/sheet20_fishing_keyed.png", (568, 76, 964, 412), 4, 3, 7.5),
    ("n_fsh_shelter", "district/sheet20_fishing_keyed.png", (980, 56, 1528, 436), 5, 4, 7.5),
    ("n_fsh_boat", "district/sheet20_fishing_keyed.png", (52, 492, 552, 680), 3, 1),
    ("n_fsh_netpile", "district/sheet20_fishing_keyed.png", (668, 524, 900, 664), 1, 1),
    ("n_fsh_traps", "district/sheet20_fishing_keyed.png", (1040, 500, 1340, 672), 2, 1),
    ("n_fsh_fishrack", "district/sheet20_fishing_keyed.png", (108, 712, 456, 988), 2, 2),
    ("n_fsh_anchor", "district/sheet20_fishing_keyed.png", (632, 760, 896, 972), 1, 1),
    ("n_fsh_driftwood", "district/sheet20_fishing_keyed.png", (1004, 720, 1324, 980), 2, 2),
    ("n_prt_warehouse", "district/sheet21_port_keyed.png", (20, 88, 748, 392), 7, 3, 7.5),
    ("n_prt_customs", "district/sheet21_port_keyed.png", (776, 4, 1128, 412), 3, 4, 7.5),
    ("n_prt_guild", "district/sheet21_port_keyed.png", (1156, 60, 1512, 424), 3, 4, 7.5),
    ("n_prt_crane", "district/sheet21_port_keyed.png", (124, 408, 572, 720), 3, 2),
    ("n_prt_bales", "district/sheet21_port_keyed.png", (668, 532, 908, 728), 2, 2),
    ("n_prt_spice", "district/sheet21_port_keyed.png", (1056, 540, 1412, 720), 2, 1),
    ("n_prt_amphorae", "district/sheet21_port_keyed.png", (132, 760, 544, 984), 2, 1),
    ("n_prt_bollard", "district/sheet21_port_keyed.png", (672, 808, 900, 980), 1, 1),
    ("n_prt_scale", "district/sheet21_port_keyed.png", (1028, 744, 1412, 988), 2, 1),
]
# 지형 시트(sheet11)의 칸 하나는 2×2타일 바닥 무늬('m_*')로 쓴다. 물가는 venice_tiles.py가 색을 따서 그린다.
GRIDS = []
ATLAS_W = 16   # 칸


def recolor(img, mode):
    """지역 재료색. 주황 기와(채도 높은 주황) → 지붕색, 크림 회벽(채도 낮은 노랑) → 벽색. 명암은 유지."""
    import colorsys
    roof = {"rose": (.955, .42, .60), "glass": (.04, .42, .62), "fish": (.07, .30, .74)}[mode]
    wall = {"rose": None, "glass": (.47, .26, .92), "fish": (.58, .20, .95)}[mode]
    img = img.copy(); px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if (h < .11 or h > .97) and s > .45 and v > .35:
                nh, ns, nv = roof[0], s * roof[1] / .7, v * roof[2]
            elif wall and .07 < h < .17 and .12 < s < .5 and v > .6:
                nh, ns, nv = wall[0], wall[1], v * wall[2]
            else:
                continue
            rr, gg, bb = colorsys.hsv_to_rgb(nh, min(1, ns), min(1, nv))
            px[x, y] = (round(rr * 255), round(gg * 255), round(bb * 255), a)
    return img


OUTLINE = (34, 28, 30)


def tidy(img, colors):
    """도트 정리: 기존 캐릭터·Ninja 타일처럼 큰 색면 위주로.
    1) 물체별 색 수 제한  2) 명암 차이가 작은 잔무늬(기와선·나뭇결·잔점)는 주변 다수색으로 흡수,
       대비가 큰 창·문·외곽선은 유지  3) 바깥 외곽선 한 색 통일."""
    w, h = img.size
    alpha = [p[3] > 0 for p in img.getdata()]
    rgb = Image.new("RGB", (w, h), (0, 0, 0))
    rgb.paste(img.convert("RGB"), mask=img.split()[3])
    q = rgb.quantize(colors=colors, method=Image.MEDIANCUT)
    pal = q.getpalette()
    lum = [0.3 * pal[i * 3] + 0.59 * pal[i * 3 + 1] + 0.11 * pal[i * 3 + 2] for i in range(len(pal) // 3)]
    idx = list(q.getdata())
    # 비슷한 색은 더 많이 쓰인 색으로 합친다 (지붕·벽이 '기본색 + 그림자 한 단계'가 되게)
    count = Counter(i for i, a in zip(idx, alpha) if a)
    order = [c for c, _ in count.most_common()]
    remap = {}
    for k, c in enumerate(order):
        for big in order[:k]:
            big = remap.get(big, big)
            d = sum((pal[c * 3 + t] - pal[big * 3 + t]) ** 2 for t in range(3)) ** .5
            if d < 34:
                remap[c] = big
                break
    idx = [remap.get(i, i) for i in idx]
    for _ in range(3):
        nxt = idx[:]
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                i = y * w + x
                if not alpha[i]:
                    continue
                around = [y2 * w + x2 for y2 in (y - 1, y, y + 1) for x2 in (x - 1, x, x + 1)]
                if not all(alpha[j] for j in around):
                    continue
                top, n = Counter(idx[j] for j in around).most_common(1)[0]
                if top != idx[i] and n >= 4 and abs(lum[top] - lum[idx[i]]) < 46:
                    nxt[i] = top
        idx = nxt
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    o = out.load()
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if not alpha[i]:
                continue
            edge = any(not (0 <= x2 < w and 0 <= y2 < h) or not alpha[y2 * w + x2]
                       for x2, y2 in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
            o[x, y] = (OUTLINE if edge else tuple(pal[idx[i] * 3: idx[i] * 3 + 3])) + (255,)
    return out


def pixelate(crop, tw, th, block=None):
    """비율 유지로 tw×th 픽셀 안에 맞춘다(block이 있으면 원본 block px = 게임 1px). 색 수를 줄인 뒤 칸마다 최빈색."""
    sw, sh = crop.size
    s = min(tw / sw, th / sh) if block is None else min(1 / block, tw / sw, th / sh)
    ow, oh = max(1, round(sw * s)), max(1, round(sh * s))
    alpha = crop.split()[3]
    pal = crop.convert("RGB").quantize(colors=28, method=Image.MEDIANCUT)
    colors = pal.getpalette()
    idx, a = pal.load(), alpha.load()
    out = Image.new("RGBA", (ow, oh), (0, 0, 0, 0))
    o = out.load()
    for y in range(oh):
        for x in range(ow):
            x0, x1 = int(x * sw / ow), max(int(x * sw / ow) + 1, int((x + 1) * sw / ow))
            y0, y1 = int(y * sh / oh), max(int(y * sh / oh) + 1, int((y + 1) * sh / oh))
            # 칸 가장자리의 섞인 색을 피하려고 가운데 부분만 본다
            mx, my = (x1 - x0) // 5, (y1 - y0) // 5
            votes, opaque, total = Counter(), 0, 0
            for yy in range(y0 + my, y1 - my):
                for xx in range(x0 + mx, x1 - mx):
                    total += 1
                    if a[xx, yy] > 128:
                        opaque += 1
                        votes[idx[xx, yy]] += 1
            if total and opaque * 2 > total:
                c = votes.most_common(1)[0][0]
                o[x, y] = tuple(colors[c * 3:c * 3 + 3]) + (255,)
    return out


def grid_cells():
    """GRIDS를 (이름, 파일, 원본 상자, 1, 1) 목록으로 펼친다. 칸 경계선이 섞이지 않게 안쪽만 쓴다."""
    out = []
    for prefix, fname, cols, rows, use in GRIDS:
        size = Image.open(os.path.join(SRC, fname)).size
        cw, ch = size[0] / cols, size[1] / rows
        i = 0
        for r in use:
            for c in range(cols):
                ix, iy = cw * .03, ch * .03
                box = (round(c * cw + ix), round(r * ch + iy), round((c + 1) * cw - ix), round((r + 1) * ch - iy))
                out.append((f"{prefix}_{i}", fname, box, 1, 1)); i += 1
    return out


def main():
    placed, cx, cy, row_h = {}, 0, 0, 0
    items = OBJECTS + grid_cells()
    for name, _, _, w, h, *_ in items:
        if cx + w > ATLAS_W:
            cx, cy, row_h = 0, cy + row_h, 0
        placed[name] = (cx, cy, w, h)
        cx += w; row_h = max(row_h, h)
    atlas = Image.new("RGBA", (ATLAS_W * 16, (cy + row_h) * 16), (0, 0, 0, 0))
    cache = {}
    cleaned = os.path.join(SRC, "cleaned")     # 정리한 개별 스프라이트 (원본 옆, 저장소 밖)
    os.makedirs(cleaned, exist_ok=True)
    for name, fname, box, w, h, *rest in items:
        if fname not in cache:
            cache[fname] = Image.open(os.path.join(SRC, fname)).convert("RGBA")
        img = pixelate(cache[fname].crop(box), w * 16, h * 16, rest[0] if rest else None)
        if len(rest) > 1:
            img = recolor(img, rest[1])
        if not name.startswith(("m_", "g_", "n_")):   # 새 마을 시트(n_)는 이미 굵은 도트라 색 정리를 하지 않는다 (포인트색 보존)          # 바닥 무늬는 그대로, 건물 12색·소품 8색으로 정리
            img = tidy(img, (16 if w * h >= 9 else 12) if name.startswith("n_") else (10 if w * h >= 9 else 7))   # 새 시트는 포인트색(파란 문·유리)을 살린다
        x, y, _, _ = placed[name]
        # 칸 상자 안에서 아래·가운데 정렬
        atlas.alpha_composite(img, (x * 16 + (w * 16 - img.width) // 2, y * 16 + h * 16 - img.height))
        img.save(os.path.join(cleaned, name + ".png"))
    atlas.save(os.path.join(HERE, "venice_atlas.png"))
    with open(os.path.join(HERE, "venice_atlas.json"), "w", encoding="utf-8") as fp:
        json.dump({k: list(v) for k, v in placed.items()}, fp, ensure_ascii=False, indent=1)
    print(len(placed), "objects ->", atlas.size)


if __name__ == "__main__":
    main()
