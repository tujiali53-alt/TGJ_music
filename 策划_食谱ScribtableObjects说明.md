Project 窗口右键 Create → Cooking 新建Scribtable。

**原食材**： `Data/Ingredients/` 

**调味料**： `Assets/_Project/Data/Seasonings/`，
**食谱：** `Assets/_Project/Data/Recipes/`。

`DishPresentation` 全游戏只建一份。



## SeasoningDefinition：调味料数据

| 字段          | 填写                                     |
| ------------- | ---------------------------------------- |
| `id`          | 建好就不要改。eg：盐填写： `season_salt` |
| `displayName` | Game视图展示名字，eg： `盐`              |
| `icon`        | 该物品图标。图还没有可以空着             |

## RecipeDefinition：食谱

一份资产是一道菜。第 1 天可以解锁 `recipe_day1`。

| 字段             | 代表什么                         | 备注                   |
| ---------------- | -------------------------------- | ---------------------- |
| `id`             | 程序字段id                       | `recipe_day1`          |
| `displayName`    | 菜名                             | `什锦蘑菇浓汤`         |
| `unlockDay`      | 第几天解锁                       | `1`                    |
| `finishedSprite` | 做对时，总结界面那一张成品图     | 可以空着，默认显示白色 |
| `ingredients`    | 盘子上必须有的原食材             | 见下面三行             |
| `seasonings`     | 每份食材必须按什么顺序加哪些调料 | 见下面                 |
| `steps`          | 日记里的句子和下锅顺序           | 现在不参与             |

`ingredients` 里每一行是一条成菜要求。

- 形状：`Whole` 原形，`Dice` 切丁，`Slice` 切片，`Shred` 切丝
- 烹饪方法：`Raw` 没煮过，`Boiled` 煮，`Fried` 煎，`BoiledAndFried` 先煎后煮

| ingredient Id  | shape | cook Method    | count |
| -------------- | ----- | -------------- | ----- |
| `ing_mushroom` | Slice | BoiledAndFried | 1     |

食谱没写份数时都按 1。

`seasonings` 里每一行是「往某一份食材上加一次这种调料」。同一个食材的多行，从上到下就是玩家必须倒的顺序。

`seasoning Id` 填调味料资产上的 `id`，`target Ingredient Id` 填食材的 `id`。



`steps` 是日记。==Steps 功能还没接上，暂时不用写==。
一行可以只填用得到的格子：

`order` 是第几步，

`hint Text` 是那一句中文，

`action` 是这一步在做什么（`Cut` 切、`Boil` 煮、`Fry` 煎、`Season` 调味、`Plate` 装盘）。

第 7 天火锅的 `ingredients` 和 `seasonings` 都留空。

