using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCam;
    public Transform player;
    public CanvasGroup clearUI;
    public string titleSceneName = "TitleScene";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        PlayerController controller = player.GetComponent<PlayerController>();
        controller.EnterCutsceneMode();

        // 2. 카메라 전환
        virtualCam.Follow = null;
        
        Vector3 startPos = virtualCam.transform.position;
        Quaternion startRot = virtualCam.transform.rotation;
        float startFOV = virtualCam.m_Lens.FieldOfView;

        Vector3 targetPos = new Vector3(0f, 1f, startPos.z);
        Quaternion targetRot = Quaternion.Euler(0f, 0f, 0f);
        // float targetFOV = 30f;

        float camLerpDuration = 2f;
        float camElapsed = 0f;

        while (camElapsed < camLerpDuration)
        {
            camElapsed += Time.deltaTime;
            float t = camElapsed / camLerpDuration;

            virtualCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            virtualCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            // virtualCam.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        // 3. 연출 시간 대기
        yield return new WaitForSeconds(3f);

        // 4. 화면 어둡게 + UI FadeIn
        float fadeDuration = 1.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            RenderSettings.ambientLight = Color.Lerp(Color.white, Color.black, t);
            clearUI.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        clearUI.blocksRaycasts = true;

        // 5. 씬 전환
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(titleSceneName);
    }
}