# -*- coding: utf-8 -*-
"""Rewrite CookingTables.xlsx to match ScriptableObject docs."""
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill
from pathlib import Path

wb = Workbook()
HEADER_FILL = PatternFill("solid", fgColor="D9EAD3")


def sheet(name, headers, rows):
    if name in wb.sheetnames:
        ws = wb[name]
    elif wb.active.title == "Sheet":
        ws = wb.active
        ws.title = name
    else:
        ws = wb.create_sheet(name)

    ws.append(headers)
    for cell in ws[1]:
        cell.font = Font(bold=True)
        cell.fill = HEADER_FILL
    for row in rows:
        ws.append(list(row))

    for col in ws.columns:
        width = 12
        for cell in col:
            if cell.value is not None:
                width = max(width, min(48, len(str(cell.value)) + 2))
        ws.column_dimensions[col[0].column_letter].width = width
    return ws


readme = wb.active
readme.title = "说明"
readme["A1"] = "CookingTables 填写说明（对齐 ScriptableObject 文档）"
readme["A1"].font = Font(bold=True, size=14)
lines = [
    "",
    "导入菜单：Cooking / Import Cooking Tables",
    "数据 Sheet 第一行必须是英文表头；不要在表头上方插说明行。",
    "",
    "【Seasonings】调味料 → Data/Seasonings/",
    "  id：建好不要改，如 season_salt",
    "  displayName：界面显示名，如 盐",
    "  icon：图标路径，可空",
    "",
    "【Recipes】食谱头信息 → Data/Recipes/（一份资产一道菜）",
    "  id / displayName / unlockDay / finishedSprite（可空）",
    "  第7天火锅：RecipeIngredients 与 RecipeSeasonings 可不写该 recipeId",
    "",
    "【RecipeIngredients】成菜要求",
    "  recipeId, ingredientId, shape, cookMethod, count",
    "  shape：Whole / Dice / Slice / Shred",
    "  cookMethod：Raw / Boiled / Fried / BoiledAndFried",
    "  count 空或≤0 时按 1",
    "",
    "【RecipeSeasonings】调味顺序（行序=玩家必须倒的顺序）",
    "  recipeId, seasoningId, targetIngredientId, count",
    "",
    "【RecipeSteps】日记步骤——功能未接入，暂时不用写",
    "  action：Cut / Boil / Fry / Season / Plate",
    "",
    "【Ingredients】原食材 → Data/Ingredients/",
    "  id 建好不要改；spriteWhole / baseColor 可空（空则保留已有图）",
    "",
    "【IngredientVisualStates】可选，状态图标；不写则保留资产现有列表",
]
for i, line in enumerate(lines, start=2):
    readme[f"A{i}"] = line
readme.column_dimensions["A"].width = 88

sheet(
    "Seasonings",
    ["id", "displayName", "icon"],
    [
        ("season_oil", "油", ""),
        ("season_salt", "盐", ""),
        ("season_pepper", "胡椒", ""),
    ],
)

sheet(
    "Recipes",
    ["id", "displayName", "unlockDay", "finishedSprite"],
    [
        ("recipe_day1", "什锦蘑菇浓汤", 1, ""),
    ],
)

sheet(
    "RecipeIngredients",
    ["recipeId", "ingredientId", "shape", "cookMethod", "count"],
    [
        ("recipe_day1", "ing_mushroom", "Slice", "BoiledAndFried", 1),
        ("recipe_day1", "ing_onion", "Slice", "BoiledAndFried", 1),
        ("recipe_day1", "ing_potato", "Slice", "Boiled", 1),
    ],
)

sheet(
    "RecipeSeasonings",
    ["recipeId", "seasoningId", "targetIngredientId", "count"],
    [
        ("recipe_day1", "season_oil", "ing_mushroom", 1),
        ("recipe_day1", "season_salt", "ing_mushroom", 1),
        ("recipe_day1", "season_pepper", "ing_mushroom", 1),
        ("recipe_day1", "season_oil", "ing_onion", 1),
        ("recipe_day1", "season_salt", "ing_onion", 1),
        ("recipe_day1", "season_pepper", "ing_onion", 1),
        ("recipe_day1", "season_salt", "ing_potato", 1),
        ("recipe_day1", "season_pepper", "ing_potato", 1),
    ],
)

sheet(
    "RecipeSteps",
    ["recipeId", "order", "hintText", "action", "ingredientId", "cookMethod", "seasoningId"],
    [],
)

ingredients = [
    ("ing_beef", "虹彩牛肉", "散发着彩虹光彩的牛肉，最美味的精华都浓缩在此", "Meat", False, True, 90, 160, "", "#FFFFFF"),
    ("ing_berry", "蜜莓果", "味道甘甜的果实，一口气吃太多小心牙疼。", "Vegetable", True, True, 70, 140, "", "#FFFFFF"),
    ("ing_cornfish", "玉米鱼", "通体黄色的有鳞鱼，肉质鲜甜，难怪被比作玉米。", "Meat", False, True, 110, 180, "", "#FFFFFF"),
    ("ing_cucumber", "黄瓜", "爽口的瓜类植物，为所有人带来清新香气的冲击。", "Vegetable", True, True, 60, 130, "", "#FFFFFF"),
    ("ing_doneegg", "熟鸡蛋", "白水煮熟的鸡蛋，蛋白包裹着饱满的蛋黄，完美！", "Meat", True, True, 100, 120, "", "#FFFFFF"),
    ("ing_egg", "鸡蛋", "不知道是哪种鸡下的蛋，看起来十分普通，希望味道也是。", "Meat", True, False, 100, 170, "", "#FFFFFF"),
    ("ing_mushroom", "白蘑菇", "生长在森林中的菌类，形似夜空中皎白的明月。", "Vegetable", False, True, 120, 180, "Assets/_Project/Arts/AI/Mushroom.png", "#FFFFFF"),
    ("ing_onion", "洋葱", "生长在森林中的野洋葱，味道辛甜。", "Vegetable", True, True, 110, 170, "", "#FFFFFF"),
    ("ing_potato", "星土豆", "外形和普通土豆差不多，切开后内部有星状的花纹。", "Vegetable", False, True, 130, 190, "", "#FFFFFF"),
    ("ing_pumpkin", "幽灵南瓜", "长有脸状花纹的白色南瓜，究竟是天生还是亡灵的执念呢？没有人知道答案。", "Vegetable", False, True, 125, 185, "", "#FFFFFF"),
    ("ing_rice", "大米", "一种形似米的谷物，味道也很接近，不如就当成米吧。", "Other", False, False, 140, 200, "", "#FFFFFF"),
    ("ing_shrimp", "红斑虾", "浑身长满斑纹的红褐色虾类，据说斑纹越多越美味。", "Meat", False, True, 95, 155, "", "#FFFFFF"),
    ("ing_tomato", "番茄", "艳红色的果实，微微的酸味让人胃口大开。", "Vegetable", True, True, 80, 150, "", "#FFFFFF"),
    ("ing_yam", "山药", "像长毛的树枝，有人说它是土豆的亲戚，反正我不信。", "Vegetable", False, True, 115, 175, "", "#FFFFFF"),
]
sheet(
    "Ingredients",
    ["id", "displayName", "description", "kind", "canEatRaw", "canCut", "optimalDoneness", "maxDoneness", "spriteWhole", "baseColor"],
    ingredients,
)

sheet(
    "IngredientVisualStates",
    ["ingredientId", "shape", "cookMethod", "sprite"],
    [],
)

order = [
    "说明",
    "Seasonings",
    "Recipes",
    "RecipeIngredients",
    "RecipeSeasonings",
    "RecipeSteps",
    "Ingredients",
    "IngredientVisualStates",
]
for i, name in enumerate(order):
    wb.move_sheet(name, offset=i - wb.sheetnames.index(name))

tables = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/Tables"
out = tables / "CookingTables.xlsx"
try:
    wb.save(out)
except PermissionError:
    out = tables / "CookingTables_new.xlsx"
    wb.save(out)
    print("WARN: CookingTables.xlsx is locked; wrote", out.name, "instead")
print("wrote", out)
print("sheets", wb.sheetnames)

assert [c.value for c in wb["RecipeIngredients"][1]] == [
    "recipeId", "ingredientId", "shape", "cookMethod", "count"
]
assert wb["RecipeIngredients"]["C2"].value == "Slice"
assert wb["RecipeSteps"].max_row == 1
assert wb["Seasonings"]["C1"].value == "icon"
assert wb["Recipes"]["D1"].value == "finishedSprite"
assert wb["Ingredients"]["I1"].value == "spriteWhole"
print("format OK")
