# 零号人设 Danbooru 参考榜单报告

> 生成时间：2026-07-05T22:31:49+08:00  
> 数据口径：Danbooru 公开 tag / post 元数据；本报告不下载、不嵌入、不复刻图片，只用于人设 Owner 的参考拆解。

## 1. 结论摘要

- 零号应以 `美少女吸引力` 为第一层，以 `doll_joints`、`android`、`cracked_skin`、`glowing_chest` 等词做局部语义叠加。
- `robot_girl`、`robot_joints`、`mechanical_arms` 适合做检索池，不适合在母图 prompt 前排使用；它们会把方向推向硬科幻或重武装。
- Danbooru 总角色榜被 Vocaloid / 东方 / Fate / 老牌手游 IP 淹没，适合看“可传播符号”，不适合直接决定零号设定。
- `doll_joints 1girl rating:g` 与 `android 1girl rating:g` 是当前最适合作为零号参考检索入口的两组关键词。

## 2. 分析方法说明

- `关键词体量` 来自 Danbooru `tags.json` 的全站 tag 投稿量，用来判断某个标签是不是常见视觉语言；它不是零号的定稿方向。
- `总角色投稿榜` 来自 Danbooru character tag 投稿量排序，用来观察二创市场里哪些角色符号传播稳定；它会被老 IP 和超大同人圈影响。
- `主题搜索组` 使用 `1girl rating:g` 作为硬约束，避免男性角色、多人图和高风险内容干扰；每组再叠加 `doll_joints`、`android`、`robot_girl` 等主题词。
- `可参考角色候选榜` 不是按单个角色总投稿量排序，而是统计主题搜索组样本中的角色 tag 共现、命中组数量和样本均分；它回答的是“哪些已有角色经常出现在这些语义附近”。
- 候选榜会二次过滤：已知男性、玩家头像、怪物、宠物、机体、非少女对象会被排除；睡衣、一设、特殊形态、机器人 / 人类形态等变体会合并回同一 canonical 角色。
- 未定稿阶段不把 `blue_eyes`、`white_hair`、`white_dress`、`frills` 等强外观词放入默认关键词体量；这些应等零号方向确定后再作为追加检索。

## 3. 关键词体量

| Tag | 中文语义 | 投稿量 | P3 转译 |
| --- | --- | --- | --- |
| [expressionless](https://danbooru.donmai.us/posts?tags=expressionless) | 无表情 | 174054 | 用于零号 normal / weak 基线，避免做成冷酷无情。 |
| [bandages](https://danbooru.donmai.us/posts?tags=bandages) | 绷带 | 126480 | 用于工坊修复、初见照看，不要堆成医疗病号服。 |
| [doll](https://danbooru.donmai.us/posts?tags=doll) | 人偶 | 56033 | 参考检索词 |
| [android](https://danbooru.donmai.us/posts?tags=android) | 仿生人 / 人造人 | 31419 | 保留人工生命和情感觉醒，不默认加入硬科幻材质。 |
| [joints](https://danbooru.donmai.us/posts?tags=joints) | 关节 | 27906 | 参考检索词 |
| [mechanical_arms](https://danbooru.donmai.us/posts?tags=mechanical_arms) | 机械臂 | 23775 | 只做局部或差分参考，不能主导母图。 |
| [mechanical_halo](https://danbooru.donmai.us/posts?tags=mechanical_halo) | 机械光环 | 21605 | 可转译为核心仓启动环、背后小型环形机构或修复光圈。 |
| [doll_joints](https://danbooru.donmai.us/posts?tags=doll_joints) | 人偶关节 | 13771 | 优先用于肩、肘、膝、髋的细小球形关节；关节可见但不破坏美少女第一印象。 |
| [robot_joints](https://danbooru.donmai.us/posts?tags=robot_joints) | 机器人关节 | 13104 | 可查资料但不要放太前，容易把零号推成硬机械。 |
| [robot_girl](https://danbooru.donmai.us/posts?tags=robot_girl) | 机器人少女 | 7858 | 作为参考检索词，不作为母图 prompt 主轴。 |
| [cracked_skin](https://danbooru.donmai.us/posts?tags=cracked_skin) | 裂纹皮肤 | 6970 | 用于初见破损、低 SAN 或 hurt，不覆盖全身。 |
| [blank_stare](https://danbooru.donmai.us/posts?tags=blank_stare) | 空洞凝视 | 2388 | 只用于低 SAN 或刚苏醒，需配合柔软眼神。 |

## 4. 总角色投稿榜截面

| Rank | Character Tag | 中文角色名 | 投稿量 | 可借鉴点 |
| --- | --- | --- | --- | --- |
| 1 | [hatsune_miku](https://danbooru.donmai.us/posts?tags=hatsune_miku) | 初音未来 | 140337 | 强剪影、强发色、符号极简。 |
| 2 | [hakurei_reimu](https://danbooru.donmai.us/posts?tags=hakurei_reimu) | 博丽灵梦 | 97169 | 红白配色和稳定身份符号。 |
| 3 | [kirisame_marisa](https://danbooru.donmai.us/posts?tags=kirisame_marisa) | 雾雨魔理沙 | 84761 | 看符号传播方式，不直接参考造型。 |
| 4 | [flandre_scarlet](https://danbooru.donmai.us/posts?tags=flandre_scarlet) | 芙兰朵露·斯卡雷特 | 61260 | 可爱与危险反差。 |
| 5 | [remilia_scarlet](https://danbooru.donmai.us/posts?tags=remilia_scarlet) | 蕾米莉亚·斯卡雷特 | 60760 | 贵族感、小体型与强符号。 |
| 6 | [izayoi_sakuya](https://danbooru.donmai.us/posts?tags=izayoi_sakuya) | 十六夜咲夜 | 52287 | 女仆、银发、优雅冷静。 |
| 7 | [artoria_pendragon_(fate)](https://danbooru.donmai.us/posts?tags=artoria_pendragon_%28fate%29) | 阿尔托莉雅·潘德拉贡 | 45553 | 看符号传播方式，不直接参考造型。 |
| 8 | [komeiji_koishi](https://danbooru.donmai.us/posts?tags=komeiji_koishi) | 古明地恋 | 43896 | 看符号传播方式，不直接参考造型。 |
| 9 | [kochiya_sanae](https://danbooru.donmai.us/posts?tags=kochiya_sanae) | 东风谷早苗 | 40416 | 看符号传播方式，不直接参考造型。 |
| 10 | [konpaku_youmu](https://danbooru.donmai.us/posts?tags=konpaku_youmu) | 魂魄妖梦 | 40158 | 看符号传播方式，不直接参考造型。 |
| 11 | [cirno](https://danbooru.donmai.us/posts?tags=cirno) | 琪露诺 | 39894 | 看符号传播方式，不直接参考造型。 |
| 12 | [alice_margatroid](https://danbooru.donmai.us/posts?tags=alice_margatroid) | 爱丽丝·玛格特罗依德 | 39222 | 人偶 / 魔法少女语义，值得零号参考。 |
| 13 | [admiral_(kancolle)](https://danbooru.donmai.us/posts?tags=admiral_%28kancolle%29) | 未确认 | 38903 | 看符号传播方式，不直接参考造型。 |
| 14 | [patchouli_knowledge](https://danbooru.donmai.us/posts?tags=patchouli_knowledge) | 帕秋莉·诺蕾姬 | 38097 | 看符号传播方式，不直接参考造型。 |
| 15 | [yakumo_yukari](https://danbooru.donmai.us/posts?tags=yakumo_yukari) | 八云紫 | 37603 | 看符号传播方式，不直接参考造型。 |
| 16 | [sensei_(blue_archive)](https://danbooru.donmai.us/posts?tags=sensei_%28blue_archive%29) | 未确认 | 36821 | 看符号传播方式，不直接参考造型。 |
| 17 | [shameimaru_aya](https://danbooru.donmai.us/posts?tags=shameimaru_aya) | 射命丸文 | 33628 | 看符号传播方式，不直接参考造型。 |
| 18 | [reisen_udongein_inaba](https://danbooru.donmai.us/posts?tags=reisen_udongein_inaba) | 铃仙·优昙华院·因幡 | 30650 | 看符号传播方式，不直接参考造型。 |
| 19 | [komeiji_satori](https://danbooru.donmai.us/posts?tags=komeiji_satori) | 古明地觉 | 30424 | 看符号传播方式，不直接参考造型。 |
| 20 | [fujiwara_no_mokou](https://danbooru.donmai.us/posts?tags=fujiwara_no_mokou) | 藤原妹红 | 30183 | 看符号传播方式，不直接参考造型。 |
| 21 | [akemi_homura](https://danbooru.donmai.us/posts?tags=akemi_homura) | 晓美焰 | 29411 | 沉默、病弱、保护欲和悲伤感。 |
| 22 | [kaname_madoka](https://danbooru.donmai.us/posts?tags=kaname_madoka) | 鹿目圆 | 29301 | 柔软、善意、魔法少女核心。 |
| 23 | [hong_meiling](https://danbooru.donmai.us/posts?tags=hong_meiling) | 红美铃 | 27761 | 看符号传播方式，不直接参考造型。 |
| 24 | [saigyouji_yuyuko](https://danbooru.donmai.us/posts?tags=saigyouji_yuyuko) | 西行寺幽幽子 | 27552 | 看符号传播方式，不直接参考造型。 |
| 25 | [inubashiri_momiji](https://danbooru.donmai.us/posts?tags=inubashiri_momiji) | 未确认 | 25597 | 看符号传播方式，不直接参考造型。 |

## 5. 主题搜索组

| 组 | 搜索词 | 总量 | 样本 | 用途 |
| --- | --- | --- | --- | --- |
| doll_joints_girl | [doll_joints 1girl rating:g](https://danbooru.donmai.us/posts?tags=doll_joints%201girl%20rating%3Ag) | 3795 | 60 | 人偶关节 + 单人美少女；零号最核心的非人语义参考。 |
| android_girl | [android 1girl rating:g](https://danbooru.donmai.us/posts?tags=android%201girl%20rating%3Ag) | 6241 | 60 | 人工生命 + 少女人设；用于情绪觉醒和非人身份参考。 |
| robot_girl | [robot_girl 1girl rating:g](https://danbooru.donmai.us/posts?tags=robot_girl%201girl%20rating%3Ag) | 2849 | 60 | 机械少女热度池；只取局部机械语言，避免硬科幻主体。 |
| joints_girl | [joints 1girl rating:g](https://danbooru.donmai.us/posts?tags=joints%201girl%20rating%3Ag) | 7310 | 60 | 可见关节总池；辅助判断关节表现的常见角色。 |
| mechanical_halo_girl | [mechanical_halo 1girl rating:g](https://danbooru.donmai.us/posts?tags=mechanical_halo%201girl%20rating%3Ag) | 3527 | 60 | 机械光环 / 头部符号；可转译为核心仓光环或背部小装置。 |
| mechanical_arms_girl | [mechanical_arms 1girl rating:g](https://danbooru.donmai.us/posts?tags=mechanical_arms%201girl%20rating%3Ag) | 4467 | 60 | 机械肢体池；只作为战损、维修、义体差分参考。 |

## 6. 可参考角色候选榜

| 角色 Tag | 中文角色名 | 合并来源 | 样本命中 | 均分 | 命中组 | 主要作品 Tag | 中文作品名 | 零号参考方式 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [shion_(overwatch)](https://danbooru.donmai.us/posts?tags=shion_%28overwatch%29) | Shion（未确认通用译名） | - | 35 | 13.03 | android_girl, joints_girl, robot_girl | overwatch | 守望先锋 | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [cecilia_immergreen](https://danbooru.donmai.us/posts?tags=cecilia_immergreen) | 塞西莉亚·伊默格林 | cecilia_immergreen_(1st_costume) | 29 | 16.48 | doll_joints_girl, joints_girl | hololive, hololive_english, 2026_fifa_world_cup | hololive, hololive English, 2026_fifa_world_cup | 参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。 |
| [noa_(blue_archive)](https://danbooru.donmai.us/posts?tags=noa_%28blue_archive%29) | 生盐诺亚 | noa_(pajamas)_(blue_archive) | 30 | 13.13 | mechanical_halo_girl | blue_archive, meitantei_precure!, precure | 蔚蓝档案, meitantei_precure!, precure | 可转译为核心仓光环、背部环形装置或启动特效。 |
| [yuuka_(blue_archive)](https://danbooru.donmai.us/posts?tags=yuuka_%28blue_archive%29) | 早濑优香 | yuuka_(pajamas)_(blue_archive), yuuka_(track)_(blue_archive) | 27 | 13.93 | mechanical_halo_girl | blue_archive | 蔚蓝档案 | 可转译为核心仓光环、背部环形装置或启动特效。 |
| [aria_(zenless_zone_zero)](https://danbooru.donmai.us/posts?tags=aria_%28zenless_zone_zero%29) | Aria | aria_(robot)_(zenless_zone_zero), aria_(human)_(zenless_zone_zero) | 21 | 8.76 | joints_girl, robot_girl | zenless_zone_zero | 绝区零 | 近期 robot_girl 热点；只看局部机械符号和辨识点。 |
| [adachi_rei](https://danbooru.donmai.us/posts?tags=adachi_rei) | 足立零 | - | 13 | 2.38 | android_girl, joints_girl, mechanical_arms_girl, robot_girl | a.i._voice, utau | A.I.VOICE, UTAU | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [aigis_(persona)](https://danbooru.donmai.us/posts?tags=aigis_%28persona%29) | 埃癸斯 | - | 11 | 5.36 | android_girl, joints_girl, robot_girl | persona, persona_3, persona_3_reload | 女神异闻录, 女神异闻录3, 女神异闻录3 Reload | 人工生命 + 可见关节的经典参考；学习非人身份如何不压过少女感。 |
| [roll_(mega_man)](https://danbooru.donmai.us/posts?tags=roll_%28mega_man%29) | 萝露 | - | 8 | 4.62 | android_girl, joints_girl | mega_man_(classic), mega_man_(series), capcom | 洛克人经典系列, 洛克人系列, 卡普空 | 可爱优先的机器人少女；适合校准零号不要过硬。 |
| [katie_(ogami_kazuki)](https://danbooru.donmai.us/posts?tags=katie_%28ogami_kazuki%29) | 未确认 | - | 8 | 3.0 | doll_joints_girl, joints_girl | original | 原创 | 参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。 |
| [faust_(project_moon)](https://danbooru.donmai.us/posts?tags=faust_%28project_moon%29) | 浮士德 | - | 6 | 11.33 | joints_girl, mechanical_arms_girl, robot_girl | limbus_company, project_moon | 边狱公司, Project Moon | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [paimon_(genshin_impact)](https://danbooru.donmai.us/posts?tags=paimon_%28genshin_impact%29) | 派蒙 | - | 9 | 1.56 | mechanical_halo_girl | genshin_impact | 原神 | 可转译为核心仓光环、背部环形装置或启动特效。 |
| [tiki_(fire_emblem)](https://danbooru.donmai.us/posts?tags=tiki_%28fire_emblem%29) | 未确认 | tiki_(young)_(fire_emblem), tiki_(young)_(lucid_heir)_(fire_emblem) | 6 | 12.0 | joints_girl, robot_girl | fire_emblem, fire_emblem:_mystery_of_the_emblem, fire_emblem_shadows | fire_emblem, fire_emblem:_mystery_of_the_emblem, fire_emblem_shadows | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [raiden_shogun](https://danbooru.donmai.us/posts?tags=raiden_shogun) | 雷电将军 | raiden_shogun_(magatsu_mitake_narukami_no_mikoto) | 5 | 9.0 | android_girl, doll_joints_girl, mechanical_arms_girl | genshin_impact | 原神 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [pharaekh_bahiti_of_the_paskareth_dynasty](https://danbooru.donmai.us/posts?tags=pharaekh_bahiti_of_the_paskareth_dynasty) | 未确认 | - | 6 | 7.67 | robot_girl | original, warhammer_40k | 原创, 战锤40K | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [ranni_the_witch](https://danbooru.donmai.us/posts?tags=ranni_the_witch) | 魔女菈妮 | - | 5 | 6.8 | doll_joints_girl, joints_girl | elden_ring | 艾尔登法环 | 冷感人偶氛围参考；只取沉静和关节，不取暗黑主体。 |
| [elster_(signalis)](https://danbooru.donmai.us/posts?tags=elster_%28signalis%29) | 艾尔斯特 | - | 4 | 8.5 | android_girl, joints_girl, robot_girl | signalis | SIGNALIS | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [malenia_blade_of_miquella](https://danbooru.donmai.us/posts?tags=malenia_blade_of_miquella) | 米凯拉的锋刃玛莲妮亚 | - | 4 | 8.25 | mechanical_arms_girl | elden_ring | 艾尔登法环 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [plain_doll](https://danbooru.donmai.us/posts?tags=plain_doll) | 人偶 | - | 4 | 2.5 | doll_joints_girl, joints_girl | bloodborne | 血源诅咒 | 冷感人偶氛围参考；只取沉静和关节，不取暗黑主体。 |
| [android_girl_aki](https://danbooru.donmai.us/posts?tags=android_girl_aki) | 未确认 | - | 4 | 1.0 | android_girl, robot_girl | original, aibo_(sony) | 原创, aibo_(sony) | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [lloyd_(granblue_fantasy)](https://danbooru.donmai.us/posts?tags=lloyd_%28granblue_fantasy%29) | 未确认 | - | 3 | 2.0 | doll_joints_girl, joints_girl, mechanical_arms_girl | shadowverse, shingeki_no_bahamut | 影之诗, 巴哈姆特之怒 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [orchis](https://danbooru.donmai.us/posts?tags=orchis) | 未确认 | - | 3 | 2.0 | doll_joints_girl, joints_girl, mechanical_arms_girl | shadowverse, shingeki_no_bahamut | 影之诗, 巴哈姆特之怒 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [splash_woman](https://danbooru.donmai.us/posts?tags=splash_woman) | 未确认 | - | 3 | 4.33 | android_girl, robot_girl | mega_man_(classic), mega_man_(series), mega_man_9 | 洛克人经典系列, 洛克人系列, mega_man_9 | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [elizabeth_rose_bloodflame](https://danbooru.donmai.us/posts?tags=elizabeth_rose_bloodflame) | 未确认 | - | 2 | 16.0 | doll_joints_girl, joints_girl | hololive, hololive_english | hololive, hololive English | 参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。 |
| [gigi_murin](https://danbooru.donmai.us/posts?tags=gigi_murin) | 吉吉·穆林 | - | 2 | 16.0 | doll_joints_girl, joints_girl | hololive, hololive_english | hololive, hololive English | 参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。 |
| [raora_panthera](https://danbooru.donmai.us/posts?tags=raora_panthera) | 未确认 | - | 2 | 16.0 | doll_joints_girl, joints_girl | hololive, hololive_english | hololive, hololive English | 参考关节位置和人偶感；脸和服装仍需重做为 P3 原创。 |
| [umika_(blue_archive)](https://danbooru.donmai.us/posts?tags=umika_%28blue_archive%29) | 未确认 | - | 3 | 6.0 | mechanical_halo_girl | blue_archive | 蔚蓝档案 | 可转译为核心仓光环、背部环形装置或启动特效。 |
| [m4_sopmod_ii_(girls'_frontline)](https://danbooru.donmai.us/posts?tags=m4_sopmod_ii_%28girls%27_frontline%29) | 未确认 | m4_sopmod_ii_(mod3)_(girls'_frontline) | 2 | 16.0 | mechanical_arms_girl | girls'_frontline | 少女前线 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [strength_(black_rock_shooter)](https://danbooru.donmai.us/posts?tags=strength_%28black_rock_shooter%29) | Strength | - | 3 | 4.33 | mechanical_arms_girl | black_rock_shooter | 黑岩射手 | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [hatsune_miku](https://danbooru.donmai.us/posts?tags=hatsune_miku) | 初音未来 | - | 2 | 7.5 | doll_joints_girl, mechanical_arms_girl | vocaloid | VOCALOID | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [albina_(project_moon)](https://danbooru.donmai.us/posts?tags=albina_%28project_moon%29) | 未确认 | - | 2 | 11.0 | robot_girl | limbus_company, project_moon | 边狱公司, Project Moon | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [ren_(project_moon)](https://danbooru.donmai.us/posts?tags=ren_%28project_moon%29) | 未确认 | - | 2 | 11.0 | robot_girl | limbus_company, project_moon | 边狱公司, Project Moon | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [talia_yang](https://danbooru.donmai.us/posts?tags=talia_yang) | 未确认 | - | 2 | 10.5 | mechanical_arms_girl | cyberpunk:_edgerunners, cyberpunk_(series), boruto:_naruto_next_generations | cyberpunk:_edgerunners, cyberpunk_(series), boruto:_naruto_next_generations | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |
| [durin_(genshin_impact)](https://danbooru.donmai.us/posts?tags=durin_%28genshin_impact%29) | 未确认 | - | 4 | 0.5 | mechanical_halo_girl | genshin_impact | 原神 | 可转译为核心仓光环、背部环形装置或启动特效。 |
| [pearl_(honkai:_star_rail)](https://danbooru.donmai.us/posts?tags=pearl_%28honkai%3A_star_rail%29) | 未确认 | - | 2 | 6.0 | android_girl, joints_girl | honkai:_star_rail, honkai_(series) | 崩坏：星穹铁道, 崩坏系列 | 参考人工生命身份，不直接搬运服装或轮廓。 |
| [kusanagi_motoko](https://danbooru.donmai.us/posts?tags=kusanagi_motoko) | 未确认 | - | 2 | 9.0 | mechanical_arms_girl | ghost_in_the_shell | ghost_in_the_shell | 只用于义体 / 战损 / 维修差分，不能成为零号母图主体。 |

## 7. 作品共现榜

| 作品 / 系列 Tag | 中文作品名 | 样本命中 | 参考价值 |
| --- | --- | --- | --- |
| original | 原创 | 58 | 原创样本多，适合观察标签常见组合。 |
| blue_archive | 蔚蓝档案 | 50 | 光环、学院感、现代二次元脸型参考；零号不走学院服。 |
| overwatch | 守望先锋 | 36 | 只作为共现热度参考。 |
| elden_ring | 艾尔登法环 | 25 | 冷感人偶和破损氛围参考，需降暗黑。 |
| hololive | hololive | 22 | 只作为共现热度参考。 |
| hololive_english | hololive English | 21 | 只作为共现热度参考。 |
| genshin_impact | 原神 | 16 | 只作为共现热度参考。 |
| mega_man_(series) | 洛克人系列 | 16 | 只作为共现热度参考。 |
| elden_ring_nightreign | 艾尔登法环：黑夜君临 | 14 | 只作为共现热度参考。 |
| utau | UTAU | 14 | 只作为共现热度参考。 |
| a.i._voice | A.I.VOICE | 13 | 只作为共现热度参考。 |
| mega_man_(classic) | 洛克人经典系列 | 12 | 只作为共现热度参考。 |
| persona | 女神异闻录 | 11 | Aigis 方向可参考 android 少女。 |
| persona_3 | 女神异闻录3 | 11 | Aigis 方向可参考 android 少女。 |
| zenless_zone_zero | 绝区零 | 11 | 近期 robot girl 热点，可看机械符号与潮流脸型。 |
| limbus_company | 边狱公司 | 10 | 只作为共现热度参考。 |
| project_moon | Project Moon | 10 | 只作为共现热度参考。 |
| warhammer_40k | 战锤40K | 6 | 只作为共现热度参考。 |
| persona_3_reload | 女神异闻录3 Reload | 5 | 只作为共现热度参考。 |
| 2001_a_space_odyssey | 未确认 | 5 | 只作为共现热度参考。 |
| indie_virtual_youtuber | 未确认 | 4 | 只作为共现热度参考。 |
| bloodborne | 血源诅咒 | 4 | 冷感人偶参考，谨慎使用暗黑气质。 |
| signalis | SIGNALIS | 4 | 只作为共现热度参考。 |
| capcom | 卡普空 | 4 | 只作为共现热度参考。 |
| shadowverse | 影之诗 | 3 | 只作为共现热度参考。 |

## 8. 高价值 Prompt 关键词

| 用途 | 关键词 |
| --- | --- |
| 母图核心 | `1girl`, `solo`, `doll`, `doll_joints`, `android`, `fragile anime girl`, `expressionless`, `sad eyes` |
| 零号识别点 | `small glowing core`, `glowing_chest`, `cracked_skin`, `bandages`, `subtle repair marks`；发色、瞳色、服装款式暂不预设 |
| 局部机械 | `subtle doll joints`, `visible shoulder joints`, `visible elbow joints`, `mechanical spine detail`, `small mechanical halo` |
| 状态差分 | `blank_stare`, `low_san`, `hurt`, `relaxed`, `repair_react`, `messy_hair`, `weak sitting pose` |

## 9. 禁用 / 慎用关键词

| Tag | 原因 |
| --- | --- |
| babydoll | Danbooru 语义偏睡衣 / 性感服饰，不适合作为零号人设关键词。 |
| sex_doll | 不适合项目方向，禁止进入 prompt。 |
| robot_sex | 不适合项目方向，禁止进入 prompt。 |
| robot_joints | 可查资料但不要放太前，容易把零号推成硬机械。 |
| mechanical_arms | 只做局部或差分参考，不能主导母图。 |
| mechanical_legs | 只做局部或差分参考，避免战斗义体主角化。 |

## 10. 对零号 V1 的执行建议

1. 母图首轮不要写 `robot_girl` 前排；建议写成 `fragile anime girl, living doll, subtle doll joints, android, small glowing core`。
2. 人偶关节只在肩、肘、膝、髋做清晰但细小的断面，不要全身金属骨架化。
3. 核心仓放在胸口或锁骨下方，做成可启动、可熄灭、可低 SAN 闪烁的主识别点。
4. 发色、瞳色和服装款式本轮不预设；后续应先做 2-3 个方向稿，再决定是否使用白发、蓝眼、白裙、斗篷或褶边等强识别元素。
5. 表情集优先做 `normal/weak`、`blank_stare_low_san`、`hurt`、`relaxed_after_repair`，这些比战斗动作更重要。

## 11. 生成证据

- Raw JSON：`美术文档/_generated/danbooru_character_reference/zero_doll_reference_raw.json`
- 数据来源：Danbooru `tags.json`、`counts/posts.json`、`posts.json` 公开接口。
- 限制：Danbooru 采样受时间、评分过滤、同人热度和 tag 标注习惯影响；本报告只作为参考池，不作为抄袭依据或最终设定。
