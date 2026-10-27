using Cooking.Core;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class PlayerLight : MonoBehaviour
    {
        public const float MinimumRadius = 0.1f;

        [SerializeField] float initialRadius = 2.5f;
        [SerializeField] float radiusPerLight = 0.45f;
        [SerializeField] float buffRadiusBonus;
        [SerializeField] Transform bed;
        [SerializeField] float bedRadius = 2.2f;
        [SerializeField] Renderer glowMask;
        [SerializeField] ParticleSystem orbitParticles;
        [SerializeField] ParticleSystem driftParticles;

        static readonly int PlayerPosId = Shader.PropertyToID("_PlayerPos");
        static readonly int BedPosId = Shader.PropertyToID("_BedPos");
        static readonly int RadiiId = Shader.PropertyToID("_Radii");

        MaterialPropertyBlock block;
        Camera viewCamera;

        public float InitialRadius => initialRadius;
        public float RadiusPerLight => radiusPerLight;
        public float CurrentRadius { get; private set; }

        public float RadiusFor(int lights)
        {
            return Mathf.Max(
                MinimumRadius,
                initialRadius + lights * radiusPerLight + buffRadiusBonus);
        }

        public float BuffRadiusBonus
        {
            get => buffRadiusBonus;
            set => buffRadiusBonus = value;
        }

        void Awake()
        {
            if (bed == null)
            {
                GameObject foundBed = GameObject.Find("bed");
                if (foundBed != null)
                    bed = foundBed.transform;
            }

            if (glowMask == null)
            {
                GameObject foundMask = GameObject.Find("GlowMask");
                if (foundMask != null)
                    glowMask = foundMask.GetComponent<Renderer>();
            }

            if (bed == null)
                Debug.LogError("场景里需要有 bed，圆光要照到床。", this);

            if (glowMask == null)
                Debug.LogError("Main Camera 下需要有 GlowMask。", this);

            if (orbitParticles == null)
                Debug.LogError("Player 下需要有 OrbitParticles。", this);

            if (driftParticles == null)
                Debug.LogError("Player 下需要有 DriftParticles。", this);

            block = new MaterialPropertyBlock();
            viewCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;

            int lights = 0;
            GameRoot root = GameRoot.Instance;
            if (root != null && root.Session != null)
                lights = root.Session.LightsAbsorbedTonight;

            CurrentRadius = RadiusFor(lights);

            FitMaskToCamera();
            PushShader();
            FitParticles(orbitParticles);
            FitParticles(driftParticles);
        }

        void FitMaskToCamera()
        {
            if (glowMask == null || viewCamera == null)
                return;

            float height = viewCamera.orthographicSize * 2f * 1.05f;
            float width = height * Mathf.Max(viewCamera.aspect, 0.01f);
            glowMask.transform.localScale = new Vector3(width, height, 1f);
        }

        void PushShader()
        {
            if (glowMask == null || block == null)
                return;

            Vector3 player = transform.position;
            Vector3 bedPosition = bed != null ? bed.position : player;
            float edge = 0.4f;
            if (glowMask.sharedMaterial != null)
            {
                float authored = glowMask.sharedMaterial.GetVector(RadiiId).x;
                if (authored > 0.001f)
                    edge = authored;
            }

            glowMask.GetPropertyBlock(block);
            block.SetVector(PlayerPosId, new Vector4(player.x, player.y, 0f, 0f));
            block.SetVector(BedPosId, new Vector4(bedPosition.x, bedPosition.y, 0f, 0f));
            block.SetVector(RadiiId, new Vector4(edge, CurrentRadius, Mathf.Max(MinimumRadius, bedRadius), 0f));
            glowMask.SetPropertyBlock(block);
        }

        void FitParticles(ParticleSystem system)
        {
            if (system == null)
                return;

            float scale = Mathf.Max(Mathf.Abs(system.transform.lossyScale.x), 0.0001f);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = CurrentRadius / scale;
        }
    }
}
