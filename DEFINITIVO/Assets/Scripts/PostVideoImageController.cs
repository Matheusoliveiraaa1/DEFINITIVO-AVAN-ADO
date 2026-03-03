using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PostVideoImageManager : MonoBehaviour
{
    [System.Serializable]
    
    public class AreaPostPrefab
    {
        public string areaName;
        public GameObject prefab;
        public AudioClip audioClip; // 🔊 NOVO
    }


    public List<AreaPostPrefab> areaPrefabs;

    [Header("Animation")]
    public float enterDuration = 1.0f;
    public float stayDuration = 7.0f;
    public float exitDuration = 1.0f;
    public float breathingAmplitude = 0.02f;
    public float breathingSpeed = 1.5f;

    [Header("Audio")]
    public AudioSource audioSource;


    private Dictionary<string, GameObject> prefabByArea;
    private GameObject currentInstance;
    private Vector2 targetPosition;
    private Coroutine animCoroutine;
    private Dictionary<string, AudioClip> audioByArea;


    private NavigationManager nav;


    private void Awake()
    {
        prefabByArea = new Dictionary<string, GameObject>();
        audioByArea = new Dictionary<string, AudioClip>();

        foreach (var a in areaPrefabs)
        {
            if (!prefabByArea.ContainsKey(a.areaName) && a.prefab != null)
                prefabByArea.Add(a.areaName, a.prefab);

            if (!audioByArea.ContainsKey(a.areaName) && a.audioClip != null)
                audioByArea.Add(a.areaName, a.audioClip);
        }

        nav = FindObjectOfType<NavigationManager>();
    }


    public void ShowForArea(string areaName)
    {
        if (!prefabByArea.ContainsKey(areaName))
            return;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = Instantiate(prefabByArea[areaName], transform);
        // 🔊 TOCAR ÁUDIO DA ÁREA
        if (audioSource != null && audioByArea.ContainsKey(areaName))
        {
            StartCoroutine(PlayPrefabAudio(audioByArea[areaName]));
        }

        RectTransform rt = currentInstance.GetComponent<RectTransform>();

        if (rt == null)
        {
            Debug.LogError("Prefab precisa ter RectTransform!");
            return;
        }

        targetPosition = rt.anchoredPosition;
        animCoroutine = StartCoroutine(Animate(rt));
    }

    private IEnumerator Animate(RectTransform rt)
    {
        rt.gameObject.SetActive(true);

        Vector2 offLeft = targetPosition + Vector2.left * Screen.width;
        Vector2 offRight = targetPosition + Vector2.right * Screen.width;

        // ENTER
        float t = 0;
        rt.anchoredPosition = offLeft;
        rt.localScale = Vector3.one;

        while (t < enterDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / enterDuration);
            rt.anchoredPosition = Vector2.Lerp(offLeft, targetPosition, p);
            yield return null;
        }

        rt.anchoredPosition = targetPosition;

        // STAY + BREATHING
        float stayTime = 0;
        Vector3 baseScale = Vector3.one;

        while (stayTime < stayDuration)
        {
            stayTime += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingAmplitude;
            rt.localScale = baseScale * pulse;
            yield return null;
        }

        // EXIT
        // EXIT (centro → esquerda)
        t = 0;
        while (t < exitDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / exitDuration);
            rt.anchoredPosition = Vector2.Lerp(targetPosition, offLeft, p);
            yield return null;
        }


        Destroy(rt.gameObject);
    }








    public void ForceHide()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
    }


    private void Update()
    {
        if (nav == null)
            return;

        if (nav.currentState != NavigationManager.AppState.Exploracao)
        {
            ForceHide();
        }
    }




    // 🔹 Adicione dentro da classe PostVideoImageManager
    public IEnumerator PlayPrefabAudio(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            yield break;

        // espera enquanto o AudioSource estiver tocando outro áudio
        while (audioSource.isPlaying)
            yield return null;

        audioSource.clip = clip;
        audioSource.Play();

        // espera o áudio terminar
        while (audioSource.isPlaying)
            yield return null;
    }











}
