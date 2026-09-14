using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField] Button shopButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button dimmerButton;
        [SerializeField] CanvasGroup dimmerGroup;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] RectTransform weaponsContent;
        [SerializeField] GameObject weaponRowPrefab;
        [SerializeField] float animationDuration = 0.28f;

        readonly Subject<Unit> _openRequested = new();
        readonly Subject<Unit> _closeRequested = new();
        readonly Subject<string> _buyRequested = new();
        readonly Subject<string> _equipRequested = new();
        readonly List<WeaponRowBindings> _rows = new();

        CancellationTokenSource _animationCts;

        public IObservable<Unit> OpenRequested => _openRequested;
        public IObservable<Unit> CloseRequested => _closeRequested;
        public IObservable<string> BuyRequested => _buyRequested;
        public IObservable<string> EquipRequested => _equipRequested;

        void Awake()
        {
            if (shopButton != null)
                shopButton.onClick.AddListener(() => _openRequested.OnNext(Unit.Default));

            if (closeButton != null)
                closeButton.onClick.AddListener(() => _closeRequested.OnNext(Unit.Default));

            if (dimmerButton != null)
                dimmerButton.onClick.AddListener(() => _closeRequested.OnNext(Unit.Default));

            FitPanelToScreen();
            SetClosedImmediate();
        }

        void FitPanelToScreen()
        {
            if (panelRoot == null)
                return;

            var parent = panelRoot.parent as RectTransform;
            if (parent == null)
                return;

            var parentSize = parent.rect.size;
            if (parentSize.x < 2f || parentSize.y < 2f)
                return;

            var width = Mathf.Clamp(parentSize.x * 0.9f, 320f, 520f);
            var height = Mathf.Clamp(parentSize.y * 0.78f, 360f, 480f);
            if (width > parentSize.x - 24f)
                width = Mathf.Max(280f, parentSize.x - 24f);
            if (height > parentSize.y - 24f)
                height = Mathf.Max(300f, parentSize.y - 24f);

            panelRoot.sizeDelta = new Vector2(width, height);
        }

        public void RebuildWeapons(
            ShopWeaponEntry[] weapons,
            Func<string, bool> isOwned,
            Func<string, bool> isEquipped,
            int balance)
        {
            ClearRows();

            if (weapons == null || weaponsContent == null || weaponRowPrefab == null)
                return;

            for (var i = 0; i < weapons.Length; i++)
            {
                var entry = weapons[i];
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                {
                    Debug.LogWarning($"ShopView: skipping '{entry.DisplayName}' — prefab is missing.");
                    continue;
                }

                var rowGo = Instantiate(weaponRowPrefab, weaponsContent);
                rowGo.SetActive(true);
                var bindings = BindRow(rowGo, entry, isOwned(entry.Id), isEquipped(entry.Id), balance);
                _rows.Add(bindings);
            }
        }

        WeaponRowBindings BindRow(
            GameObject rowGo,
            ShopWeaponEntry entry,
            bool owned,
            bool equipped,
            int balance)
        {
            var nameLabel = FindText(rowGo.transform, "Name");
            var priceLabel = FindText(rowGo.transform, "Price");
            var statusLabel = FindText(rowGo.transform, "Status");
            var actionButton = FindButton(rowGo.transform, "ActionButton");
            var actionLabel = FindText(rowGo.transform, "ActionLabel");
            var iconImage = FindImage(rowGo.transform, "WeaponIcon");
            var priceIcon = FindImage(rowGo.transform, "PriceIcon");

            if (nameLabel != null)
                nameLabel.text = entry.DisplayName;

            if (iconImage != null && entry.Icon != null)
                iconImage.sprite = entry.Icon;

            var canBuy = !owned && balance >= entry.Price;
            if (priceLabel != null)
                priceLabel.text = owned ? string.Empty : $"{entry.Price}";

            if (priceIcon != null)
                priceIcon.enabled = !owned;

            if (statusLabel != null)
            {
                if (equipped)
                    statusLabel.text = "Надето";
                else if (owned)
                    statusLabel.text = "Куплено";
                else
                    statusLabel.text = "Заблокировано";
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                if (equipped)
                {
                    if (actionLabel != null)
                        actionLabel.text = "Экипировано";
                    actionButton.interactable = false;
                }
                else if (owned)
                {
                    if (actionLabel != null)
                        actionLabel.text = "Экипировать";
                    actionButton.interactable = true;
                    var id = entry.Id;
                    actionButton.onClick.AddListener(() => _equipRequested.OnNext(id));
                }
                else
                {
                    if (actionLabel != null)
                        actionLabel.text = canBuy ? "Купить" : "Нужно монет";
                    actionButton.interactable = canBuy;
                    var id = entry.Id;
                    actionButton.onClick.AddListener(() => _buyRequested.OnNext(id));
                }
            }

            return new WeaponRowBindings
            {
                Root = rowGo,
                WeaponId = entry.Id
            };
        }

        static Text FindText(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<Text>() : null;
        }

        static Button FindButton(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        static Image FindImage(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        void ClearRows()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Root != null)
                    Destroy(_rows[i].Root);
            }

            _rows.Clear();

            if (weaponsContent == null)
                return;

            for (var i = weaponsContent.childCount - 1; i >= 0; i--)
            {
                var child = weaponsContent.GetChild(i);
                if (child != null && child.name != "WeaponRowTemplate")
                    Destroy(child.gameObject);
            }
        }

        public void ShowAnimated()
        {
            FitPanelToScreen();
            AnimateOpenAsync().Forget();
        }

        public void HideAnimated() => AnimateCloseAsync().Forget();

        async UniTaskVoid AnimateOpenAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            if (dimmerGroup != null)
            {
                dimmerGroup.gameObject.SetActive(true);
                dimmerGroup.blocksRaycasts = true;
                dimmerGroup.interactable = true;
            }

            if (panelGroup != null)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.blocksRaycasts = true;
                panelGroup.interactable = true;
            }

            var duration = Mathf.Max(0.01f, animationDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                if (dimmerGroup != null)
                    dimmerGroup.alpha = Mathf.Lerp(0f, 0.72f, EaseOutCubic(t));

                if (panelGroup != null)
                    panelGroup.alpha = EaseOutCubic(t);

                if (panelRoot != null)
                {
                    // Cap overshoot so the panel never clips outside the screen.
                    var scale = Mathf.Lerp(0.86f, 1f, EaseOutCubic(t));
                    panelRoot.localScale = new Vector3(scale, scale, 1f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (dimmerGroup != null)
                dimmerGroup.alpha = 0.72f;
            if (panelGroup != null)
                panelGroup.alpha = 1f;
            if (panelRoot != null)
                panelRoot.localScale = Vector3.one;
        }

        async UniTaskVoid AnimateCloseAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            var duration = Mathf.Max(0.01f, animationDuration * 0.85f);
            var elapsed = 0f;
            var startDimmer = dimmerGroup != null ? dimmerGroup.alpha : 0f;
            var startPanel = panelGroup != null ? panelGroup.alpha : 0f;
            var startScale = panelRoot != null ? panelRoot.localScale.x : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInCubic(t);

                if (dimmerGroup != null)
                    dimmerGroup.alpha = Mathf.Lerp(startDimmer, 0f, eased);

                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(startPanel, 0f, eased);

                if (panelRoot != null)
                {
                    var scale = Mathf.Lerp(startScale, 0.88f, eased);
                    panelRoot.localScale = new Vector3(scale, scale, 1f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            SetClosedImmediate();
        }

        void SetClosedImmediate()
        {
            if (dimmerGroup != null)
            {
                dimmerGroup.alpha = 0f;
                dimmerGroup.blocksRaycasts = false;
                dimmerGroup.interactable = false;
                dimmerGroup.gameObject.SetActive(false);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable = false;
                panelGroup.gameObject.SetActive(false);
            }

            if (panelRoot != null)
                panelRoot.localScale = new Vector3(0.82f, 0.82f, 1f);
        }

        void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        void OnDestroy()
        {
            CancelAnimation();
            _openRequested.Dispose();
            _closeRequested.Dispose();
            _buyRequested.Dispose();
            _equipRequested.Dispose();
        }

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        static float EaseInCubic(float t) => t * t * t;

        struct WeaponRowBindings
        {
            public GameObject Root;
            public string WeaponId;
        }
    }
}
