using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ParticleTransition : MonoBehaviour
{
    [SerializeField] private ParticleSystem transitionParticles;
    [SerializeField] private float delayBeforeLoad = 1.5f; // Wait until particles finish
    [SerializeField] private string nextSceneName = "BeforeStartingScreen";
    [SerializeField] private AudioClip transitionSound;
    private AudioSource audioSource;


    private void Awake()
    {
        if (transitionParticles == null)
            transitionParticles = GetComponent<ParticleSystem>();

        audioSource = GetComponent<AudioSource>();
    }

    public void StartParticleTransition()
    {
        if (transitionParticles != null)
        {
            transitionParticles.Play(); // Trigger the burst
            StartCoroutine(LoadSceneAfterParticles());
        }
        else
        {
            Debug.LogError("Particle System not assigned!");
            SceneManager.LoadScene(nextSceneName); // Fallback
        }

        if (transitionParticles != null)
        {
            transitionParticles.Play();
            if (audioSource && transitionSound)
                audioSource.PlayOneShot(transitionSound);
            StartCoroutine(LoadSceneAfterParticles());
        }
    }

    private IEnumerator LoadSceneAfterParticles()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(nextSceneName);
    }
}