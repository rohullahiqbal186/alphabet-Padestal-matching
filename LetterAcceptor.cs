using UnityEngine;
using System.Reflection;

public class LetterAcceptor : MonoBehaviour
{
    public char requiredLetter = 'A';
    public Color successColor = Color.green;
    public Vector3 snapLocalOffset = new Vector3(0, 0.5f, 0);
    public AudioClip successSound;

    private Transform snapPoint;
    private bool isOccupied = false;
    private Renderer pedestalRenderer;
    private AudioSource audioSource;
    private Color originalColor;

    void Start()
    {
        pedestalRenderer = GetComponentInChildren<Renderer>();
        if (pedestalRenderer != null) originalColor = pedestalRenderer.material.color;

        GameObject snap = new GameObject("SnapPoint");
        snap.transform.SetParent(transform);
        snap.transform.localPosition = snapLocalOffset;
        snapPoint = snap.transform;

        GameObject trigger = new GameObject("Trigger");
        trigger.transform.SetParent(transform);
        trigger.transform.localPosition = new Vector3(0, 1f, 0);
        SphereCollider col = trigger.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.2f;

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isOccupied) return;

        CompleteAlphabetCube cube = other.GetComponentInParent<CompleteAlphabetCube>();
        if (cube != null && cube.GetLetter() == requiredLetter.ToString())
        {
            cube.PlaceOnAcceptor(snapPoint);
            isOccupied = true;
            if (pedestalRenderer != null) pedestalRenderer.material.color = successColor;
            if (successSound != null) audioSource.PlayOneShot(successSound);
            Debug.Log($"Placed {cube.GetLetter()} on {requiredLetter}");
            return;
        }

        MonoBehaviour[] scripts = other.GetComponentsInParent<MonoBehaviour>();
        foreach (var script in scripts)
        {
            MethodInfo get = script.GetType().GetMethod("GetLetter");
            MethodInfo place = script.GetType().GetMethod("PlaceOnAcceptor");
            if (get != null && place != null)
            {
                string letter = get.Invoke(script, null) as string;
                if (letter == requiredLetter.ToString())
                {
                    place.Invoke(script, new object[] { snapPoint });
                    isOccupied = true;
                    if (pedestalRenderer != null) pedestalRenderer.material.color = successColor;
                    if (successSound != null) audioSource.PlayOneShot(successSound);
                    return;
                }
            }
        }
    }
}