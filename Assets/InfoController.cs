using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
 
public class InfoController : MonoBehaviour
{
    [SerializeField] private Text _fpsText { get => GetComponent<Text>(); }
    [SerializeField] private float _hudRefreshRate = 0.1f;
 
    private float _timer;
 
    float ConverteBytesToGB(long bytes){
        return (float)bytes / 1024f / 1024f;
    }

    private void Update()
    {
        if (Time.unscaledTime > _timer)
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            float memoryUsage = ConverteBytesToGB(System.GC.GetTotalMemory(false));
            _fpsText.text = $"{fps}fps {memoryUsage}";
            _timer = Time.unscaledTime + _hudRefreshRate;
        }
    }
}