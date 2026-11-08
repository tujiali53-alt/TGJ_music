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
        [SerializeField] Sprite occupiedTokenBase;
        [SerializeField] float donenessPerSecond = 40f;
        [SerializeField] Color cookedColor = new Color(0.22f, 0.12f, 0.08f, 1f);
        [SerializeField] float cookedAlpha = 1f;
        [SerializeField] float fireKnobDegreesPerSecond = 90f;

        public Sprite OccupiedTokenBase => occupiedTokenBase;

        const float FireOnAngle = -45f;

        static CookingPot current;

        readonly List<Zone> zones = new List<Zone>();
        Button boiledFireButton;
        Button friedFireButton;
        bool boiling;
        bool frying;
        float boiledKnobAngle;
        float friedKnobAngle;
        float boiledKnobTarget;
        float friedKnobTarget;

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
                zone.Tokens.WarnMissingAreas();
                if (zone.Tokens.TokenCount == 0)
                    Debug.LogError(area.name + " 下需要已经摆好的食物令牌。", area);

                zones.Add(zone);
            }

            if (zones.Count == 0)
                Debug.LogError("CookingPot 下需要 Fried_area 和 Boiled_area，并挂 PotArea。", this);

            boiledFireButton = SceneButtons.Find("openFire_BoiledButton");
            friedFireButton = SceneButtons.Find("openFire_FiredButton");
            if (boiledFireButton != null)
                boiledFireButton.onClick.AddListener(ToggleBoiledFire);
            if (friedFireButton != null)
                friedFireButton.onClick.AddListener(ToggleFriedFire);
        }

        void OnDestroy()
        {
            if (boiledFireButton != null)
                boiledFireButton.onClick.RemoveListener(ToggleBoiledFire);
            if (friedFireButton != null)
                friedFireButton.onClick.RemoveListener(ToggleFriedFire);
        }

        public bool TryPlace(IngredientInstance item)
        {
            return false;
        }

        public static bool PlaceAt(
            Vector2 screen,
            Camera cam,
            IngredientInstance item,
            IngredientDefinition definition,
            out bool outsideArea)
        {
            outsideArea = false;
            if (current == null || item == null)
            {
                outsideArea = true;
                return false;
            }

            if (TutorialDirector.Instance != null && TutorialDirector.Instance.BlocksCookingInput)
                return false;

            Zone zone = null;
            FoodSlotView token = null;
            for (int i = 0; i < current.zones.Count; i++)
            {
                if (!current.zones[i].Tokens.TryHit(screen, cam, out FoodSlotView hit))
                    continue;

                zone = current.zones[i];
                token = hit;
                break;
            }

            if (zone == null || token == null)
            {
                outsideArea = true;
                return false;
            }

            if (token.Shown != null && token.Shown != item)
                return false;

            bool placedNow = token.Shown != item;
            if (placedNow)
            {
                Detach(item);
                Plate.Detach(item);
                CuttingBoard.Detach(item);
                IngredientInventory.Remove(item);
                zone.Held.Add(item);
                zone.Tokens.ShowOn(token, item, definition);
            }

            if (current.Heating(zone))
                ApplyArea(item, zone.Area.Method);

            if (placedNow && zone.Area != null && TutorialDirector.Instance != null)
                TutorialDirector.Instance.NotifyPotPlaced(item, zone.Area.Method);

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
            if (TutorialDirector.Instance != null)
                TutorialDirector.Instance.NotifyReturnedToInventory(item);
        }

        public static bool TryConvertLeavingEgg(IngredientInstance item)
        {
            if (current == null || item == null || current.FindHolding(item) == null)
                return false;

            IngredientDefinition egg = FoodTray.Lookup(item.DefinitionId);
            if (egg == null || egg.id != "ing_egg")
                return false;

            if (item.CookMethod == CookMethod.Raw)
                return false;

            if (item.Doneness < DeliciousnessScore.OptimalStart(egg) || item.Doneness >= egg.maxDoneness)
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

        public void ToggleBoiledFire()
        {
            boiling = !boiling;
            boiledKnobTarget = boiling ? FireOnAngle : 0f;
            AudioManager.PlayClick();
            AudioManager.SetBoiling(boiling);
            if (boiling)
                Mark(CookMethod.Boiled);
        }

        public void ToggleFriedFire()
        {
            frying = !frying;
            friedKnobTarget = frying ? FireOnAngle : 0f;
            AudioManager.PlayClick();
            AudioManager.SetFrying(frying);
            if (frying)
                Mark(CookMethod.Fried);
        }

        public void TurnOffAllFires()
        {
            if (boiling)
                ToggleBoiledFire();
            if (frying)
                ToggleFriedFire();
        }

        public static Color DonenessTint(float doneness, float maxDoneness)
        {
            if (current == null || maxDoneness <= 0f)
                return Color.white;

            float t = Mathf.Clamp01(doneness / maxDoneness);
            Color color = Color.Lerp(Color.white, current.cookedColor, t);
            color.a = Mathf.Lerp(1f, Mathf.Clamp01(current.cookedAlpha), t);
            return color;
        }

        void Mark(CookMethod method)
        {
            for (int z = 0; z < zones.Count; z++)
            {
                Zone zone = zones[z];
                if (zone.Area == null || zone.Area.Method != method)
                    continue;

                List<IngredientInstance> held = zone.Held;
                for (int i = 0; i < held.Count; i++)
                {
                    if (held[i] != null)
                        ApplyArea(held[i], method);
                }
            }
        }

        public static bool IsHeating(CookMethod method)
        {
            if (current == null)
                return false;

            if (method == CookMethod.Boiled)
                return current.boiling;

            if (method == CookMethod.Fried)
                return current.frying;

            return false;
        }

        void Update()
        {
            StepFireKnob(boiledFireButton, ref boiledKnobAngle, boiledKnobTarget);
            StepFireKnob(friedFireButton, ref friedKnobAngle, friedKnobTarget);

            for (int z = 0; z < zones.Count; z++)
            {
                Zone zone = zones[z];
                if (!Heating(zone))
                    continue;

                List<IngredientInstance> held = zone.Held;
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

                    float previous = item.Doneness;
                    float next = Mathf.Min(definition.maxDoneness, previous + donenessPerSecond * Time.deltaTime);
                    if (item.OptimalHitTime < 0f && previous < definition.optimalDoneness && next >= definition.optimalDoneness)
                    {
                        item.OptimalHitTime = Time.time;
                        item.Doneness = definition.optimalDoneness;
                        if (TutorialDirector.Instance != null
                            && TutorialDirector.Instance.TryHandleOptimalDoneness(item))
                            TurnOffAllFires();
                        continue;
                    }

                    item.Doneness = next;
                }
            }
        }

        void StepFireKnob(Button button, ref float angle, float target)
        {
            if (button == null)
                return;

            float speed = fireKnobDegreesPerSecond > 0f ? fireKnobDegreesPerSecond : 90f;
            angle = Mathf.MoveTowards(angle, target, speed * Time.deltaTime);
            button.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        bool Heating(Zone zone)
        {
            if (zone == null || zone.Area == null)
                return false;

            if (zone.Area.Method == CookMethod.Boiled)
                return boiling;

            if (zone.Area.Method == CookMethod.Fried)
                return frying;

            return false;
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
                CookMethod = egg.CookMethod,
                SeasoningIds = seasonings,
                IsCharcoal = false,
                OptimalHitTime = egg.OptimalHitTime >= 0f ? egg.OptimalHitTime : Time.time
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
