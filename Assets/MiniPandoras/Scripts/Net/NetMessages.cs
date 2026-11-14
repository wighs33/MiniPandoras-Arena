using MiniPandoras.Fighters;
using Unity.Netcode;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>참가자가 누른 버튼. 판도라 칸은 「판도라 교체」(완성 뒤 추가)를 위해 메시지 모양에 처음부터 둔다.</summary>
    public enum GuestButton : byte
    {
        Attack,
        Skill,
        Pandora,
    }

    /// <summary>호스트가 네트워크 틱마다 보내는 인물 한 명의 모습. 참가자 화면은 인물을 이대로 둔다.</summary>
    public struct FighterSnapshot : INetworkSerializable
    {
        public Vector3 Position;
        // 바닥에서 돈 각도(도).
        public float Yaw;
        // 바닥을 따라 움직이는 빠르기(m/s). 걷기·달리기 섞기에 쓴다.
        public float Speed;
        public int Mana;
        public bool Hidden;
        // 차원이동으로 숨은 동안 「나갈 곳」 표시의 자리.
        public Vector3 GatePoint;
        // 이 인물을 조작하는 참가자에게서 마지막으로 받은 이동 입력의 번호(없으면 -1). 참가자가 예측한 자리를 맞출 때 쓴다.
        public int LastInput;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Yaw);
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Mana);
            serializer.SerializeValue(ref Hidden);
            serializer.SerializeValue(ref GatePoint);
            serializer.SerializeValue(ref LastInput);
        }
    }

    /// <summary>한 번 맞힌 결과(<see cref="Hit"/>)를 네트워크로 보내는 모양.</summary>
    public struct HitMessage : INetworkSerializable
    {
        private int damage;
        private bool critical;
        private bool finisher;
        private Vector3 push;
        private Vector3 source;
        private bool staggers;

        public HitMessage(Hit hit)
        {
            damage = hit.Damage;
            critical = hit.Critical;
            finisher = hit.Finisher;
            push = hit.Push;
            source = hit.Source;
            staggers = hit.Staggers;
        }

        public Hit ToHit()
        {
            return new Hit(damage, critical, finisher, push, source, staggers);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref critical);
            serializer.SerializeValue(ref finisher);
            serializer.SerializeValue(ref push);
            serializer.SerializeValue(ref source);
            serializer.SerializeValue(ref staggers);
        }
    }
}
