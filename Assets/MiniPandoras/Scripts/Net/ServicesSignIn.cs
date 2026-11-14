using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace MiniPandoras.Net
{
    /// <summary>
    /// Unity 서비스(Relay 방, 순위표, Cloud Code)에 익명으로 로그인한다. 이미 했으면 바로 끝난다.
    /// 처음 로그인한 PC는 그 뒤로도 같은 플레이어 ID로 들어간다(인증 패키지가 이 PC에 기억한다).
    /// </summary>
    public static class ServicesSignIn
    {
        // 진행 중인(또는 끝난) 로그인. 둘이 거의 같은 때 부르면 같은 로그인을 함께 기다린다.
        // 따로 두 번 시작하면 한쪽이 초기화가 끝나기 전에 서비스를 불러 「초기화되지 않았다」 오류가 난다.
        private static Task signingIn;

        public static Task SignInAsync()
        {
            bool failedBefore = signingIn != null && (signingIn.IsFaulted || signingIn.IsCanceled);
            if (signingIn == null || failedBefore)
            {
                signingIn = SignInOnceAsync();
            }

            return signingIn;
        }

        private static async Task SignInOnceAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
