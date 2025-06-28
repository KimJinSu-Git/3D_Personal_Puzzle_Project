using UnityEngine;
using UnityEngine.SceneManagement;

namespace Michsky.UI.Dark
{
    public class ExitToSystem : MonoBehaviour
    {
        public void ExitGame()
        {
            Debug.Log("Exit method is working in builds.");
            Application.Quit();
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene(0);
        }
    }
}