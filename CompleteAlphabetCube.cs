using UnityEngine;

public class CompleteAlphabetCube : MonoBehaviour
{
    public string letter = "A";
    public Vector3 cubePosition;
    public Color cubeColor = Color.red;
    public float cubeSize = 0.5f;
    public bool enableFloating = true;
    public bool enableRotation = true;
    public float rotateSpeed = 15f;

    public bool isPlaced = false;

    private GameObject cube;
    private Vector3 startPosition;
    private float floatTimer = 0f;
    private Rigidbody rb;

    void Start() { if (cube == null) Init(); }
    public void Init() { CreateCube(); startPosition = cubePosition; AddRigidbody(); }

    void CreateCube()
    {
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"Cube_{letter}";
        cube.transform.SetParent(transform);
        cube.transform.localPosition = cubePosition;
        cube.transform.localScale = Vector3.one * cubeSize;
        cube.GetComponent<Renderer>().material.color = cubeColor;

        // Front face text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(cube.transform);
        textObj.transform.localPosition = new Vector3(0, 0, cubeSize / 2 + 0.02f);
        textObj.transform.localScale = Vector3.one * 0.12f;
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = letter;
        tm.fontSize = 80;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }

    void AddRigidbody()
    {
        rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (cube == null || isPlaced) return;
        if (enableFloating)
        {
            floatTimer += Time.deltaTime * 0.8f;
            float y = startPosition.y + Mathf.Sin(floatTimer) * 0.1f;
            cube.transform.localPosition = new Vector3(cubePosition.x, y, cubePosition.z);
        }
        if (enableRotation)
            cube.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    public string GetLetter() => letter;
    public void PlaceOnAcceptor(Transform snapPoint)
    {
        if (isPlaced) return;
        isPlaced = true;
        enableFloating = enableRotation = false;
        cube.transform.position = snapPoint.position;
        cube.transform.rotation = snapPoint.rotation;
        cube.GetComponent<Collider>().enabled = false;
        rb.isKinematic = true;
        Debug.Log($"Placed {letter}");
    }
}