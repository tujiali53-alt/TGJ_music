using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class CookingPot : MonoBehaviour, IFoodStation
    {
        public const int Capacity = 8;

        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] float donenessPerSecond = 40f;

        static CookingPot current;

        readonly List<Zone> zones = new List<Zone>();
        Button fireButton;
        bool burning;

        void Awake()
        {
            current = this;
            PotArea[] areas = GetComponentsInChildren<PotArea>(true);
            for (int i = 0; i < areas.Length; i++)
            {
                PotArea area = areas[i];
                if (area == null)
                    continue;

                if (area.Method != CookMethod.Fried && area.Method != CookMethod.Boiled)
                {
                    Debug.LogError("锅区只接受煎或煮：" + area.name, area);
                    continue;
                }

                var zone = new Zone(area);
                zone.Tokens.Collect();
                if (zone.Tokens.TokenCount == 0)
                    Debug.LogError(area.name + " 下需要已经摆好的食物令牌。", area);

                zones.Add(zone);
            }

            if (zones.Count == 0)
                Debug.LogError("CookingPot 下需要 Fried_area 和 Boiled_area，并挂 PotArea。", this);

            fireButton = SceneButtons.Find("openFireButton");
            if (fireButton != null)
                fireButton.onClick.AddListener(ToggleFire);
        }

        void OnDestroy()
        {
            if (fireButton != null)
                fireButton.onClick.RemoveListener(ToggleFire);
        }

        public bool TryPlace(IngredientInstance item)
        {
            return false;
        }

        public static bool PlaceOn(PotArea area, IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            if (current == null || area == null)
                return false;

            return current.TryPlaceOn(area, item, localPoint, definition);
        }

        bool TryPlaceOn(PotArea area, IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            Zone zone = FindZone(area);
            if (item == null || zone == null)
                return false;

            if (zone.Held.Contains(item))
            {
                ApplyArea(item, zone.Area.Method);
                zone.Tokens.Present(item, localPoint, definition);
                return true;
            }

            if (zone.Held.Count >= zone.Tokens.TokenCount || !zone.Tokens.HasFree())
                return false;

            Detach(item);
            Plate.Detach(item);
            CuttingBoard.Detach(item);
            IngredientInventory.Remove(item);
            zone.Held.Add(item);
            ApplyArea(item, zone.Area.Method);
            zone.Tokens.Present(item, localPoint, definition);
            return true;
        }

        public static bool Detach(IngredientInstance item)
        {
            if (current == null || item == null)
                return false;

            Zone zone = current.FindHolding(item);
            if (zone == null)
                return false;

            zone.Held.Remove(item);
            zone.Tokens.Clear(item);
            return true;
        }

        public static void ReleaseToInventory(IngredientInstance item)
        {
            if (TryConvertLeavingEgg(item))
                return;

            if (!Detach(item))
                return;

            IngredientInventory.Add(item);
        }

        public static bool TryConvertLeavingEgg(IngredientInstance item)
        {
            if (current == null || item == null || current.FindHolding(item) == null)
                return false;

            IngredientDefinition egg = FoodTray.Lookup(item.DefinitionId);
            if (egg == null || egg.id != "ing_egg")
                return false;

            if (item.CookMethod != CookMethod.Boiled)
                return false;

            if (item.Doneness < egg.optimalDoneness || item.Doneness >= egg.maxDoneness)
                return false;

            IngredientDefinition done = FoodTray.Lookup("ing_doneegg");
            if (done == null)
            {
                Debug.LogError("缺少熟鸡蛋定义 ing_doneegg。");
                return false;
            }

            if (!Detach(item))
                return false;

            IngredientInventory.Add(CreateDoneEgg(item, done));
            return true;
        }

        public void ToggleFire()
        {
            burning = !burning;
        }

        void Update()
        {
            if (!burning)
                return;

            for (int z = 0; z < zones.Count; z++)
            {
                List<IngredientInstance> held = zones[z].Held;
                for (int i = 0; i < held.Count; i++)
                {
                    IngredientInstance item = held[i];
                    if (item == null)
                        continue;

                    IngredientDefinition definition = FindDefinition(item.DefinitionId);
                    if (definition == null)
                    {
                        Debug.LogError("煮锅缺少这份食材的定义：" + item.DefinitionId, this);
                        continue;
                    }

                    if (item.Doneness >= definition.maxDoneness)
                        continue;

                    float next = item.Doneness + donenessPerSecond * Time.deltaTime;
                    item.Doneness = Mathf.Min(definition.maxDoneness, next);
                }
            }
        }

        static void ApplyArea(IngredientInstance item, CookMethod area)
        {
            bool fried = item.CookMethod == CookMethod.Fried || item.CookMethod == CookMethod.BoiledAndFried;
            bool boiled = item.CookMethod == CookMethod.Boiled || item.CookMethod == CookMethod.BoiledAndFried;
            if (area == CookMethod.Fried)
                fried = true;
            else if (area == CookMethod.Boiled)
                boiled = true;

            if (fried && boiled)
                item.CookMethod = CookMethod.BoiledAndFried;
            else if (fried)
                item.CookMethod = CookMethod.Fried;
            else if (boiled)
                item.CookMethod = CookMethod.Boiled;
        }

        Zone FindZone(PotArea area)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Area == area)
                    return zones[i];
            }

            return null;
        }

        Zone FindHolding(IngredientInstance item)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Held.Contains(item))
                    return zones[i];
            }

            return null;
        }

        static IngredientInstance CreateDoneEgg(IngredientInstance egg, IngredientDefinition done)
        {
            var seasonings = new List<string>();
            if (egg.SeasoningIds != null)
            {
                for (int i = 0; i < egg.SeasoningIds.Count; i++)
                    seasonings.Add(egg.SeasoningIds[i]);
            }

            return new IngredientInstance
            {
                InstanceId = NextInstanceId(),
                DefinitionId = done.id,
                Quality = egg.Quality,
                Quantity = 1,
                Doneness = done.optimalDoneness,
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Boiled,
                SeasoningIds = seasonings,
                IsCharcoal = false
            };
        }

        static int NextInstanceId()
        {
            int next = 1;
            GameSession session = IngredientInventory.Session();
            if (session != null)
            {
                if (session.Inventory != null)
                {
                    for (int i = 0; i < session.Inventory.Count; i++)
                        next = RaiseId(next, session.Inventory[i]);
                }

                if (session.HeldDish != null && session.HeldDish.Items != null)
                {
                    for (int i = 0; i < session.HeldDish.Items.Count; i++)
                        next = RaiseId(next, session.HeldDish.Items[i]);
                }
            }

            if (current != null)
            {
                for (int z = 0; z < current.zones.Count; z++)
                {
                    List<IngredientInstance> held = current.zones[z].Held;
                    for (int i = 0; i < held.Count; i++)
                        next = RaiseId(next, held[i]);
                }
            }

            return next;
        }

        static int RaiseId(int next, IngredientInstance item)
        {
            if (item != null && item.InstanceId >= next)
                return item.InstanceId + 1;

            return next;
        }

        IngredientDefinition FindDefinition(string definitionId)
        {
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    IngredientDefinition definition = definitions[i];
                    if (definition != null && definition.id == definitionId)
                        return definition;
                }
            }

            return FoodTray.Lookup(definitionId);
        }

        sealed class Zone
        {
            public Zone(PotArea area)
            {
                Area = area;
                Tokens = new FoodTokens(area.transform);
            }

            public PotArea Area { get; }
            public FoodTokens Tokens { get; }
            public List<IngredientInstance> Held { get; } = new List<IngredientInstance>();
        }
    }
}
