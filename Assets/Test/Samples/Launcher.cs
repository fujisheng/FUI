using FUI.Test;

using UnityEngine;

public class Launcher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestLauncher.Instance.UIManager.OpenAsync("LoginView");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
