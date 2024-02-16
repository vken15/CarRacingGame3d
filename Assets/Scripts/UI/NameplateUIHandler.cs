using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NameplateUIHandler : MonoBehaviour
{
    [SerializeField] private Text nameplate;
    private Transform canvasTransform;
    private Transform carTransform;

    // Start is called before the first frame update
    private void Start()
    {
        canvasTransform = GetComponent<Transform>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (carTransform != null)
        {
            canvasTransform.position = carTransform.position;
        }
    }

    public void SetData(string name, Transform value, Color color)
    {
        nameplate.text = name;
        carTransform = value;
        nameplate.color = color;
    }
}
