using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Gate Open & Bloom")]
    public Transform doorPivot; // 문 회전 축
    public Volume brightGateVolume;
    
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

        // 1. 카메라 Follow 해제
        virtualCam.Follow = null;

        Vector3 startPos = virtualCam.transform.position;
        Quaternion startRot = virtualCam.transform.rotation;
        float startFOV = virtualCam.m_Lens.FieldOfView;

        Vector3 targetPos = new Vector3(0f, 2f, startPos.z - 5f); // Z 값도 점점 뒤로
        Quaternion targetRot = Quaternion.Euler(5f, 0f, 0f);
        float targetFOV = 30f;

        float camLerpDuration = 2f;
        float camElapsed = 0f;

        while (camElapsed < camLerpDuration)
        {
            camElapsed += Time.deltaTime;
            float t = camElapsed / camLerpDuration;

            virtualCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            virtualCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            virtualCam.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }
        
        float effectLerpDuration = 2f;
        float effectElapsed = 0f;

        Quaternion doorStartRot = Quaternion.Euler(0f, 0f, 0f);
        Quaternion doorTargetRot = Quaternion.Euler(0f, -90f, 0f);

        VolumeProfile profile = brightGateVolume.profile;
        profile.TryGet(out Bloom bloom);
        float bloomStart = 0.5f;
        float bloomTarget = 3f;

        while (effectElapsed < effectLerpDuration)
        {
            effectElapsed += Time.deltaTime;
            float t = effectElapsed / effectLerpDuration;

            doorPivot.rotation = Quaternion.Slerp(doorStartRot, doorTargetRot, t);

            if (bloom != null)
                bloom.intensity.value = Mathf.Lerp(bloomStart, bloomTarget, t);

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        float fadeDuration = 1.5f;
        float elapsed = 0f;
        Transform uiText = clearUI.transform.GetChild(0);
        Vector3 originalScale = uiText.localScale;
        Vector3 targetScale = originalScale * 1.15f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            RenderSettings.ambientLight = Color.Lerp(Color.white, Color.black, t);
            clearUI.alpha = Mathf.Lerp(0f, 1f, t);
            uiText.localScale = Vector3.Lerp(originalScale, targetScale, t);

            yield return null;
        }

        clearUI.blocksRaycasts = true;

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(titleSceneName);
    }
}