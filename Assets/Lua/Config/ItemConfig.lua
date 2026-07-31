-- ItemConfig.lua
-- 物品配置表（假数据），用于验证虚拟滚动和 Tab 切换。
-- 后续接入真实数据后，替换此文件即可，上游 BagPanelCtrl 的业务逻辑不变。
--
-- 字段说明：
--   id       — 物品 ID，唯一
--   name     — 物品名称
--   type     — Equipment / Consumable / Gem
--   quality  — 品质等级：1=普通(白) 2=稀有(蓝) 3=史诗(紫) 4=传说(橙)
--   count    — 当前持有数量
--   stackMax — 最大堆叠数
local ItemConfig = {
    -- ════════════════════════════════════════
    -- 装备（Equipment）：20 件
    -- ════════════════════════════════════════
    [1001] = { id=1001, name="铁剑",         type="Equipment", quality=1, count=1,  stackMax=1 },
    [1002] = { id=1002, name="青铜甲",       type="Equipment", quality=1, count=1,  stackMax=1 },
    [1003] = { id=1003, name="皮靴",         type="Equipment", quality=1, count=1,  stackMax=1 },
    [1004] = { id=1004, name="铁盔",         type="Equipment", quality=1, count=1,  stackMax=1 },
    [1005] = { id=1005, name="钢盾",         type="Equipment", quality=2, count=1,  stackMax=1 },
    [1006] = { id=1006, name="精铁长剑",     type="Equipment", quality=2, count=1,  stackMax=1 },
    [1007] = { id=1007, name="锁子甲",       type="Equipment", quality=2, count=1,  stackMax=1 },
    [1008] = { id=1008, name="银骑士盔",     type="Equipment", quality=2, count=1,  stackMax=1 },
    [1009] = { id=1009, name="火焰之剑",     type="Equipment", quality=3, count=1,  stackMax=1 },
    [1010] = { id=1010, name="龙鳞甲",       type="Equipment", quality=3, count=1,  stackMax=1 },
    [1011] = { id=1011, name="疾风靴",       type="Equipment", quality=3, count=1,  stackMax=1 },
    [1012] = { id=1012, name="魔法护符",     type="Equipment", quality=3, count=1,  stackMax=1 },
    [1013] = { id=1013, name="霜噬之刃",     type="Equipment", quality=3, count=1,  stackMax=1 },
    [1014] = { id=1014, name="圣光之盾",     type="Equipment", quality=3, count=1,  stackMax=1 },
    [1015] = { id=1015, name="影袭匕首",     type="Equipment", quality=3, count=1,  stackMax=1 },
    [1016] = { id=1016, name="龙骨战锤",     type="Equipment", quality=4, count=1,  stackMax=1 },
    [1017] = { id=1017, name="天界法袍",     type="Equipment", quality=4, count=1,  stackMax=1 },
    [1018] = { id=1018, name="深渊之冠",     type="Equipment", quality=4, count=1,  stackMax=1 },
    [1019] = { id=1019, name="凤凰之翼",     type="Equipment", quality=4, count=1,  stackMax=1 },
    [1020] = { id=1020, name="混沌之刃",     type="Equipment", quality=4, count=1,  stackMax=1 },

    -- ════════════════════════════════════════
    -- 消耗品（Consumable）：24 组
    -- ════════════════════════════════════════
    [2001] = { id=2001, name="生命药水",     type="Consumable", quality=1, count=20, stackMax=99 },
    [2002] = { id=2002, name="法力药水",     type="Consumable", quality=1, count=15, stackMax=99 },
    [2003] = { id=2003, name="解毒剂",       type="Consumable", quality=1, count=8,  stackMax=99 },
    [2004] = { id=2004, name="火焰卷轴",     type="Consumable", quality=1, count=5,  stackMax=20 },
    [2005] = { id=2005, name="冰霜卷轴",     type="Consumable", quality=1, count=3,  stackMax=20 },
    [2006] = { id=2006, name="传送卷轴",     type="Consumable", quality=2, count=2,  stackMax=10 },
    [2007] = { id=2007, name="大回复药水",   type="Consumable", quality=2, count=10, stackMax=50 },
    [2008] = { id=2008, name="大法力药水",   type="Consumable", quality=2, count=8,  stackMax=50 },
    [2009] = { id=2009, name="隐身药水",     type="Consumable", quality=2, count=3,  stackMax=10 },
    [2010] = { id=2010, name="疾跑药水",     type="Consumable", quality=2, count=5,  stackMax=10 },
    [2011] = { id=2011, name="力量药剂",     type="Consumable", quality=3, count=4,  stackMax=10 },
    [2012] = { id=2012, name="防御药剂",     type="Consumable", quality=3, count=4,  stackMax=10 },
    [2013] = { id=2013, name="敏捷药剂",     type="Consumable", quality=3, count=4,  stackMax=10 },
    [2014] = { id=2014, name="经验药水",     type="Consumable", quality=3, count=6,  stackMax=10 },
    [2015] = { id=2015, name="复活十字章",   type="Consumable", quality=3, count=1,  stackMax=5 },
    [2016] = { id=2016, name="狂暴药剂",     type="Consumable", quality=4, count=2,  stackMax=5 },
    [2017] = { id=2017, name="神圣祝福",     type="Consumable", quality=4, count=1,  stackMax=3 },
    [2018] = { id=2018, name="魔力增幅",     type="Consumable", quality=4, count=3,  stackMax=5 },
    [2019] = { id=2019, name="钢铁意志",     type="Consumable", quality=4, count=2,  stackMax=5 },
    [2020] = { id=2020, name="龙息药水",     type="Consumable", quality=4, count=1,  stackMax=3 },

    -- ════════════════════════════════════════
    -- 宝石（Gem）：15 种
    -- ════════════════════════════════════════
    [3001] = { id=3001, name="红宝石",       type="Gem", quality=2, count=12, stackMax=99 },
    [3002] = { id=3002, name="蓝宝石",       type="Gem", quality=2, count=8,  stackMax=99 },
    [3003] = { id=3003, name="绿宝石",       type="Gem", quality=2, count=10, stackMax=99 },
    [3004] = { id=3004, name="黄水晶",       type="Gem", quality=2, count=6,  stackMax=99 },
    [3005] = { id=3005, name="紫水晶",       type="Gem", quality=2, count=7,  stackMax=99 },
    [3006] = { id=3006, name="攻击红宝石",   type="Gem", quality=3, count=4,  stackMax=99 },
    [3007] = { id=3007, name="防御蓝宝石",   type="Gem", quality=3, count=3,  stackMax=99 },
    [3008] = { id=3008, name="生命绿宝石",   type="Gem", quality=3, count=5,  stackMax=99 },
    [3009] = { id=3009, name="暴击紫水晶",   type="Gem", quality=3, count=2,  stackMax=99 },
    [3010] = { id=3010, name="迅捷黄玉",     type="Gem", quality=3, count=3,  stackMax=99 },
    [3011] = { id=3011, name="龙炎红宝石",   type="Gem", quality=4, count=1,  stackMax=10 },
    [3012] = { id=3012, name="冰霜蓝宝石",   type="Gem", quality=4, count=1,  stackMax=10 },
    [3013] = { id=3013, name="自然翡翠",     type="Gem", quality=4, count=2,  stackMax=10 },
    [3014] = { id=3014, name="暗影紫晶",     type="Gem", quality=4, count=1,  stackMax=10 },
    [3015] = { id=3015, name="神圣钻石",     type="Gem", quality=4, count=1,  stackMax=10 },
}

return ItemConfig
