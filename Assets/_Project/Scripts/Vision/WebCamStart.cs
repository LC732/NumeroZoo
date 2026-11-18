using UnityEngine;

public class WebCamStart : MonoBehaviour
{

    [SerializeField] private Material webcamMaterial;
    [SerializeField] private string webcamName = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        Debug.Log("Cameras:");
        foreach (WebCamDevice webcam in devices){
            Debug.Log(webcam.name);
        }

        WebCamTexture webcamTexture = new(devices[0].name, 1280, 720, 30);

        webcamMaterial.mainTexture = webcamTexture;

        webcamTexture.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
