using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 위상전이 동안 쓴 사람이 움직인 자리에 몸의 잔상을 남긴다(2D판과 같다). 몸에 붙는 기운 효과(PhaseAura)에 들어 있어
    /// 쓴 사람을 따라다니고, 몸이 정한 거리만큼 움직일 때마다 지금 자세를 메시로 구워(BakeMesh) 그 자리에 세운 뒤
    /// 툰 효과 셰이더의 갉아먹기(_Erosion)로 사라지게 한다.
    /// 잔상은 처음에 몇 벌(몸 부위마다 메시 하나) 만들어 두고 켜고 끄며 돌려 쓴다. 쓴 사람을 따라가지 않게 월드에 따로 둔다.
    /// </summary>
    public sealed class SkillAfterimage : MonoBehaviour
    {
        private static readonly int ErosionId = Shader.PropertyToID("_Erosion");

        [Tooltip("잔상을 그리는 재질(툰 효과 셰이더)")]
        [SerializeField] private Material material;
        [Tooltip("몸이 이만큼(m) 움직일 때마다 잔상을 하나 남긴다")]
        [SerializeField] private float spacing = 0.35f;
        [Tooltip("잔상 하나가 다 사라지는 데 걸리는 시간(초)")]
        [SerializeField] private float lifetime = 0.35f;
        [Tooltip("한꺼번에 남아 있을 수 있는 잔상 수")]
        [SerializeField] private int capacity = 6;

        private SkinnedMeshRenderer[] bodyParts;
        private GameObject holder;
        // [잔상, 몸 부위]
        private MeshFilter[,] shapes;
        private MeshRenderer[,] images;
        private float[] ages;
        private int next;
        private Transform body;
        private Vector3 lastLeftAt;
        private MaterialPropertyBlock fade;

        private void Start()
        {
            // 쓴 사람에게 붙은 뒤(SkillCaster가 붙인다) 그 사람의 몸과 지금 입은 옷을 찾는다.
            var caster = GetComponentInParent<Fighter>();
            body = caster.transform;
            bodyParts = caster.GetComponentsInChildren<SkinnedMeshRenderer>();
            holder = new GameObject("Afterimages");
            shapes = new MeshFilter[capacity, bodyParts.Length];
            images = new MeshRenderer[capacity, bodyParts.Length];
            ages = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                // 처음에는 모두 다 사라진 잔상이다.
                ages[i] = lifetime;
                for (int part = 0; part < bodyParts.Length; part++)
                {
                    CreateImage(i, part);
                }
            }

            fade = new MaterialPropertyBlock();
            lastLeftAt = body.position;
        }

        private void CreateImage(int i, int part)
        {
            var image = new GameObject(bodyParts[part].name);
            image.transform.SetParent(holder.transform, false);
            shapes[i, part] = image.AddComponent<MeshFilter>();
            shapes[i, part].sharedMesh = new Mesh();
            images[i, part] = image.AddComponent<MeshRenderer>();
            // 부위의 재질 칸(서브메시)마다 같은 잔상 재질을 넣는다.
            var materials = new Material[bodyParts[part].sharedMaterials.Length];
            System.Array.Fill(materials, material);
            images[i, part].sharedMaterials = materials;
            image.SetActive(false);
        }

        private void Update()
        {
            if ((body.position - lastLeftAt).sqrMagnitude >= spacing * spacing)
            {
                LeaveImage();
            }

            for (int i = 0; i < capacity; i++)
            {
                if (ages[i] < lifetime)
                {
                    ages[i] += Time.deltaTime;
                    Fade(i);
                }
            }
        }

        private void LeaveImage()
        {
            lastLeftAt = body.position;
            int i = next;
            next = (next + 1) % capacity;
            ages[i] = 0f;
            for (int part = 0; part < bodyParts.Length; part++)
            {
                // Luna의 부위는 FBX 단위 때문에 0.01배로 줄어 있다. 크기를 빼고 구우면(false) 미터 단위 메시가 나오므로
                // 잔상에는 자리와 방향만 입힌다(크기를 넣고 구우면 100배 큰 잔상이 됐다).
                Transform from = bodyParts[part].transform;
                Transform image = shapes[i, part].transform;
                bodyParts[part].BakeMesh(shapes[i, part].sharedMesh, false);
                image.SetPositionAndRotation(from.position, from.rotation);
                image.gameObject.SetActive(true);
            }
        }

        private void Fade(int i)
        {
            bool alive = ages[i] < lifetime;
            fade.SetFloat(ErosionId, ages[i] / lifetime);
            for (int part = 0; part < bodyParts.Length; part++)
            {
                images[i, part].gameObject.SetActive(alive);
                images[i, part].SetPropertyBlock(fade);
            }
        }

        private void OnDestroy()
        {
            if (holder == null)
            {
                return;
            }

            foreach (MeshFilter shape in shapes)
            {
                Destroy(shape.sharedMesh);
            }

            Destroy(holder);
        }
    }
}
