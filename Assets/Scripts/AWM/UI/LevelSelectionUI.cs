using AWM.System;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    [SerializeField]
    private GameObject headerBackground;

    [SerializeField]
    private GameObject headerText;

    [SerializeField]
    private Image musicToggleImage;

    [SerializeField]
    private Image sfxToggleImage;

    [SerializeField]
    private Sprite musicOnSprite;

    [SerializeField]
    private Sprite musicOffSprite;

    [SerializeField]
    private Sprite sfxOnSprite;

    [SerializeField]
    private Sprite sfxOffSprite;

    private void Start()
    {
        musicToggleImage.sprite = Root.Instance.AudioManager.MusicIsOn ? musicOnSprite : musicOffSprite;
        sfxToggleImage.sprite = Root.Instance.AudioManager.SFXIsOn ? sfxOnSprite : sfxOffSprite;
    }

    public void ChangeVisibilityOfHeader(bool show)
    {
        headerBackground.SetActive(show);
        headerText.SetActive(show);
    }

    public void ToggleMusic()
    {
        musicToggleImage.sprite = Root.Instance.AudioManager.ToggleMusic() ? musicOnSprite : musicOffSprite;
    }

    public void ToggleSFX()
    {
        sfxToggleImage.sprite = Root.Instance.AudioManager.ToggleSFX() ? sfxOnSprite : sfxOffSprite;
    }
}
