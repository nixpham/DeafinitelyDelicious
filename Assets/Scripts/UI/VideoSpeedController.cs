using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoSpeedController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Text speedLabel; //shows current speed

    void Start()
    {
        //Set slider to normal speed on start
        speedSlider.value = 1f;

        //Listen for slider changes
        speedSlider.onValueChanged.AddListener(OnSliderChanged);

        UpdateLabel(1f);
    }

    void OnSliderChanged(float value)
    {
        videoPlayer.playbackSpeed = value;
        UpdateLabel(value);
    }

    void UpdateLabel(float value)
    {
        if (speedLabel != null)
            speedLabel.text = value.ToString("F2") + "x";
    }

    void OnDestroy()
    {
        speedSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}