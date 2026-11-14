using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniPandoras.Input
{
    /// <summary>
    /// Input System과 게임 코드 사이의 경계. 기기를 플레이어의 의도로만 바꾼다(무엇을 할지는 정하지 않는다).
    /// 화면 조작(가상 게임패드), 게임패드, 키보드가 같은 액션으로 들어오므로, 게임 코드는 입력이 어디서 왔는지 모른다.
    /// 2D판(MiniPandoras-2D)의 같은 클래스를 복사했다. 대전에는 말 걸기와 동료가 없어서 행동 버튼을 공격으로 바꾸고 동료 칸을 뺐다.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference attack;
        [Tooltip("판도라 스킬 칸 순서대로")]
        [SerializeField] private InputActionReference[] skills = Array.Empty<InputActionReference>();
        [Tooltip("판도라 칸 순서대로")]
        [SerializeField] private InputActionReference[] pandoras = Array.Empty<InputActionReference>();

        private Action<InputAction.CallbackContext>[] skillHandlers = Array.Empty<Action<InputAction.CallbackContext>>();
        private Action<InputAction.CallbackContext>[] pandoraHandlers = Array.Empty<Action<InputAction.CallbackContext>>();

        /// <summary>조이스틱 방향. 길이는 0~1.</summary>
        public Vector2 Move => move.action.ReadValue<Vector2>();

        /// <summary>공격 버튼. 누를 때마다 3연격의 다음 타가 된다.</summary>
        public event Action AttackPressed;

        /// <summary>판도라 스킬 칸(0부터).</summary>
        public event Action<int> SkillPressed;

        /// <summary>판도라 칸(0부터). 그 칸의 판도라로 바꾼다.</summary>
        public event Action<int> PandoraPressed;

        private void OnEnable()
        {
            move.action.Enable();
            Listen(attack, OnAttack);
            skillHandlers = ListenToSlots(skills, slot => SkillPressed?.Invoke(slot));
            pandoraHandlers = ListenToSlots(pandoras, slot => PandoraPressed?.Invoke(slot));
        }

        // 끄면 조이스틱도 0으로 읽힌다(방 코드를 치는 동안 WASD가 이동으로 들어가지 않게 끈다).
        private void OnDisable()
        {
            move.action.Disable();
            attack.action.performed -= OnAttack;
            StopListening(skills, skillHandlers);
            StopListening(pandoras, pandoraHandlers);
        }

        /// <summary>칸마다 누르면 그 칸 번호를 알리게 한다. 돌려준 처리기는 끊을 때 쓴다.</summary>
        private static Action<InputAction.CallbackContext>[] ListenToSlots(InputActionReference[] slots, Action<int> pressed)
        {
            var handlers = new Action<InputAction.CallbackContext>[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                handlers[i] = context => pressed(slot);
                Listen(slots[i], handlers[i]);
            }

            return handlers;
        }

        private static void StopListening(InputActionReference[] slots, Action<InputAction.CallbackContext>[] handlers)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].action.performed -= handlers[i];
            }
        }

        private static void Listen(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            AttackPressed?.Invoke();
        }
    }
}
