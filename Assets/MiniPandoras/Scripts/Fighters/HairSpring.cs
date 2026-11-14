using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 머리카락 가닥을 흔든다(스프링 본). 뼈마다 끝점을 따로 움직이며, 직전의 움직임을 이어 가려는 관성,
    /// 모델에 지어진 자세로 돌아가려는 힘, 아래로 처지는 무게를 더한 뒤 뼈 길이를 지키고 머리·어깨의 공(<see cref="HairCollider"/>)
    /// 밖으로 밀어낸다. 그 끝점을 향하게 뼈를 돌린다.
    /// 머리카락 뼈는 애니메이션이 움직이지 않으므로, 애니메이션과 IK가 끝난 뒤(LateUpdate) 이번 프레임의 머리 자세를 따라 돈다.
    /// Luna의 머리카락은 덩어리 위에 가닥이 얹힌 모양이라 크게 흔들면 가닥이 떨어져 보인다. 그래서 몸이 옮겨 간 만큼 끝점도
    /// 대부분 함께 옮기고(<see cref="bodyInertia"/>만 늦게 따라온다), 지어진 자세에서 <see cref="maxAngle"/>까지만 벗어나게 한다.
    /// </summary>
    public sealed class HairSpring : MonoBehaviour
    {
        // 계산 한 걸음(초). 걸음을 고정해야 프레임 빠르기와 상관없이, 느려진 시간(쓰러짐 연출)에서도 같은 모양으로 흔들린다.
        private const float Step = 1f / 60f;
        // 한 프레임에 따라잡는 걸음 수의 한계. 에디터가 멈췄다 돌아올 때 몰아서 계산하지 않게 한다.
        private const int MaxStepsPerFrame = 4;
        // 머리가 한 프레임에 이보다 멀리 옮겨지면 순간이동(차원참, 새 판)으로 보고 흔들림 없이 제자리에 둔다.
        // 공간도약(한 프레임에 1m 남짓)은 순간이동이 아니라서 머리카락이 뒤로 날린다.
        private const float TeleportDistance = 2f;

        private struct Joint
        {
            public Transform Bone;
            public Quaternion RestLocalRotation;
            // 뼈 자신의 축에서 본 끝점 방향과 끝점까지의 길이(m).
            public Vector3 Axis;
            public float Length;
            public Vector3 Tail;
            public Vector3 PreviousTail;
            // 지난 프레임에 뼈 뿌리가 있던 곳. 몸이 옮겨 간 거리를 잰다.
            public Vector3 LastRoot;
        }

        [Tooltip("가닥마다 첫 뼈(머리 뼈의 자식). 그 아래로 첫 자식을 따라 끝까지 흔든다")]
        [SerializeField] private Transform[] roots;
        [SerializeField] private HairCollider[] colliders;
        [Tooltip("지어진 자세로 돌아가려는 힘(1/초²). 클수록 빳빳하고 빨리 돌아온다")]
        [SerializeField] private float stiffness = 400f;
        [Tooltip("한 걸음마다 움직임을 잃는 비율(0~1). 클수록 덜 출렁인다")]
        [SerializeField, Range(0f, 1f)] private float drag = 0.15f;
        [Tooltip("아래로 처지는 힘(m/초²)")]
        [SerializeField] private float gravity = 3f;
        [Tooltip("몸이 움직일 때 머리카락 끝이 제자리에 남으려는 비율(0~1). 1이면 실제 머리카락처럼 다 남고, 0이면 흔들리지 않는다")]
        [SerializeField, Range(0f, 1f)] private float bodyInertia = 0.3f;
        [Tooltip("뼈 하나가 지어진 자세에서 벗어날 수 있는 각도(도)")]
        [SerializeField] private float maxAngle = 30f;
        [Tooltip("머리카락의 굵기(m). 공에서 이만큼 더 띄운다")]
        [SerializeField] private float hairRadius = 0.02f;

        private Joint[] joints;
        private Transform head;
        private Vector3 lastHeadPosition;
        private float unsimulated;

        private void Awake()
        {
            head = roots[0].parent;
            joints = BuildJoints();
        }

        private void OnEnable()
        {
            // 숨었다 나오면(차원이동) 그동안의 움직임이 없으므로 제자리에서 다시 시작한다.
            Settle();
        }

        private void LateUpdate()
        {
            if ((head.position - lastHeadPosition).sqrMagnitude > TeleportDistance * TeleportDistance)
            {
                Settle();
            }

            lastHeadPosition = head.position;
            for (int i = 0; i < joints.Length; i++)
            {
                CarryWithBody(ref joints[i]);
            }

            unsimulated = Mathf.Min(unsimulated + Time.deltaTime, Step * MaxStepsPerFrame);
            while (unsimulated >= Step)
            {
                unsimulated -= Step;
                for (int i = 0; i < joints.Length; i++)
                {
                    Simulate(ref joints[i]);
                    PointAtTail(ref joints[i]);
                }
            }

            // 걸음이 돌지 않은 프레임(느려진 시간)에도 머리가 움직였으면 뼈를 끝점 쪽으로 다시 돌린다.
            for (int i = 0; i < joints.Length; i++)
            {
                PointAtTail(ref joints[i]);
                joints[i].LastRoot = joints[i].Bone.position;
            }
        }

        /// <summary>뼈 뿌리가 지난 프레임부터 옮겨 간 거리의 대부분만큼 끝점도 함께 옮긴다(관성은 그대로 둔다).</summary>
        private void CarryWithBody(ref Joint joint)
        {
            Vector3 carried = (joint.Bone.position - joint.LastRoot) * (1f - bodyInertia);
            joint.Tail += carried;
            joint.PreviousTail += carried;
        }

        private void Simulate(ref Joint joint)
        {
            Vector3 root = joint.Bone.position;
            Vector3 restDirection = RestRotation(joint) * joint.Axis;
            Vector3 pull = (root + restDirection * joint.Length - joint.Tail) * stiffness + Vector3.down * gravity;
            Vector3 next = joint.Tail + (joint.Tail - joint.PreviousTail) * (1f - drag) + pull * (Step * Step);

            Vector3 direction = Vector3.RotateTowards(restDirection, next - root, maxAngle * Mathf.Deg2Rad, 0f);
            next = root + direction * joint.Length;
            foreach (HairCollider ball in colliders)
            {
                float reach = ball.Radius + hairRadius;
                Vector3 fromCenter = next - ball.Center;
                if (fromCenter.sqrMagnitude < reach * reach)
                {
                    next = KeepLength(root, ball.Center + fromCenter.normalized * reach, joint.Length);
                }
            }

            joint.PreviousTail = joint.Tail;
            joint.Tail = next;
        }

        private static void PointAtTail(ref Joint joint)
        {
            Quaternion rest = RestRotation(joint);
            joint.Bone.rotation = Quaternion.FromToRotation(rest * joint.Axis, joint.Tail - joint.Bone.position) * rest;
        }

        /// <summary>모든 끝점을 지어진 자세에 멈춰 둔다(흔들림 없음).</summary>
        private void Settle()
        {
            if (joints == null)
            {
                return;
            }

            for (int i = 0; i < joints.Length; i++)
            {
                ref Joint joint = ref joints[i];
                joint.Bone.localRotation = joint.RestLocalRotation;
                joint.Tail = joint.Bone.position + joint.Bone.rotation * joint.Axis * joint.Length;
                joint.PreviousTail = joint.Tail;
                joint.LastRoot = joint.Bone.position;
            }

            lastHeadPosition = head.position;
            unsimulated = 0f;
        }

        /// <summary>이 뼈가 지어진 자세라면 지금 향할 방향(부모가 이번 프레임에 어디를 보든 따라간다).</summary>
        private static Quaternion RestRotation(Joint joint)
        {
            return joint.Bone.parent.rotation * joint.RestLocalRotation;
        }

        private static Vector3 KeepLength(Vector3 root, Vector3 tail, float length)
        {
            return root + (tail - root).normalized * length;
        }

        /// <summary>가닥을 뿌리부터 끝까지 늘어놓는다. 부모 뼈가 먼저 돌아야 자식 뼈의 자리가 맞으므로 이 순서로 흔든다.</summary>
        private Joint[] BuildJoints()
        {
            var list = new System.Collections.Generic.List<Joint>();
            foreach (Transform root in roots)
            {
                Vector3 axis = Vector3.up;
                float length = 0f;
                for (Transform bone = root; bone != null; bone = bone.childCount > 0 ? bone.GetChild(0) : null)
                {
                    // 마지막 뼈는 자식이 없으니 앞 뼈와 같은 방향·길이로 끝점을 잡는다.
                    if (bone.childCount > 0)
                    {
                        Transform child = bone.GetChild(0);
                        axis = child.localPosition.normalized;
                        length = Vector3.Distance(bone.position, child.position);
                    }

                    list.Add(new Joint { Bone = bone, RestLocalRotation = bone.localRotation, Axis = axis, Length = length });
                }
            }

            return list.ToArray();
        }
    }
}
