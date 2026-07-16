#!/usr/bin/env python3
"""Fetch Danbooru tag metadata for character-design reference reports.

The script intentionally does not download or embed images. It collects public
tag counts and safe-rated post metadata, then writes a Markdown research report
plus raw JSON evidence for art-direction review.
"""

from __future__ import annotations

import argparse
import datetime as _dt
import json
import math
import time
import urllib.parse
import urllib.request
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any


DANBOORU_ROOT = "https://danbooru.donmai.us"
USER_AGENT = "ProjectP3CharacterReferenceResearch/1.0"
DEFAULT_TIMEOUT_SECONDS = 18

DEFAULT_TAG_KEYWORDS = [
    "doll",
    "doll_joints",
    "joints",
    "android",
    "robot_girl",
    "humanoid_robot",
    "robot_joints",
    "mechanical_halo",
    "mechanical_arms",
    "single_mechanical_arm",
    "mechanical_legs",
    "mechanical_eyes",
    "mechanical_spine",
    "cracked_skin",
    "bandages",
    "expressionless",
    "blank_stare",
    "sad",
    "sleepy",
]

FAST_TAG_KEYWORDS = [
    "doll",
    "doll_joints",
    "joints",
    "android",
    "robot_girl",
    "robot_joints",
    "mechanical_halo",
    "mechanical_arms",
    "cracked_skin",
    "expressionless",
    "blank_stare",
    "bandages",
]

DEFAULT_SEARCH_GROUPS = [
    {
        "id": "doll_joints_girl",
        "tags": "doll_joints 1girl rating:g",
        "intent": "人偶关节 + 单人美少女；零号最核心的非人语义参考。",
    },
    {
        "id": "android_girl",
        "tags": "android 1girl rating:g",
        "intent": "人工生命 + 少女人设；用于情绪觉醒和非人身份参考。",
    },
    {
        "id": "robot_girl",
        "tags": "robot_girl 1girl rating:g",
        "intent": "机械少女热度池；只取局部机械语言，避免硬科幻主体。",
    },
    {
        "id": "joints_girl",
        "tags": "joints 1girl rating:g",
        "intent": "可见关节总池；辅助判断关节表现的常见角色。",
    },
    {
        "id": "mechanical_halo_girl",
        "tags": "mechanical_halo 1girl rating:g",
        "intent": "机械光环 / 头部符号；可转译为核心仓光环或背部小装置。",
    },
    {
        "id": "mechanical_arms_girl",
        "tags": "mechanical_arms 1girl rating:g",
        "intent": "机械肢体池；只作为战损、维修、义体差分参考。",
    },
    {
        "id": "fragile_doll",
        "tags": "doll_joints expressionless 1girl rating:g",
        "intent": "无表情人偶感；用于 normal / low_san 表情基线。",
    },
    {
        "id": "damaged_doll",
        "tags": "doll_joints cracked_skin 1girl rating:g",
        "intent": "裂纹与损伤；用于零号初见和 hurt 差分。",
    },
]

FAST_SEARCH_GROUPS = DEFAULT_SEARCH_GROUPS[:6]

EXCLUDED_CHARACTER_SUBSTRINGS = [
    "(cosplay)",
    "(meme)",
    "sensei_",
    "admiral_",
    "commander_",
    "producer_",
    "master_chief",
    "billy_kid",
    "boothill",
    "anakin_skywalker",
    "finn_the_human",
    "hal_9000",
]

EXCLUDED_CHARACTER_TAGS = {
    # Male / player-avatar / non-target human tags that can leak through 1girl
    # searches because they appear in multi-character metadata or variants.
    "aether_(genshin_impact)",
    "scaramouche_(genshin_impact)",
    "wanderer_(genshin_impact)",
    "yuuki_makoto_(persona_3)",
    "hans_christian_andersen_(fate)",
    "proto_man",
    "mega_man_(character)",
    "zero_(mega_man)",
    "zero(z)_(mega_man)",
    "x_(mega_man)",
    "dr._doppler's_assistant",
    "connor_(detroit)",
    "sekiro",
    "boothill_(honkai:_star_rail)",
    # Mascots, monsters, robots, pets, or non-human objects that are not useful
    # as bishoujo character-design references for Zero.
    "revenant_(elden_ring)",
    "tango_(mega_man)",
    "miraidon",
    "pyro_jack",
    "messengers_(bloodborne)",
    "gquuuuuux",
    "chattino_(raora_panthera)",
    "otomo_(cecilia_immergreen)",
    "ototori_(cecilia_immergreen)",
    "grem_(gigi_murin)",
    "rosarian_(elizabeth_rose_bloodflame)",
    "ohr_(blue_archive)",
}

CHARACTER_ALIASES = {
    "cecilia_immergreen_(1st_costume)": "cecilia_immergreen",
    "noa_(pajamas)_(blue_archive)": "noa_(blue_archive)",
    "yuuka_(pajamas)_(blue_archive)": "yuuka_(blue_archive)",
    "yuuka_(track)_(blue_archive)": "yuuka_(blue_archive)",
    "aria_(robot)_(zenless_zone_zero)": "aria_(zenless_zone_zero)",
    "aria_(human)_(zenless_zone_zero)": "aria_(zenless_zone_zero)",
    "raiden_shogun_(magatsu_mitake_narukami_no_mikoto)": "raiden_shogun",
    "tiki_(young)_(fire_emblem)": "tiki_(fire_emblem)",
    "tiki_(young)_(lucid_heir)_(fire_emblem)": "tiki_(fire_emblem)",
    "alpha:_inverse_crown_(pgr)": "alpha_(pgr)",
    "alpha:_inverse_crown_(nightshroud_coronet)_(pgr)": "alpha_(pgr)",
    "m4_sopmod_ii_(mod3)_(girls'_frontline)": "m4_sopmod_ii_(girls'_frontline)",
}

RISKY_OR_LOW_VALUE_TAGS = {
    "babydoll": "Danbooru 语义偏睡衣 / 性感服饰，不适合作为零号人设关键词。",
    "sex_doll": "不适合项目方向，禁止进入 prompt。",
    "robot_sex": "不适合项目方向，禁止进入 prompt。",
    "robot_joints": "可查资料但不要放太前，容易把零号推成硬机械。",
    "mechanical_arms": "只做局部或差分参考，不能主导母图。",
    "mechanical_legs": "只做局部或差分参考，避免战斗义体主角化。",
}

TAG_TRANSLATION_NOTES = {
    "doll_joints": "优先用于肩、肘、膝、髋的细小球形关节；关节可见但不破坏美少女第一印象。",
    "android": "保留人工生命和情感觉醒，不默认加入硬科幻材质。",
    "robot_girl": "作为参考检索词，不作为母图 prompt 主轴。",
    "mechanical_halo": "可转译为核心仓启动环、背后小型环形机构或修复光圈。",
    "cracked_skin": "用于初见破损、低 SAN 或 hurt，不覆盖全身。",
    "expressionless": "用于零号 normal / weak 基线，避免做成冷酷无情。",
    "blank_stare": "只用于低 SAN 或刚苏醒，需配合柔软眼神。",
    "white_hair": "可增强脆弱、人工生命和主角识别度。",
    "blue_eyes": "适合作为核心光与情绪焦点。",
    "bandages": "用于工坊修复、初见照看，不要堆成医疗病号服。",
}

TAG_CN_NAMES = {
    "doll": "人偶",
    "doll_joints": "人偶关节",
    "joints": "关节",
    "android": "仿生人 / 人造人",
    "robot_girl": "机器人少女",
    "humanoid_robot": "人形机器人",
    "robot_joints": "机器人关节",
    "mechanical_halo": "机械光环",
    "mechanical_arms": "机械臂",
    "single_mechanical_arm": "单机械臂",
    "mechanical_legs": "机械腿",
    "mechanical_eyes": "机械眼",
    "mechanical_spine": "机械脊柱",
    "cracked_skin": "裂纹皮肤",
    "bandages": "绷带",
    "expressionless": "无表情",
    "blank_stare": "空洞凝视",
    "sad": "悲伤",
    "sleepy": "困倦",
    "messy_hair": "凌乱头发",
    "white_hair": "白发",
    "blue_eyes": "蓝眼",
    "cloak": "斗篷",
    "white_dress": "白裙",
    "frills": "褶边",
}

CHARACTER_CN_NAMES = {
    "hatsune_miku": "初音未来",
    "hakurei_reimu": "博丽灵梦",
    "kirisame_marisa": "雾雨魔理沙",
    "flandre_scarlet": "芙兰朵露·斯卡雷特",
    "remilia_scarlet": "蕾米莉亚·斯卡雷特",
    "izayoi_sakuya": "十六夜咲夜",
    "artoria_pendragon_(fate)": "阿尔托莉雅·潘德拉贡",
    "komeiji_koishi": "古明地恋",
    "kochiya_sanae": "东风谷早苗",
    "konpaku_youmu": "魂魄妖梦",
    "cirno": "琪露诺",
    "alice_margatroid": "爱丽丝·玛格特罗依德",
    "patchouli_knowledge": "帕秋莉·诺蕾姬",
    "yakumo_yukari": "八云紫",
    "shameimaru_aya": "射命丸文",
    "reisen_udongein_inaba": "铃仙·优昙华院·因幡",
    "komeiji_satori": "古明地觉",
    "fujiwara_no_mokou": "藤原妹红",
    "akemi_homura": "晓美焰",
    "kaname_madoka": "鹿目圆",
    "hong_meiling": "红美铃",
    "saigyouji_yuyuko": "西行寺幽幽子",
    "kagamine_rin": "镜音铃",
    "raiden_shogun": "雷电将军",
    "lumine_(genshin_impact)": "荧",
    "saber_(fate)": "Saber",
    "mash_kyrielight": "玛修·基列莱特",
    "aigis_(persona)": "埃癸斯",
    "roll_(mega_man)": "萝露",
    "herta_(honkai:_star_rail)": "黑塔",
    "herta_(puppet)_(honkai:_star_rail)": "黑塔人偶",
    "ranni_the_witch": "魔女菈妮",
    "plain_doll": "人偶",
    "cecilia_immergreen": "塞西莉亚·伊默格林",
    "cecilia_immergreen_(1st_costume)": "塞西莉亚·伊默格林（一设）",
    "otomo_(cecilia_immergreen)": "奥托莫",
    "ototori_(cecilia_immergreen)": "奥托托里",
    "gigi_murin": "吉吉·穆林",
    "noa_(blue_archive)": "生盐诺亚",
    "noa_(pajamas)_(blue_archive)": "生盐诺亚（睡衣）",
    "yuuka_(blue_archive)": "早濑优香",
    "yuuka_(pajamas)_(blue_archive)": "早濑优香（睡衣）",
    "yuuka_(track)_(blue_archive)": "早濑优香（体操服）",
    "adachi_rei": "足立零",
    "scaramouche_(genshin_impact)": "散兵",
    "wanderer_(genshin_impact)": "流浪者",
    "paimon_(genshin_impact)": "派蒙",
    "aria_(robot)_(zenless_zone_zero)": "Aria（机器人）",
    "aria_(zenless_zone_zero)": "Aria",
    "aria_(human)_(zenless_zone_zero)": "Aria（人类形态）",
    "shion_(overwatch)": "Shion（未确认通用译名）",
    "elster_(signalis)": "艾尔斯特",
    "malenia_blade_of_miquella": "米凯拉的锋刃玛莲妮亚",
    "snow_white_(nikke)": "白雪公主",
    "alpha_(pgr)": "露西亚·深红之渊 / Alpha",
    "faust_(project_moon)": "浮士德",
    "ump45_(girls'_frontline)": "UMP45",
    "tari_(meta_runner)": "Tari",
    "strength_(black_rock_shooter)": "Strength",
}

COPYRIGHT_CN_NAMES = {
    "original": "原创",
    "touhou": "东方Project",
    "vocaloid": "VOCALOID",
    "kantai_collection": "舰队Collection",
    "fate_(series)": "Fate 系列",
    "fate/grand_order": "Fate/Grand Order",
    "blue_archive": "蔚蓝档案",
    "genshin_impact": "原神",
    "honkai:_star_rail": "崩坏：星穹铁道",
    "honkai_(series)": "崩坏系列",
    "zenless_zone_zero": "绝区零",
    "arknights": "明日方舟",
    "girls'_frontline": "少女前线",
    "girls'_frontline_2:_exilium": "少女前线2：追放",
    "punishing:_gray_raven": "战双帕弥什",
    "persona": "女神异闻录",
    "persona_3": "女神异闻录3",
    "persona_3_reload": "女神异闻录3 Reload",
    "mega_man_(series)": "洛克人系列",
    "mega_man_(classic)": "洛克人经典系列",
    "mega_man_zero_(series)": "洛克人Zero系列",
    "nier_(series)": "尼尔系列",
    "nier:automata": "尼尔：机械纪元",
    "rozen_maiden": "蔷薇少女",
    "mahou_shoujo_madoka_magica": "魔法少女小圆",
    "mahou_shoujo_madoka_magica_(anime)": "魔法少女小圆（动画）",
    "elden_ring": "艾尔登法环",
    "elden_ring_nightreign": "艾尔登法环：黑夜君临",
    "bloodborne": "血源诅咒",
    "hololive": "hololive",
    "hololive_english": "hololive English",
    "overwatch": "守望先锋",
    "signalis": "SIGNALIS",
    "limbus_company": "边狱公司",
    "project_moon": "Project Moon",
    "black_rock_shooter": "黑岩射手",
    "goddess_of_victory:_nikke": "胜利女神：NIKKE",
    "warhammer_40k": "战锤40K",
    "utau": "UTAU",
    "a.i._voice": "A.I.VOICE",
    "capcom": "卡普空",
    "shadowverse": "影之诗",
    "shingeki_no_bahamut": "巴哈姆特之怒",
    "meta_runner": "Meta Runner",
}


def request_json(path: str, params: dict[str, Any], delay: float, timeout: int = DEFAULT_TIMEOUT_SECONDS) -> Any:
    query = urllib.parse.urlencode(params)
    url = f"{DANBOORU_ROOT}{path}?{query}"
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    if delay > 0:
        time.sleep(delay)
    return data


def tag_search_url(tags: str) -> str:
    return f"{DANBOORU_ROOT}/posts?tags={urllib.parse.quote(tags)}"


def fetch_tag_exact(name: str, delay: float) -> dict[str, Any] | None:
    rows = request_json("/tags.json", {"search[name]": name, "limit": 1}, delay)
    if not rows:
        return None
    row = rows[0]
    return {
        "name": row.get("name", name),
        "post_count": int(row.get("post_count") or 0),
        "category": int(row.get("category") or 0),
        "url": f"{DANBOORU_ROOT}/posts?tags={urllib.parse.quote(row.get('name', name))}",
    }


def fetch_top_character_tags(limit: int, delay: float) -> list[dict[str, Any]]:
    rows = request_json(
        "/tags.json",
        {
            "search[category]": 4,
            "search[hide_empty]": "yes",
            "search[order]": "count",
            "limit": limit,
        },
        delay,
    )
    return [
        {
            "rank": i + 1,
            "name": row.get("name"),
            "post_count": int(row.get("post_count") or 0),
            "url": f"{DANBOORU_ROOT}/posts?tags={urllib.parse.quote(row.get('name', ''))}",
        }
        for i, row in enumerate(rows)
    ]


def fetch_post_count(tags: str, delay: float) -> int:
    data = request_json("/counts/posts.json", {"tags": tags}, delay)
    return int((data.get("counts") or {}).get("posts") or 0)


def fetch_posts_for_group(tags: str, pages: int, limit: int, delay: float) -> list[dict[str, Any]]:
    posts: list[dict[str, Any]] = []
    for page in range(1, pages + 1):
        rows = request_json(
            "/posts.json",
            {
                "tags": tags,
                "limit": limit,
                "page": page,
                "only": "id,score,rating,tag_string_character,tag_string_copyright,tag_string_general",
            },
            delay,
        )
        if not rows:
            break
        posts.extend(rows)
    return posts


def split_tags(value: str | None) -> list[str]:
    if not value:
        return []
    return [tag for tag in value.split(" ") if tag]


def is_excluded_character(tag: str) -> bool:
    low = tag.lower()
    return tag in EXCLUDED_CHARACTER_TAGS or any(piece in low for piece in EXCLUDED_CHARACTER_SUBSTRINGS)


def canonical_character_tag(tag: str) -> str:
    return CHARACTER_ALIASES.get(tag, tag)


def summarize_search_groups(groups: list[dict[str, str]], pages: int, limit: int, delay: float) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    group_summaries: list[dict[str, Any]] = []
    character_hits: dict[str, dict[str, Any]] = defaultdict(
        lambda: {
            "sample_count": 0,
            "score_sum": 0,
            "groups": set(),
            "aliases": Counter(),
            "copyrights": Counter(),
            "general_tags": Counter(),
        }
    )
    copyright_hits: Counter[str] = Counter()
    general_hits: Counter[str] = Counter()

    for group_index, group in enumerate(groups, start=1):
        print(f"[{group_index}/{len(groups)}] post group: {group['id']} :: {group['tags']}", flush=True)
        try:
            count = fetch_post_count(group["tags"], delay)
            posts = fetch_posts_for_group(group["tags"], pages, limit, delay)
            error = None
        except Exception as exc:
            count = 0
            posts = []
            error = str(exc)
        char_counter: Counter[str] = Counter()
        copy_counter: Counter[str] = Counter()
        general_counter: Counter[str] = Counter()

        for post in posts:
            score = int(post.get("score") or 0)
            characters = split_tags(post.get("tag_string_character"))
            copyrights = split_tags(post.get("tag_string_copyright"))
            generals = split_tags(post.get("tag_string_general"))

            for copyright_tag in copyrights:
                copy_counter[copyright_tag] += 1
                copyright_hits[copyright_tag] += 1

            for general_tag in generals:
                general_counter[general_tag] += 1
                general_hits[general_tag] += 1

            for character in characters:
                if is_excluded_character(character):
                    continue
                canonical = canonical_character_tag(character)
                char_counter[canonical] += 1
                hit = character_hits[canonical]
                hit["sample_count"] += 1
                hit["score_sum"] += score
                hit["groups"].add(group["id"])
                hit["aliases"].update([character])
                hit["copyrights"].update(copyrights)
                hit["general_tags"].update(generals)

        group_summaries.append(
            {
                "id": group["id"],
                "tags": group["tags"],
                "intent": group["intent"],
                "total_posts": count,
                "sample_posts": len(posts),
                "error": error,
                "search_url": tag_search_url(group["tags"]),
                "top_characters": counter_to_rows(char_counter, 20),
                "top_copyrights": counter_to_rows(copy_counter, 12),
                "top_general_tags": counter_to_rows(general_counter, 16),
            }
        )

    aggregate_characters = []
    for tag, hit in character_hits.items():
        sample_count = int(hit["sample_count"])
        groups_hit = sorted(hit["groups"])
        avg_score = hit["score_sum"] / sample_count if sample_count else 0
        relevance = sample_count * (1 + 0.2 * max(0, len(groups_hit) - 1)) + math.log1p(max(0, avg_score))
        aggregate_characters.append(
            {
                "tag": tag,
                "sample_count": sample_count,
                "avg_score": round(avg_score, 2),
                "groups": groups_hit,
                "copyrights": counter_to_rows(hit["copyrights"], 5),
                "general_tags": counter_to_rows(hit["general_tags"], 8),
                "aliases": counter_to_rows(hit["aliases"], 8),
                "relevance": round(relevance, 2),
                "url": f"{DANBOORU_ROOT}/posts?tags={urllib.parse.quote(tag)}",
            }
        )
    aggregate_characters.sort(key=lambda row: (row["relevance"], row["sample_count"], row["avg_score"]), reverse=True)

    aggregate = {
        "top_characters": aggregate_characters,
        "top_copyrights": counter_to_rows(copyright_hits, 30),
        "top_general_tags": counter_to_rows(general_hits, 40),
    }
    return group_summaries, aggregate


def counter_to_rows(counter: Counter[str], limit: int) -> list[dict[str, Any]]:
    return [{"tag": key, "count": value} for key, value in counter.most_common(limit)]


def enrich_chinese_names(data: dict[str, Any]) -> None:
    for row in data.get("tag_counts", []):
        row["cn_name"] = cn_tag_name(row.get("name", ""))
    for row in data.get("top_character_tags", []):
        row["cn_name"] = cn_character_name(row.get("name", ""))
    aggregate = data.get("aggregate", {})
    for row in aggregate.get("top_characters", []):
        row["cn_name"] = cn_character_name(row.get("tag", ""))
        row["copyright_cn_names"] = [
            cn_copyright_name(item.get("tag", ""))
            for item in row.get("copyrights", [])
        ]
    for row in aggregate.get("top_copyrights", []):
        row["cn_name"] = cn_copyright_name(row.get("tag", ""))
    for group in data.get("search_groups", []):
        for row in group.get("top_characters", []):
            row["cn_name"] = cn_character_name(row.get("tag", ""))
        for row in group.get("top_copyrights", []):
            row["cn_name"] = cn_copyright_name(row.get("tag", ""))
        for row in group.get("top_general_tags", []):
            row["cn_name"] = cn_tag_name(row.get("tag", ""))


def markdown_link(label: str, url: str) -> str:
    return f"[{label}]({url})"


def rows_table(headers: list[str], rows: list[list[Any]]) -> str:
    lines = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join("---" for _ in headers) + " |",
    ]
    for row in rows:
        lines.append("| " + " | ".join(str(value) for value in row) + " |")
    return "\n".join(lines)


def cn_tag_name(tag: str) -> str:
    return TAG_CN_NAMES.get(tag, "未确认")


def cn_character_name(tag: str) -> str:
    return CHARACTER_CN_NAMES.get(tag, "未确认")


def cn_copyright_name(tag: str) -> str:
    return COPYRIGHT_CN_NAMES.get(tag, "未确认")


def format_copyright_cn_names(rows: list[dict[str, Any]]) -> str:
    names = []
    for row in rows:
        tag = row["tag"]
        cn_name = cn_copyright_name(tag)
        names.append(cn_name if cn_name != "未确认" else tag)
    return ", ".join(names)


def render_report(data: dict[str, Any]) -> str:
    generated_at = data["generated_at"]
    lines: list[str] = []
    lines.extend(
        [
            "# 零号人设 Danbooru 参考榜单报告",
            "",
            f"> 生成时间：{generated_at}  ",
            "> 数据口径：Danbooru 公开 tag / post 元数据；本报告不下载、不嵌入、不复刻图片，只用于人设 Owner 的参考拆解。",
            "",
            "## 1. 结论摘要",
            "",
            "- 零号应以 `美少女吸引力` 为第一层，以 `doll_joints`、`android`、`cracked_skin`、`glowing_chest` 等词做局部语义叠加。",
            "- `robot_girl`、`robot_joints`、`mechanical_arms` 适合做检索池，不适合在母图 prompt 前排使用；它们会把方向推向硬科幻或重武装。",
            "- Danbooru 总角色榜被 Vocaloid / 东方 / Fate / 老牌手游 IP 淹没，适合看“可传播符号”，不适合直接决定零号设定。",
            "- `doll_joints 1girl rating:g` 与 `android 1girl rating:g` 是当前最适合作为零号参考检索入口的两组关键词。",
            "",
            "## 2. 分析方法说明",
            "",
            "- `关键词体量` 来自 Danbooru `tags.json` 的全站 tag 投稿量，用来判断某个标签是不是常见视觉语言；它不是零号的定稿方向。",
            "- `总角色投稿榜` 来自 Danbooru character tag 投稿量排序，用来观察二创市场里哪些角色符号传播稳定；它会被老 IP 和超大同人圈影响。",
            "- `主题搜索组` 使用 `1girl rating:g` 作为硬约束，避免男性角色、多人图和高风险内容干扰；每组再叠加 `doll_joints`、`android`、`robot_girl` 等主题词。",
            "- `可参考角色候选榜` 不是按单个角色总投稿量排序，而是统计主题搜索组样本中的角色 tag 共现、命中组数量和样本均分；它回答的是“哪些已有角色经常出现在这些语义附近”。",
            "- 候选榜会二次过滤：已知男性、玩家头像、怪物、宠物、机体、非少女对象会被排除；睡衣、一设、特殊形态、机器人 / 人类形态等变体会合并回同一 canonical 角色。",
            "- 未定稿阶段不把 `blue_eyes`、`white_hair`、`white_dress`、`frills` 等强外观词放入默认关键词体量；这些应等零号方向确定后再作为追加检索。",
            "",
            "## 3. 关键词体量",
            "",
        ]
    )

    tag_rows = []
    for row in data["tag_counts"]:
        note = TAG_TRANSLATION_NOTES.get(row["name"], RISKY_OR_LOW_VALUE_TAGS.get(row["name"], "参考检索词"))
        tag_rows.append([markdown_link(row["name"], row["url"]), cn_tag_name(row["name"]), row["post_count"], note])
    lines.append(rows_table(["Tag", "中文语义", "投稿量", "P3 转译"], tag_rows))

    lines.extend(["", "## 4. 总角色投稿榜截面", ""])
    top_rows = [
        [row["rank"], markdown_link(row["name"], row["url"]), cn_character_name(row["name"]), row["post_count"], top_character_note(row["name"])]
        for row in data["top_character_tags"][:25]
    ]
    lines.append(rows_table(["Rank", "Character Tag", "中文角色名", "投稿量", "可借鉴点"], top_rows))

    lines.extend(["", "## 5. 主题搜索组", ""])
    group_rows = []
    for group in data["search_groups"]:
        group_rows.append(
            [
                group["id"],
                markdown_link(group["tags"], group["search_url"]),
                group["total_posts"],
                group["sample_posts"],
                group["intent"],
            ]
        )
    lines.append(rows_table(["组", "搜索词", "总量", "样本", "用途"], group_rows))

    lines.extend(["", "## 6. 可参考角色候选榜", ""])
    candidate_rows = []
    for row in data["aggregate"]["top_characters"][:35]:
        copyrights = ", ".join(item["tag"] for item in row["copyrights"][:3])
        copyright_names = format_copyright_cn_names(row["copyrights"][:3])
        aliases = ", ".join(item["tag"] for item in row.get("aliases", []) if item["tag"] != row["tag"])
        if not aliases:
            aliases = "-"
        groups = ", ".join(row["groups"])
        candidate_rows.append(
            [
                markdown_link(row["tag"], row["url"]),
                cn_character_name(row["tag"]),
                aliases,
                row["sample_count"],
                row["avg_score"],
                groups,
                copyrights,
                copyright_names,
                character_translation_note(row),
            ]
        )
    lines.append(rows_table(["角色 Tag", "中文角色名", "合并来源", "样本命中", "均分", "命中组", "主要作品 Tag", "中文作品名", "零号参考方式"], candidate_rows))

    lines.extend(["", "## 7. 作品共现榜", ""])
    copyright_rows = [
        [item["tag"], cn_copyright_name(item["tag"]), item["count"], copyright_translation_note(item["tag"])]
        for item in data["aggregate"]["top_copyrights"][:25]
    ]
    lines.append(rows_table(["作品 / 系列 Tag", "中文作品名", "样本命中", "参考价值"], copyright_rows))

    lines.extend(["", "## 8. 高价值 Prompt 关键词", ""])
    lines.append(
        rows_table(
            ["用途", "关键词"],
            [
                ["母图核心", "`1girl`, `solo`, `doll`, `doll_joints`, `android`, `fragile anime girl`, `expressionless`, `sad eyes`"],
                ["零号识别点", "`small glowing core`, `glowing_chest`, `cracked_skin`, `bandages`, `subtle repair marks`；发色、瞳色、服装款式暂不预设"],
                ["局部机械", "`subtle doll joints`, `visible shoulder joints`, `visible elbow joints`, `mechanical spine detail`, `small mechanical halo`"],
                ["状态差分", "`blank_stare`, `low_san`, `hurt`, `relaxed`, `repair_react`, `messy_hair`, `weak sitting pose`"],
            ],
        )
    )

    lines.extend(["", "## 9. 禁用 / 慎用关键词", ""])
    risky_rows = [[tag, reason] for tag, reason in RISKY_OR_LOW_VALUE_TAGS.items()]
    lines.append(rows_table(["Tag", "原因"], risky_rows))

    lines.extend(
        [
            "",
            "## 10. 对零号 V1 的执行建议",
            "",
            "1. 母图首轮不要写 `robot_girl` 前排；建议写成 `fragile anime girl, living doll, subtle doll joints, android, small glowing core`。",
            "2. 人偶关节只在肩、肘、膝、髋做清晰但细小的断面，不要全身金属骨架化。",
            "3. 核心仓放在胸口或锁骨下方，做成可启动、可熄灭、可低 SAN 闪烁的主识别点。",
            "4. 发色、瞳色和服装款式本轮不预设；后续应先做 2-3 个方向稿，再决定是否使用白发、蓝眼、白裙、斗篷或褶边等强识别元素。",
            "5. 表情集优先做 `normal/weak`、`blank_stare_low_san`、`hurt`、`relaxed_after_repair`，这些比战斗动作更重要。",
            "",
            "## 11. 生成证据",
            "",
            f"- Raw JSON：`{data['raw_json_path']}`",
            "- 数据来源：Danbooru `tags.json`、`counts/posts.json`、`posts.json` 公开接口。",
            "- 限制：Danbooru 采样受时间、评分过滤、同人热度和 tag 标注习惯影响；本报告只作为参考池，不作为抄袭依据或最终设定。",
            "",
        ]
    )
    return "\n".join(lines)


def top_character_note(tag: str) -> str:
    notes = {
        "hatsune_miku": "强剪影、强发色、符号极简。",
        "hakurei_reimu": "红白配色和稳定身份符号。",
        "flandre_scarlet": "可爱与危险反差。",
        "remilia_scarlet": "贵族感、小体型与强符号。",
        "izayoi_sakuya": "女仆、银发、优雅冷静。",
        "alice_margatroid": "人偶 / 魔法少女语义，值得零号参考。",
        "akemi_homura": "沉默、病弱、保护欲和悲伤感。",
        "kaname_madoka": "柔软、善意、魔法少女核心。",
        "mash_kyrielight": "盾、守护、主角陪伴感。",
    }
    return notes.get(tag, "看符号传播方式，不直接参考造型。")


def character_translation_note(row: dict[str, Any]) -> str:
    tag = row["tag"]
    joined_groups = " ".join(row["groups"])
    copyrights = {item["tag"] for item in row["copyrights"]}
    if "aigis" in tag:
        return "人工生命 + 可见关节的经典参考；学习非人身份如何不压过少女感。"
    if "roll_" in tag:
        return "可爱优先的机器人少女；适合校准零号不要过硬。"
    if "herta" in tag:
        return "人偶分身 / puppet 语义；参考精致与非人感。"
    if "ranni" in tag or "plain_doll" in tag:
        return "冷感人偶氛围参考；只取沉静和关节，不取暗黑主体。"
    if "aria_" in tag and "zenless_zone_zero" in copyrights:
        return "近期 robot_girl 热点；只看局部机械符号和辨识点。"
    if "mechanical_arms" in joined_groups:
        return "只用于义体 / 战损 / 维修差分，不能成为零号母图主体。"
    if "mechanical_halo" in joined_groups:
        return "可转译为核心仓光环、背部环形装置或启动特效。"
    if "doll_joints" in joined_groups:
        return "参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。"
    if "android" in joined_groups or "robot_girl" in joined_groups:
        return "参考人工生命身份，不直接搬运服装或轮廓。"
    return "作为二次元角色热度样本，拆解符号，不复刻设计。"


def copyright_translation_note(tag: str) -> str:
    notes = {
        "original": "原创样本多，适合观察标签常见组合。",
        "touhou": "强角色符号与同人传播，不适合直接转造型。",
        "vocaloid": "强发色和符号传播参考。",
        "blue_archive": "光环、学院感、现代二次元脸型参考；零号不走学院服。",
        "persona": "Aigis 方向可参考 android 少女。",
        "persona_3": "Aigis 方向可参考 android 少女。",
        "honkai:_star_rail": "Herta puppet / Clara 机器反差可参考。",
        "zenless_zone_zero": "近期 robot girl 热点，可看机械符号与潮流脸型。",
        "punishing:_gray_raven": "战斗构造体参考，零号不要过战斗化。",
        "girls'_frontline": "战术人形参考，零号不走军武主体。",
        "elden_ring": "冷感人偶和破损氛围参考，需降暗黑。",
        "bloodborne": "冷感人偶参考，谨慎使用暗黑气质。",
    }
    return notes.get(tag, "只作为共现热度参考。")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate a Danbooru character reference report for Project P3.")
    parser.add_argument(
        "--output-dir",
        default="美术文档/_generated/danbooru_character_reference",
        help="Directory for report and raw JSON.",
    )
    parser.add_argument("--report-name", default="zero_doll_reference_report.md")
    parser.add_argument("--raw-name", default="zero_doll_reference_raw.json")
    parser.add_argument("--top-character-limit", type=int, default=40)
    parser.add_argument("--sample-pages", type=int, default=1)
    parser.add_argument("--sample-limit", type=int, default=120)
    parser.add_argument("--delay", type=float, default=0.1)
    parser.add_argument("--fast", action="store_true", help="Use a smaller default keyword/search profile for routine runs.")
    parser.add_argument("--skip-top-characters", action="store_true", help="Skip global character tag ranking.")
    parser.add_argument(
        "--keyword",
        action="append",
        default=[],
        help="Extra exact tag keyword to count. Can be passed multiple times.",
    )
    parser.add_argument(
        "--search",
        action="append",
        default=[],
        help="Extra post search group, e.g. \"doll_joints white_hair rating:g\".",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    raw_path = output_dir / args.raw_name
    report_path = output_dir / args.report_name

    base_keywords = FAST_TAG_KEYWORDS if args.fast else DEFAULT_TAG_KEYWORDS
    keywords = list(dict.fromkeys(base_keywords + args.keyword))
    tag_counts = []
    for index, keyword in enumerate(keywords, start=1):
        print(f"[{index}/{len(keywords)}] tag count: {keyword}", flush=True)
        try:
            row = fetch_tag_exact(keyword, args.delay)
            if row:
                tag_counts.append(row)
        except Exception as exc:
            tag_counts.append(
                {
                    "name": keyword,
                    "post_count": 0,
                    "category": None,
                    "url": f"{DANBOORU_ROOT}/posts?tags={urllib.parse.quote(keyword)}",
                    "error": str(exc),
                }
            )
    tag_counts.sort(key=lambda row: row["post_count"], reverse=True)

    groups = list(FAST_SEARCH_GROUPS if args.fast else DEFAULT_SEARCH_GROUPS)
    for index, search in enumerate(args.search, start=1):
        groups.append({"id": f"extra_{index}", "tags": search, "intent": "用户追加搜索组。"})

    generated_at = _dt.datetime.now().astimezone().isoformat(timespec="seconds")
    data = {
        "generated_at": generated_at,
        "source": "Danbooru public metadata APIs",
        "raw_json_path": str(raw_path).replace("\\", "/"),
        "tag_counts": tag_counts,
    }
    if args.skip_top_characters:
        data["top_character_tags"] = []
        data["top_character_error"] = "skipped"
    else:
        print("Fetching top character tag ranking", flush=True)
        try:
            data["top_character_tags"] = fetch_top_character_tags(args.top_character_limit, args.delay)
        except Exception as exc:
            data["top_character_tags"] = []
            data["top_character_error"] = str(exc)
    print(f"Fetching {len(groups)} search groups", flush=True)
    search_groups, aggregate = summarize_search_groups(groups, args.sample_pages, args.sample_limit, args.delay)
    data["search_groups"] = search_groups
    data["aggregate"] = aggregate
    enrich_chinese_names(data)

    raw_path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    report_path.write_text(render_report(data), encoding="utf-8")
    print(f"Wrote {report_path}")
    print(f"Wrote {raw_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
