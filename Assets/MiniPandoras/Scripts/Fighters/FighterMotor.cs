using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>인물의 몸을 바닥 위에서 움직이고 돌린다. 무엇을 할지는 Fighter가 정한다.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FighterMotor : MonoBehaviour
    {
        // 땅에 붙어 있게 늘 조금 아래로 누른다(내리막에서 뜨지 않게).
        private const float GroundStick = 2f;

        [SerializeField] private FighterTuning tuning;
        [SerializeField] private CharacterController body;

        private Vector3 velocity;
        private float fallSpeed;
        // 온라인 참가자 화면: 몸을 스스로 움직이지 않고 호스트가 알려 준 자리에 둔다.
        private bool followsHost;
        private float hostSpeed;

        /// <summary>지금 바닥을 따라 움직이는 빠르기(m/s).</summary>
        public float Speed => followsHost ? hostSpeed : velocity.magnitude;

        /// <summary>
        /// 이제부터 걷기·내딛기·밀려나기로 몸을 옮기지 않고 <see cref="Place"/>로 받은 자리에 둔다(온라인 참가자 화면).
        /// 그 움직임은 호스트가 이미 했다. 그동안 CharacterController는 끈다: 켜 두면 자리를 옮길 때마다 두 몸의 겹침을 풀려고
        /// 조금씩 밀어내서, 받은 자리 사이에서도 매 프레임 몸이 떨렸다.
        /// </summary>
        public void FollowHost(bool follow)
        {
            followsHost = follow;
            velocity = Vector3.zero;
            body.enabled = !follow;
        }

        /// <summary>그만큼 몸을 옮긴다(벽은 뚫지 않는다). 예측한 자리를 호스트의 자리에 맞춰 갈 때 쓴다.</summary>
        public void Nudge(Vector3 offset)
        {
            if (body.enabled)
            {
                body.Move(offset);
            }
        }

        /// <summary>호스트가 알려 준 자리·방향(바닥에서 돈 각도)·빠르기로 몸을 둔다.</summary>
        public void Place(Vector3 position, float yaw, float speed)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            hostSpeed = speed;
        }

        /// <summary>그 방향(월드, 길이 0~1)으로 달린다. 길이가 짧으면 걷는다.</summary>
        public void Run(Vector3 direction, float deltaTime)
        {
            if (followsHost)
            {
                return;
            }

            Vector3 target = direction * tuning.RunSpeed;
            float rate = target.sqrMagnitude > velocity.sqrMagnitude ? tuning.Acceleration : tuning.Deceleration;
            velocity = Vector3.MoveTowards(velocity, target, rate * deltaTime);
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, tuning.TurnSpeed * deltaTime);
            }

            Step(deltaTime);
        }

        /// <summary>몸 반지름(m). 공격이 닿는지 잴 때 쓴다.</summary>
        public float Radius => body.radius;

        /// <summary>바라보는 쪽으로 그 빠르기(m/s)로 내딛는다. 공격 중에 쓴다.</summary>
        public void Lunge(float speed, float deltaTime)
        {
            Slide(transform.forward * speed, deltaTime);
        }

        /// <summary>돌지 않고 그 속도(m/s)로 미끄러진다. 맞아서 밀려날 때 쓴다(0이면 제자리).</summary>
        public void Slide(Vector3 newVelocity, float deltaTime)
        {
            if (followsHost)
            {
                return;
            }

            velocity = newVelocity;
            Step(deltaTime);
        }

        /// <summary>움직임을 멈추고 그 자리로 옮긴다(새 판을 시작할 때).</summary>
        public void Teleport(Vector3 position, Quaternion facing)
        {
            // CharacterController가 켜져 있으면 transform을 바로 옮겨도 다음 Move에서 원래 자리로 돌아간다.
            body.enabled = false;
            transform.SetPositionAndRotation(position, facing);
            body.enabled = !followsHost;
            velocity = Vector3.zero;
            fallSpeed = 0f;
        }

        /// <summary>다른 인물의 몸과 부딪히지 않게(또는 다시 부딪히게) 한다.</summary>
        public void PassThrough(FighterMotor other, bool pass)
        {
            Physics.IgnoreCollision(body, other.body, pass);
        }

        /// <summary>몸이 부딪히는지. 숨은 동안(차원이동) 끈다. 끈 동안에는 움직이지 않는다.</summary>
        public void SetSolid(bool solid)
        {
            body.enabled = solid && !followsHost;
        }

        /// <summary>그 방향을 곧바로 바라본다.</summary>
        public void Face(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Step(float deltaTime)
        {
            if (body.isGrounded && fallSpeed < 0f)
            {
                fallSpeed = -GroundStick;
            }

            fallSpeed -= tuning.Gravity * deltaTime;
            body.Move((velocity + Vector3.up * fallSpeed) * deltaTime);
        }
    }
}
