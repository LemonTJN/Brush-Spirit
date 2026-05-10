using BrushSpirit.Core;
using BrushSpirit.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameBootstrap = BrushSpirit.GameRuntimeBootstrap;

namespace BrushSpirit.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public EquipmentData data;
        [SerializeField] float rotateSpeed = 120f;

        static bool s_firstDropHighlightPending01 = true;

        public static Pickup SpawnAt(Vector3 position, EquipmentData equipment)
        {
            var go = new GameObject("Pickup_" + (equipment != null ? equipment.displayName : "loot"));
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameBootstrap.CreatePlaceholderSprite();
            sr.sortingOrder = 20;
            sr.color = equipment != null ? equipment.visualTint : Color.yellow;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;
            var p = go.AddComponent<Pickup>();
            p.data = equipment;
            var clamp = go.AddComponent<ClampToWorldBounds2D>();
            clamp.halfWidthPad = 0.42f;
            clamp.halfHeightPad = 0.42f;
            clamp.skipClampWhenOutsideViewport = false;

            if (SceneManager.GetActiveScene().name == "InkForest_01" && s_firstDropHighlightPending01)
            {
                s_firstDropHighlightPending01 = false;
                go.AddComponent<PickupFirstHighlight>();
            }

            return p;
        }

        void Update()
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var holder = other.GetComponent<EquipmentHolder>();
            if (holder == null || data == null) return;
            if (holder.TryEquip(data))
            {
                Destroy(gameObject);
            }
        }
    }
}
