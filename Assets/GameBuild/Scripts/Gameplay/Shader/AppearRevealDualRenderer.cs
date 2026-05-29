using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dual reveal for Appear materials: ghost stays in the renderer material slot (1:1 with submesh);
/// revealed opaque is submitted to <see cref="AppearRevealOpaqueRenderFeature"/> per submesh.
/// </summary>
[DefaultExecutionOrder(105)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class AppearRevealDualRenderer : MonoBehaviour
{
    public const string AppearGhostShaderGuid = "4a3682d05c9d74141aebf057ac7cf784";
    public const string RevealedOpaqueShaderName = "Shader Graphs/AppearShader_RevealedOpaque";
    public const string RevealedOpaqueTemplatePath = "Assets/Shader/Shader Graphs_Appear_RevealedOpaque.mat";

    public const int GhostRenderQueue = 2990;
    public const int OpaqueRevealRenderQueue = 2450;

    struct OpaqueDrawSlot
    {
        public int submeshIndex;
        public Material opaqueMaterial;
    }

    [SerializeField]
    [Tooltip("Skip opaque draw for slots whose shared material name contains any of these (e.g. Glass).")]
    string[] excludeMaterialNameContains = { "Glass", "glass" };

    [SerializeField]
    bool disableOpaquePass;

    Renderer _renderer;
    MeshRenderer _meshRenderer;
    SkinnedMeshRenderer _skinnedRenderer;
    AppearRevealGate _gate;
    MaterialPropertyBlock _ghostBlock;
    MaterialPropertyBlock _drawBlock;
    readonly List<OpaqueDrawSlot> _opaqueSlots = new List<OpaqueDrawSlot>();
    readonly List<Material> _opaqueInstances = new List<Material>();
    Mesh _bakedSkinnedMesh;
    bool _setupDone;

    static readonly int RevealId = Shader.PropertyToID("_Reveal");
    static readonly int RevealSoftId = Shader.PropertyToID("_RevealSoft");

    void Awake()
    {
        CacheRendererRefs();
        ResolveGate();
        if (_ghostBlock == null)
            _ghostBlock = new MaterialPropertyBlock();
        if (_drawBlock == null)
            _drawBlock = new MaterialPropertyBlock();

        TrySetupDualReveal();
    }

    void LateUpdate()
    {
        if (!_setupDone)
            TrySetupDualReveal();

        SyncGhostRevealPropertyBlocks();
        RegisterOpaqueDrawRequests();
    }

    void OnValidate()
    {
        CacheRendererRefs();
    }

    void CacheRendererRefs()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
        if (_skinnedRenderer == null)
            _skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    void ResolveGate()
    {
        _gate = GetComponent<AppearRevealGate>();
        if (_gate == null)
            _gate = GetComponentInParent<AppearRevealGate>();
    }

    public static void EnsureOnRenderer(Renderer renderer, string[] excludeNameTokens = null)
    {
        if (renderer == null)
            return;

        if (renderer.GetComponent<AppearRevealDualRenderer>() != null)
            return;

        if (!RendererUsesAppearGhost(renderer, excludeNameTokens))
            return;

        renderer.gameObject.AddComponent<AppearRevealDualRenderer>();
    }

    public static bool RendererUsesAppearGhost(Renderer renderer, string[] excludeNameTokens = null)
    {
        if (renderer == null)
            return false;

        Material[] mats = renderer.sharedMaterials;
        if (mats == null)
            return false;

        for (int i = 0; i < mats.Length; i++)
        {
            if (IsAppearGhostMaterial(mats[i], excludeNameTokens))
                return true;
        }

        return false;
    }

    public static bool IsAppearGhostShader(Shader shader)
    {
        return shader != null && shader.name.Contains("AppearShader") && !shader.name.Contains("RevealedOpaque");
    }

    public static bool IsAppearGhostMaterial(Material mat, string[] excludeNameTokens = null)
    {
        if (mat == null || mat.shader == null)
            return false;
        if (MaterialNameExcluded(mat.name, excludeNameTokens))
            return false;
        return IsAppearGhostShader(mat.shader);
    }

    static bool IsRevealedOpaqueMaterial(Material mat)
    {
        return mat != null && mat.shader != null && mat.shader.name.Contains("RevealedOpaque");
    }

    static bool MaterialNameExcluded(string materialName, string[] excludeNameTokens)
    {
        if (string.IsNullOrEmpty(materialName) || excludeNameTokens == null)
            return false;

        for (int i = 0; i < excludeNameTokens.Length; i++)
        {
            string token = excludeNameTokens[i];
            if (!string.IsNullOrEmpty(token) && materialName.Contains(token))
                return true;
        }

        return false;
    }

    int GetSubmeshCount()
    {
        if (_skinnedRenderer != null && _skinnedRenderer.sharedMesh != null)
            return _skinnedRenderer.sharedMesh.subMeshCount;

        if (_meshRenderer != null)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh.subMeshCount;
        }

        return _renderer != null && _renderer.sharedMaterials != null
            ? _renderer.sharedMaterials.Length
            : 0;
    }

    void TrySetupDualReveal()
    {
        if (_renderer == null || _setupDone)
            return;

        Material[] shared = _renderer.sharedMaterials;
        if (shared == null || shared.Length == 0)
            return;

        int submeshCount = GetSubmeshCount();
        if (submeshCount <= 0)
            return;

        RepairLegacyInsertedOpaqueMaterials(shared, submeshCount);
        shared = _renderer.sharedMaterials;

        bool hasGhost = false;
        for (int i = 0; i < shared.Length; i++)
        {
            if (shared[i] != null && IsAppearGhostShader(shared[i].shader))
                hasGhost = true;
        }

        if (!hasGhost)
        {
            _setupDone = true;
            return;
        }

        _opaqueSlots.Clear();
        ClearOpaqueInstances();

        if (!disableOpaquePass)
        {
            int slotCount = Mathf.Min(shared.Length, submeshCount);
            for (int i = 0; i < slotCount; i++)
            {
                Material ghost = shared[i];
                if (!IsAppearGhostMaterial(ghost, excludeMaterialNameContains))
                    continue;

                Material opaque = CreateOpaqueInstanceFromGhost(ghost);
                if (opaque == null)
                    continue;

                _opaqueInstances.Add(opaque);
                _opaqueSlots.Add(new OpaqueDrawSlot
                {
                    submeshIndex = i,
                    opaqueMaterial = opaque
                });
            }
        }

        ApplyGhostRenderQueues();
        _setupDone = true;
    }

    void RepairLegacyInsertedOpaqueMaterials(Material[] shared, int submeshCount)
    {
        if (shared.Length <= submeshCount)
            return;

        bool hasInsertedOpaque = false;
        for (int i = 0; i < shared.Length; i++)
        {
            if (IsRevealedOpaqueMaterial(shared[i]))
            {
                hasInsertedOpaque = true;
                break;
            }
        }

        if (!hasInsertedOpaque)
            return;

        var repaired = new List<Material>(shared.Length);
        for (int i = 0; i < shared.Length; i++)
        {
            if (!IsRevealedOpaqueMaterial(shared[i]))
                repaired.Add(shared[i]);
        }

        if (repaired.Count == 0)
            return;

        _renderer.materials = repaired.ToArray();
    }

    Material CreateOpaqueInstanceFromGhost(Material ghostSource)
    {
        Shader shader = Shader.Find(RevealedOpaqueShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[{nameof(AppearRevealDualRenderer)}] Missing opaque shader '{RevealedOpaqueShaderName}'.", this);
            return null;
        }

        Material opaque = new Material(shader);

        if (ghostSource.HasProperty("_BaseMap") && opaque.HasProperty("_BaseMap"))
            opaque.SetTexture("_BaseMap", ghostSource.GetTexture("_BaseMap"));

        if (ghostSource.HasProperty("_BaseColor") && opaque.HasProperty("_BaseColor"))
            opaque.SetColor("_BaseColor", ghostSource.GetColor("_BaseColor"));
        else if (ghostSource.HasProperty("_MainColor") && opaque.HasProperty("_BaseColor"))
            opaque.SetColor("_BaseColor", ghostSource.GetColor("_MainColor"));

        if (ghostSource.HasProperty("_Reveal") && opaque.HasProperty("_Reveal"))
            opaque.SetFloat("_Reveal", ghostSource.GetFloat("_Reveal"));

        if (ghostSource.HasProperty("_RevealSoft") && opaque.HasProperty("_RevealSoft"))
            opaque.SetFloat("_RevealSoft", ghostSource.GetFloat("_RevealSoft"));

        opaque.renderQueue = OpaqueRevealRenderQueue;
        return opaque;
    }

    void ApplyGhostRenderQueues()
    {
        Material[] mats = _renderer.materials;
        bool changed = false;

        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            if (m == null || m.shader == null)
                continue;

            if (!IsAppearGhostShader(m.shader))
                continue;

            if (m.renderQueue != GhostRenderQueue)
            {
                m.renderQueue = GhostRenderQueue;
                changed = true;
            }
        }

        if (changed)
            _renderer.materials = mats;
    }

    void SyncGhostRevealPropertyBlocks()
    {
        if (_renderer == null)
            return;

        if (_gate == null)
            ResolveGate();

        float reveal = ResolveRevealValue();
        Material[] shared = _renderer.sharedMaterials;
        if (shared == null)
            return;

        for (int i = 0; i < shared.Length; i++)
        {
            if (!IsAppearGhostMaterial(shared[i], excludeMaterialNameContains))
                continue;

            _renderer.GetPropertyBlock(_ghostBlock, i);
            _ghostBlock.SetFloat(RevealId, reveal);
            _renderer.SetPropertyBlock(_ghostBlock, i);
        }
    }

    float ResolveRevealValue()
    {
        if (_gate != null)
            return _gate.appearSatisfied ? _gate.revealAlongU : 0f;

        return GetSharedMaterialRevealDefault();
    }

    float GetSharedMaterialRevealDefault()
    {
        Material[] shared = _renderer.sharedMaterials;
        if (shared == null)
            return 1f;

        for (int i = 0; i < shared.Length; i++)
        {
            Material m = shared[i];
            if (!IsAppearGhostMaterial(m, excludeMaterialNameContains))
                continue;
            if (m != null && m.HasProperty("_Reveal"))
                return m.GetFloat("_Reveal");
        }

        return 1f;
    }

    void RegisterOpaqueDrawRequests()
    {
        if (!isActiveAndEnabled || disableOpaquePass || _opaqueSlots.Count == 0)
            return;

        if (_renderer == null || !_renderer.enabled || !_renderer.gameObject.activeInHierarchy)
            return;

        if (!_renderer.isVisible)
            return;

        AppearRevealOpaqueDrawRegistry.EnsureCurrentFrame();

        Mesh mesh = AcquireDrawMesh();
        if (mesh == null)
            return;

        float reveal = ResolveRevealValue();
        Matrix4x4 matrix = _renderer.localToWorldMatrix;
        int layer = gameObject.layer;

        for (int i = 0; i < _opaqueSlots.Count; i++)
        {
            OpaqueDrawSlot slot = _opaqueSlots[i];
            if (slot.opaqueMaterial == null)
                continue;

            if (slot.submeshIndex < 0 || slot.submeshIndex >= mesh.subMeshCount)
                continue;

            _drawBlock.Clear();
            _drawBlock.SetFloat(RevealId, reveal);

            AppearRevealOpaqueDrawRegistry.Add(new AppearRevealOpaqueDrawRegistry.DrawRequest
            {
                mesh = mesh,
                matrix = matrix,
                material = slot.opaqueMaterial,
                submeshIndex = slot.submeshIndex,
                reveal = reveal,
                layer = layer
            });
        }
    }

    Mesh AcquireDrawMesh()
    {
        if (_skinnedRenderer != null && _skinnedRenderer.sharedMesh != null)
        {
            if (_bakedSkinnedMesh == null)
                _bakedSkinnedMesh = new Mesh { name = "AppearReveal_BakedSkinned" };

            _skinnedRenderer.BakeMesh(_bakedSkinnedMesh);
            return _bakedSkinnedMesh;
        }

        if (_meshRenderer != null)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null)
                return filter.sharedMesh;
        }

        return null;
    }

    void ClearOpaqueInstances()
    {
        for (int i = 0; i < _opaqueInstances.Count; i++)
        {
            Material inst = _opaqueInstances[i];
            if (inst == null)
                continue;

            if (Application.isPlaying)
                Destroy(inst);
            else
                DestroyImmediate(inst);
        }

        _opaqueInstances.Clear();
    }

    void OnDestroy()
    {
        ClearOpaqueInstances();

        if (_bakedSkinnedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_bakedSkinnedMesh);
            else
                DestroyImmediate(_bakedSkinnedMesh);
        }
    }
}
