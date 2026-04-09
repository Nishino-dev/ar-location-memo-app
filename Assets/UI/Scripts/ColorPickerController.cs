using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

public class ColorPickerController : MonoBehaviour
{
    private VisualElement _pickerRoot;
    private VisualElement _container;
    private VisualElement _previewBox;
    private SliderInt _rSlider, _gSlider, _bSlider;
    private Slider _sSlider;
    private Button _closeBtn;
    private Button _randomBtn;
    private Button _addpresetBtn;
    private VisualElement _presetScroll;

    private VisualElement _colorPopup;
    private VisualElement _popupColorPreview;
    private Label _popupHexLabel;
    private Button _popupDeleteBtn;
    private Button _popupCloseBtn;

    private Color _selectedColorForPopup;
    private VisualElement _selectedDotForPopup;
    private List<Color> _currentPresets = new List<Color>();
    private string _saveKey = "UserColorPresets";

    public Action<Color> OnColorConfirmed;

    public void Setup(VisualElement root)
    {
        if (_container != null) return;

        _container = root.Q<VisualElement>("ColorPickerContainer");
        _pickerRoot = root.Q<VisualElement>("PickerRoot");
        _previewBox = root.Q<VisualElement>("ColorPreview");
        _presetScroll = root.Q<VisualElement>("PresetScroll");

        _rSlider = root.Q<SliderInt>("R_Slider");
        _gSlider = root.Q<SliderInt>("G_Slider");
        _bSlider = root.Q<SliderInt>("B_Slider");
        _sSlider = root.Q<Slider>("SaturationSlider");

        _closeBtn = root.Q<Button>("CloseButton");
        _randomBtn = root.Q<Button>("RandomButton");
        _addpresetBtn = root.Q<Button>("AddPresetButton");

        _colorPopup = root.Q<VisualElement>("Picker_ColorPopup");
        _popupColorPreview = root.Q<VisualElement>("Popup_ColorPreview");
        _popupHexLabel = root.Q<Label>("Popup_HexLabel");
        _popupDeleteBtn = root.Q<Button>("Popup_DeleteButton");
        _popupCloseBtn = root.Q<Button>("Popup_CloseButton");

        _rSlider.RegisterValueChangedCallback(_ => UpdatePreview());
        _gSlider.RegisterValueChangedCallback(_ => UpdatePreview());
        _bSlider.RegisterValueChangedCallback(_ => UpdatePreview());
        _sSlider.RegisterValueChangedCallback(_ => UpdatePreview());

        _randomBtn.clicked += () => {
            SetColorToSliders(Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));
        };

        _addpresetBtn.clicked += () => {
            Color currentColor = _previewBox.resolvedStyle.backgroundColor;

            string currentHex = "#" + ColorUtility.ToHtmlStringRGB(currentColor);
            bool alreadyExists = _currentPresets.Any(c => "#" + ColorUtility.ToHtmlStringRGB(c) == currentHex);

            if (!alreadyExists)
            {
                CreateAndAddPreset(currentColor);
                SavePresets();
            }
        };

        _closeBtn.clicked += () => {
            _container.style.display = DisplayStyle.None;
            OnColorConfirmed?.Invoke(_previewBox.style.backgroundColor.value);
        };

        _popupCloseBtn.clicked += () => _colorPopup.style.display = DisplayStyle.None;
        _popupDeleteBtn.clicked += () => {
            DeletePreset(_selectedColorForPopup, _selectedDotForPopup);
            _colorPopup.style.display = DisplayStyle.None;
        };

        LoadPresets();
        if (_currentPresets.Count == 0)
        {
            CreateAndAddPreset(Color.black);
            CreateAndAddPreset(Color.white);
            CreateAndAddPreset(new Color(1f, 0.95f, 0.66f));
        }

        _container.style.display = DisplayStyle.None;
    }

    public void Open(Color initialColor)
    {
        if (_pickerRoot == null) Setup(GetComponent<UIDocument>().rootVisualElement);

        _rSlider.value = Mathf.RoundToInt(initialColor.r * 255f);
        _gSlider.value = Mathf.RoundToInt(initialColor.g * 255f);
        _bSlider.value = Mathf.RoundToInt(initialColor.b * 255f);
        _sSlider.value = 1.0f;

        UpdatePreview();
        _container.style.display = DisplayStyle.Flex;
    }

    private void UpdatePreview()
    {
        Color baseColor = new Color(_rSlider.value / 255f, _gSlider.value / 255f, _bSlider.value / 255f);
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        s = Mathf.Clamp01(s * _sSlider.value);
        _previewBox.style.backgroundColor = Color.HSVToRGB(h, s, v);
    }

    private void SetColorToSliders(Color color)
    {
        _rSlider.value = Mathf.RoundToInt(color.r * 255f);
        _gSlider.value = Mathf.RoundToInt(color.g * 255f);
        _bSlider.value = Mathf.RoundToInt(color.b * 255f);
        UpdatePreview();
    }

    private void CreateAndAddPreset(Color color)
    {
        string targetHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        if (_currentPresets.Any(c => "#" + ColorUtility.ToHtmlStringRGB(c) == targetHex))
        {
            return;
        }

        _currentPresets.Add(color);
        CreatePresetDot(color);

        if (_presetScroll is ScrollView scrollView)
        {
            scrollView.schedule.Execute(() => {
                scrollView.scrollOffset = new Vector2(scrollView.horizontalScroller.highValue, 0);
            }).StartingIn(10);
        }
    }

    private void CreatePresetDot(Color color)
    {
        VisualElement dot = new VisualElement();
        dot.style.width = 60;
        dot.style.height = 60;
        dot.style.borderTopLeftRadius = 30;
        dot.style.borderTopRightRadius = 30;
        dot.style.borderBottomLeftRadius = 30;
        dot.style.borderBottomRightRadius = 30;
        dot.style.borderLeftWidth = 1;
        dot.style.borderRightWidth = 1;
        dot.style.borderTopWidth = 1;
        dot.style.borderBottomWidth = 1;

        Color bColor = new Color(0.86f, 0.86f, 0.86f);
        dot.style.borderTopColor = bColor;
        dot.style.borderBottomColor = bColor;
        dot.style.borderLeftColor = bColor;
        dot.style.borderRightColor = bColor;

        dot.style.backgroundColor = color;
        dot.style.marginTop = 10;
        dot.style.marginLeft = 10;
        dot.style.marginRight = 10;
        dot.style.marginBottom = 10;
        dot.pickingMode = PickingMode.Position;

        IVisualElementScheduledItem longPressTask = null;

        dot.RegisterCallback<ClickEvent>(evt => SetColorToSliders(color));

        dot.RegisterCallback<PointerDownEvent>(evt => {
            longPressTask = dot.schedule.Execute(() => ShowPopup(color, dot)).StartingIn(600);
        });

        dot.RegisterCallback<PointerUpEvent>(evt => {
            longPressTask?.Pause();
            longPressTask = null;
            if (_colorPopup.style.display == DisplayStyle.None) SetColorToSliders(color);
        });

        dot.RegisterCallback<PointerLeaveEvent>(evt => {
            longPressTask?.Pause();
            longPressTask = null;
        });

        _presetScroll.Add(dot);
    }

    private void ShowPopup(Color color, VisualElement targetDot)
    {
        _selectedColorForPopup = color;
        _selectedDotForPopup = targetDot;
        _popupColorPreview.style.backgroundColor = color;
        _popupHexLabel.text = "#" + ColorUtility.ToHtmlStringRGB(color);
        _colorPopup.style.display = DisplayStyle.Flex;
    }

    private void DeletePreset(Color color, VisualElement dotElement)
    {
        _currentPresets.Remove(color);
        dotElement.RemoveFromHierarchy();
        SavePresets();
    }

    private void SavePresets()
    {
        string data = string.Join(",", _currentPresets.Select(c => "#" + ColorUtility.ToHtmlStringRGB(c)));
        PlayerPrefs.SetString(_saveKey, data);
        PlayerPrefs.Save();
    }

    private void LoadPresets()
    {
        if (!PlayerPrefs.HasKey(_saveKey)) return;
        string data = PlayerPrefs.GetString(_saveKey);
        if (string.IsNullOrEmpty(data)) return;

        foreach (var hex in data.Split(','))
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color loadedColor)) CreateAndAddPreset(loadedColor);
        }
    }
}