using System;
using System.Collections.Generic;
using Aim.Models;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] CanvasGroup rootGroup;
        [SerializeField] GameObject rootPanel;
        [SerializeField] GameObject modesPanel;
        [SerializeField] GameObject setupPanel;
        [SerializeField] Button campaignButton;
        [SerializeField] Button modesButton;
        [SerializeField] Button modesBackButton;
        [SerializeField] Button setupBackButton;
        [SerializeField] Button playButton;
        [SerializeField] Button hitsMinusButton;
        [SerializeField] Button hitsPlusButton;
        [SerializeField] Button ammoMinusButton;
        [SerializeField] Button ammoPlusButton;
        [SerializeField] Transform modesGrid;
        [SerializeField] Text setupTitleText;
        [SerializeField] Text hitsValueText;
        [SerializeField] Text ammoValueText;
        [SerializeField] Slider hitsSlider;
        [SerializeField] Slider ammoSlider;
        [SerializeField] GameObject ammoRow;
        [SerializeField] GameObject modeCardPrefab;
        [SerializeField] float panelFitPadding = 0.92f;

        readonly Subject<Unit> _campaignRequested = new();
        readonly Subject<Unit> _modesRequested = new();
        readonly Subject<Unit> _modesBackRequested = new();
        readonly Subject<Unit> _setupBackRequested = new();
        readonly Subject<Unit> _playRequested = new();
        readonly Subject<LevelType> _modeSelected = new();
        readonly Subject<int> _hitsChanged = new();
        readonly Subject<int> _ammoChanged = new();

        readonly List<GameObject> _spawnedCards = new();
        RectTransform _modesPanelRect;
        RectTransform _setupPanelRect;
        Vector2 _modesPanelBaseSize;
        Vector2 _setupPanelBaseSize;

        public IObservable<Unit> CampaignRequested => _campaignRequested;
        public IObservable<Unit> ModesRequested => _modesRequested;
        public IObservable<Unit> ModesBackRequested => _modesBackRequested;
        public IObservable<Unit> SetupBackRequested => _setupBackRequested;
        public IObservable<Unit> PlayRequested => _playRequested;
        public IObservable<LevelType> ModeSelected => _modeSelected;
        public IObservable<int> HitsChanged => _hitsChanged;
        public IObservable<int> AmmoChanged => _ammoChanged;

        void Awake()
        {
            CachePanelSizes();

            if (campaignButton != null)
                campaignButton.onClick.AddListener(() => _campaignRequested.OnNext(Unit.Default));
            if (modesButton != null)
                modesButton.onClick.AddListener(() => _modesRequested.OnNext(Unit.Default));
            if (modesBackButton != null)
                modesBackButton.onClick.AddListener(() => _modesBackRequested.OnNext(Unit.Default));
            if (setupBackButton != null)
                setupBackButton.onClick.AddListener(() => _setupBackRequested.OnNext(Unit.Default));
            if (playButton != null)
                playButton.onClick.AddListener(() => _playRequested.OnNext(Unit.Default));

            if (hitsMinusButton != null)
                hitsMinusButton.onClick.AddListener(() => StepHits(-1));
            if (hitsPlusButton != null)
                hitsPlusButton.onClick.AddListener(() => StepHits(1));
            if (ammoMinusButton != null)
                ammoMinusButton.onClick.AddListener(() => StepAmmo(-1));
            if (ammoPlusButton != null)
                ammoPlusButton.onClick.AddListener(() => StepAmmo(1));

            if (hitsSlider != null)
                hitsSlider.onValueChanged.AddListener(v => _hitsChanged.OnNext(Mathf.RoundToInt(v)));
            if (ammoSlider != null)
                ammoSlider.onValueChanged.AddListener(v => _ammoChanged.OnNext(Mathf.RoundToInt(v)));
        }

        void CachePanelSizes()
        {
            _modesPanelRect = modesPanel != null ? modesPanel.GetComponent<RectTransform>() : null;
            _setupPanelRect = setupPanel != null ? setupPanel.GetComponent<RectTransform>() : null;
            if (_modesPanelRect != null)
                _modesPanelBaseSize = _modesPanelRect.sizeDelta;
            if (_setupPanelRect != null)
                _setupPanelBaseSize = _setupPanelRect.sizeDelta;
        }

        public void SetOpen(bool open)
        {
            if (rootGroup == null)
            {
                gameObject.SetActive(open);
                return;
            }

            rootGroup.gameObject.SetActive(open);
            rootGroup.alpha = open ? 1f : 0f;
            rootGroup.blocksRaycasts = open;
            rootGroup.interactable = open;
            if (open)
                FitPanelsToScreen();
        }

        public void SetScreen(MainMenuScreen screen)
        {
            FitPanelsToScreen();
            if (rootPanel != null)
                rootPanel.SetActive(screen == MainMenuScreen.Root);
            if (modesPanel != null)
                modesPanel.SetActive(screen == MainMenuScreen.Modes);
            if (setupPanel != null)
                setupPanel.SetActive(screen == MainMenuScreen.Setup);
        }

        public void BindModeCards(IReadOnlyList<(LevelType type, string title, Sprite icon)> modes)
        {
            ClearCards();
            if (modesGrid == null || modeCardPrefab == null || modes == null)
                return;

            for (var i = 0; i < modes.Count; i++)
            {
                var mode = modes[i];
                var card = Instantiate(modeCardPrefab, modesGrid);
                card.SetActive(true);
                _spawnedCards.Add(card);

                var title = card.transform.Find("Title")?.GetComponent<Text>();
                if (title != null)
                    title.text = mode.title;

                var cover = card.transform.Find("Cover")?.GetComponent<Image>();
                if (cover != null)
                {
                    cover.sprite = mode.icon;
                    cover.preserveAspect = false;
                    cover.color = Color.white;
                }

                var button = card.GetComponent<Button>();
                if (button != null)
                {
                    var captured = mode.type;
                    button.onClick.AddListener(() => _modeSelected.OnNext(captured));
                }
            }
        }

        public void BindSetup(string title, int hits, int ammo, bool showAmmo, int hitsMin, int hitsMax, int ammoMin, int ammoMax)
        {
            if (setupTitleText != null)
                setupTitleText.text = title;

            if (ammoRow != null)
                ammoRow.SetActive(showAmmo);

            if (hitsSlider != null)
            {
                hitsSlider.minValue = hitsMin;
                hitsSlider.maxValue = hitsMax;
                hitsSlider.wholeNumbers = true;
                hitsSlider.SetValueWithoutNotify(Mathf.Clamp(hits, hitsMin, hitsMax));
            }

            if (ammoSlider != null)
            {
                ammoSlider.minValue = ammoMin;
                ammoSlider.maxValue = ammoMax;
                ammoSlider.wholeNumbers = true;
                ammoSlider.SetValueWithoutNotify(Mathf.Clamp(ammo, ammoMin, ammoMax));
            }

            SetHitsLabel(hits);
            SetAmmoLabel(ammo);
        }

        public void SetHitsLabel(int value)
        {
            if (hitsValueText != null)
                hitsValueText.text = value.ToString();
            if (hitsSlider != null && Mathf.RoundToInt(hitsSlider.value) != value)
                hitsSlider.SetValueWithoutNotify(value);
        }

        public void SetAmmoLabel(int value)
        {
            if (ammoValueText != null)
                ammoValueText.text = value.ToString();
            if (ammoSlider != null && Mathf.RoundToInt(ammoSlider.value) != value)
                ammoSlider.SetValueWithoutNotify(value);
        }

        void StepHits(int delta)
        {
            if (hitsSlider == null)
                return;
            var next = Mathf.Clamp(
                Mathf.RoundToInt(hitsSlider.value) + delta,
                Mathf.RoundToInt(hitsSlider.minValue),
                Mathf.RoundToInt(hitsSlider.maxValue));
            hitsSlider.value = next;
        }

        void StepAmmo(int delta)
        {
            if (ammoSlider == null)
                return;
            var next = Mathf.Clamp(
                Mathf.RoundToInt(ammoSlider.value) + delta,
                Mathf.RoundToInt(ammoSlider.minValue),
                Mathf.RoundToInt(ammoSlider.maxValue));
            ammoSlider.value = next;
        }

        void FitPanelsToScreen()
        {
            var rootRect = transform as RectTransform;
            if (rootRect == null)
                return;

            var canvasSize = rootRect.rect.size;
            if (canvasSize.x < 1f || canvasSize.y < 1f)
                return;

            FitPanel(_modesPanelRect, _modesPanelBaseSize, canvasSize);
            FitPanel(_setupPanelRect, _setupPanelBaseSize, canvasSize);
        }

        void FitPanel(RectTransform panel, Vector2 baseSize, Vector2 canvasSize)
        {
            if (panel == null || baseSize.x < 1f || baseSize.y < 1f)
                return;

            var scale = Mathf.Min(
                1f,
                (canvasSize.x * panelFitPadding) / baseSize.x,
                (canvasSize.y * panelFitPadding) / baseSize.y);
            panel.localScale = new Vector3(scale, scale, 1f);
        }

        void ClearCards()
        {
            for (var i = 0; i < _spawnedCards.Count; i++)
            {
                if (_spawnedCards[i] != null)
                    Destroy(_spawnedCards[i]);
            }

            _spawnedCards.Clear();
        }

        void OnDestroy()
        {
            ClearCards();
            _campaignRequested.Dispose();
            _modesRequested.Dispose();
            _modesBackRequested.Dispose();
            _setupBackRequested.Dispose();
            _playRequested.Dispose();
            _modeSelected.Dispose();
            _hitsChanged.Dispose();
            _ammoChanged.Dispose();
        }
    }
}
