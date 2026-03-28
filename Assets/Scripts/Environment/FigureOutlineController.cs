using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FigureOutlineController : MonoBehaviour
{
    public static List<GameObject> trackedObjects  = new List<GameObject>();
    public List<GameObject> specialObjects = new List<GameObject>();
    public Color outlineColor = Color.red;
    public float outlineWidth = 0.015f;
    public string shaderName = "Custom/Outline";
    public LayerMask blockingLayers = Physics.AllLayers;

    private Camera mainCamera;
    private Shader shader;
    private Dictionary<GameObject, List<GameObject>> outlineMap = new();

    void Start()
    {
        mainCamera = Camera.main;
        //Debug.Log($"[Outline] Start. Камера: {(mainCamera == null ? "НЕ НАЙДЕНА!" : mainCamera.name)}");

        shader = Shader.Find(shaderName);
        //Debug.Log($"[Outline] Шейдер '{shaderName}': {(shader == null ? "НЕ НАЙДЕН!" : "OK")}");

        if (shader == null) { enabled = false; return; }

        //Debug.Log($"[Outline] Врагов в trackedObjects  при старте: {trackedObjects .Count}");

        trackedObjects.AddRange(EnemyType.enemiesList.Select(enemy=>enemy.enemy.gameObject));
        trackedObjects.AddRange(specialObjects);

    }

    void Update()
    {
        foreach (var figure in trackedObjects )
        {
            if (figure == null) continue;

            if (!outlineMap.ContainsKey(figure.gameObject))
            {
                //Debug.Log($"[Outline] Новый враг найден: {figure.gameObject.name}, создаём outline...");
                var created = CreateOutlineRenderers(figure.gameObject);
                //Debug.Log($"[Outline] Создано outline-объектов для {figure.gameObject.name}: {created.Count}");
                outlineMap[figure.gameObject] = created;
            }

            if (!outlineMap.TryGetValue(figure.gameObject, out var gos)) continue;

            bool blocked = IsBlocked(figure.gameObject);


            if (figure.CompareTag("Enemy") && figure.GetComponent<Enemy>().isDead)
            {
                //Debug.Log(figure.GetComponent<EnemyType>().enemy.isDead);
                blocked = false;
            }
            
            //Debug.Log($"[Outline] {figure.gameObject.name} blocked={blocked}");

            foreach (var go in gos)
                if (go != null)
                    go.SetActive(blocked);
        }

        var toRemove = new List<GameObject>();
        foreach (var key in outlineMap.Keys)
        {
            if (key == null) { toRemove.Add(key); continue; }

            bool stillAlive = false;
            foreach (var e in trackedObjects )
                if (e != null && e.gameObject == key) { stillAlive = true; break; }

            if (!stillAlive)
            {
                //Debug.Log($"[Outline] Враг {key.name} мёртв, удаляем outline.");
                foreach (var go in outlineMap[key])
                    if (go != null) Destroy(go);
                toRemove.Add(key);
            }
        }
        foreach (var key in toRemove)
            outlineMap.Remove(key);
    }

    List<GameObject> CreateOutlineRenderers(GameObject figure)
    {
        var list = new List<GameObject>();
        if (shader == null) return list;

        var allRenderers = figure.GetComponentsInChildren<Renderer>();
        //Debug.Log($"[Outline] У {figure.name} найдено рендереров: {allRenderers.Length}");

        foreach (var rend in allRenderers)
        {
            //Debug.Log($"[Outline]   Рендерер: {rend.gameObject.name} | Тип: {rend.GetType().Name}");

            if (rend is ParticleSystemRenderer)
            {
                //Debug.Log($"[Outline]   -> Пропускаем (ParticleSystem)");
                continue;
            }

            GameObject go = new GameObject($"_Outline_{rend.gameObject.name}");
            go.transform.SetParent(rend.transform, false);
            go.layer = rend.gameObject.layer;

            Renderer outlineRend = null;

            if (rend is SkinnedMeshRenderer smr)
            {
                // sharedMesh может быть null у анимированных мешей — бейкаем если нужно
                Mesh mesh = smr.sharedMesh;
                if (mesh == null)
                {
                    mesh = new Mesh();
                    smr.BakeMesh(mesh);
                    //Debug.LogWarning($"[Outline]   -> sharedMesh был null, использован BakeMesh.");
                }

                //Debug.Log($"[Outline]   -> SkinnedMeshRenderer, костей: {smr.bones.Length}");
                var newSmr = go.AddComponent<SkinnedMeshRenderer>();
                newSmr.sharedMesh = mesh;
                newSmr.bones = smr.bones;
                newSmr.rootBone = smr.rootBone;
                outlineRend = newSmr;
            }
            else if (rend is MeshRenderer)
            {
                var mf = rend.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    //Debug.LogWarning($"[Outline]   -> MeshRenderer без MeshFilter, пропускаем.");
                    Destroy(go);
                    continue;
                }
                //Debug.Log($"[Outline]   -> MeshRenderer OK, меш: {mf.sharedMesh?.name}");
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                outlineRend = go.AddComponent<MeshRenderer>();
            }
            else
            {
                //Debug.LogWarning($"[Outline]   -> Неизвестный тип рендерера, пропускаем.");
                Destroy(go);
                continue;
            }

            if (outlineRend == null) { Destroy(go); continue; }

            var mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = new Material(shader);
                mat.SetColor("_OutlineColor", figure.CompareTag("Player") ? Color.blue : outlineColor);
                mat.SetFloat("_OutlineWidth", outlineWidth);
                mats[i] = mat;
            }
            outlineRend.sharedMaterials = mats;
            go.SetActive(false);
            list.Add(go);

            //Debug.Log($"[Outline]   -> outline-объект создан: {go.name}");
        }

        return list;
    }

    bool IsBlocked(GameObject figure)
    {
        Vector3 from = mainCamera.transform.position;
        Vector3 to = figure.transform.position;
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dist, blockingLayers);

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject != figure &&
                !hit.collider.transform.IsChildOf(figure.transform))
            {
                //Debug.Log($"[Outline] {figure.name} перекрыт объектом: '{hit.collider.gameObject.name}' (слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                return true;
            }
        }

        return false;
    }

    void OnDestroy()
    {
        foreach (var list in outlineMap.Values)
            foreach (var go in list)
                if (go != null) Destroy(go);
    }
}