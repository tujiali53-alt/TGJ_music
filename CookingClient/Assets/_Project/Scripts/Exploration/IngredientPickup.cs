using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class IngredientPickup : MonoBehaviour
    {
        [SerializeField] IngredientDefinition definition;
        [SerializeField] Transform player;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] float collectDistance = 2.5f;

        bool collected;

        void Awake()
        {
            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (hitCollider == null)
                Debug.LogError("采集物上需要已经放好碰撞体。", this);

            if (definition == null)
                Debug.LogError("采集物上需要指定食材定义。", this);

            if (player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    player = found.transform;
            }
        }

        void Update()
        {
            if (collected || definition == null || hitCollider == null || player == null)
                return;

            if (GetComponent<InteractionPoint>() != null)
                return;

            if (!ScenePointer.TryGetClick(out Vector2 world))
                return;

            if (!hitCollider.OverlapPoint(world))
                return;

            Vector2 playerPosition = player.position;
            if (Vector2.Distance(playerPosition, transform.position) > collectDistance)
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return;

            Collect(root.Session);
        }

        void Collect(GameSession session)
        {
            if (session.Inventory == null)
                session.Inventory = new List<IngredientInstance>();

            int nextId = 1;
            for (int i = 0; i < session.Inventory.Count; i++)
            {
                if (session.Inventory[i].InstanceId >= nextId)
                    nextId = session.Inventory[i].InstanceId + 1;
            }

            session.Inventory.Add(new IngredientInstance
            {
                InstanceId = nextId,
                DefinitionId = definition.id,
                Quality = QualityTier.Normal,
                Quantity = 1,
                Doneness = 0f,
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Raw,
                SeasoningIds = new List<string>(),
                IsCharcoal = false
            });

            collected = true;
            gameObject.SetActive(false);
        }
    }
}
