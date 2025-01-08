using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;

using System;

namespace FUI.Samples
{
    [Config(Layer.Common, Attributes.FullScreen)]
    [Binding("LoginView")]
    public class LoginViewModel : ViewModel
    {
        [Binding("userName", nameof(InputFieldElement.Text), bindingMode: BindingMode.OneWayToSource)]
        public string UserName { get; set; }

        [Binding("password", nameof(InputFieldElement.Text), bindingMode: BindingMode.OneWayToSource)]
        public string Password { get; set; }

        [Command("login", nameof(ButtonElement.OnClick))]
        public event Action OnLogin;
    }

    public class LoginViewBehavior : ViewBehavior<LoginViewModel>
    {
        protected override void OnOpen(object param)
        {
            VM.OnLogin += OnLogin;
        }

        protected override void OnClose()
        {
            VM.OnLogin -= OnLogin;
        }

        void OnLogin()
        {
            if(VM.Password == "12345" && VM.UserName == "fui")
            {
                // 登录成功 打开主界面
                TestLauncher.Instance.UIManager.Open("HomeView");
            }
            else
            {
                //打开提示窗口
                TestLauncher.Instance.UIManager.Open("TipsView", new TipsData("登录失败", "用户名或密码错误"));
            }
        }
    }
}