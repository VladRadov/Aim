using System.Collections.Generic;
using Aim;
using Aim.Config;
using Aim.Installers;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class ShopUiSetup
    {
        const string CatalogPath = "Assets/Aim/Resources/WeaponShopCatalog.asset";
        const string ShopIconPath = "Assets/Aim/Sprites/shop_icon.png";
        const string CoinIconPath = "Assets/Aim/Sprites/coin_icon.png";
        const string WeaponIconsFolder = "Assets/Aim/Sprites/ShopWeapons";
        const string BaseWeaponsFolder = "Assets/Low Poly Pistol Weapon Pack 3/Prefabs/Weapons";
        const string PreSetWeaponsFolder = "Assets/Low Poly Pistol Weapon Pack 3/Prefabs/Weapons/Pistol_PreSet";

        static readonly Color IconCircle = new(0.16f, 0.24f, 0.36f, 1f);
        static readonly Color IconCircleBorder = new(0.40f, 0.70f, 0.95f, 0.85f);
        static readonly Color PanelBody = new(0.09f, 0.11f, 0.16f, 0.98f);
        static readonly Color PanelBorder = new(0.32f, 0.62f, 0.92f, 0.55f);
        static readonly Color PanelShadow = new(0f, 0f, 0f, 0.55f);
        static readonly Color HeaderBar = new(0.14f, 0.22f, 0.34f, 1f);
        static readonly Color Accent = new(0.38f, 0.78f, 1f, 1f);
        static readonly Color SoftText = new(0.78f, 0.84f, 0.92f, 1f);
        static readonly Color RowCard = new(0.13f, 0.17f, 0.24f, 0.95f);
        static readonly Color DimmerColor = new(0.02f, 0.04f, 0.08f, 0.78f);
        static readonly Color ButtonNormal = new(0.20f, 0.42f, 0.68f, 1f);
        static readonly Color ButtonOwned = new(0.18f, 0.55f, 0.42f, 1f);

        [MenuItem("Aim/Rebuild Shop UI")]
        public static void RebuildShopUi()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene or create a Canvas first.");
                return;
            }

            EnsureEventSystem();
            EnsureSpriteImport(ShopIconPath);
            EnsureWeaponIconImports();
            var catalog = EnsureCatalog();

            // Keep settings icon at top-right; place shop to its left.
            var settings = Object.FindAnyObjectByType<SettingsView>();
            if (settings != null)
            {
                var settingsButton = settings.transform.Find("SettingsButton") as RectTransform;
                if (settingsButton != null)
                    settingsButton.anchoredPosition = new Vector2(-18f, -18f);
            }

            var existing = Object.FindAnyObjectByType<ShopView>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var shop = CreateShopUi(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                SetObjectField(bootstrap, "shopView", shop);
                SetObjectField(bootstrap, "weaponShopCatalog", catalog);
            }

            var installer = Object.FindAnyObjectByType<GameInstaller>();
            if (installer != null)
                SetObjectField(installer, "weaponShopCatalog", catalog);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = shop.gameObject;
            var weaponCount = catalog != null && catalog.Weapons != null ? catalog.Weapons.Length : 0;
            Debug.Log($"Shop UI rebuilt. Catalog: {CatalogPath} ({weaponCount} weapons). Save the scene.");
        }

        static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                    Object.DestroyImmediate(oldModule);
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static void EnsureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            var dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        static void EnsureWeaponIconImports()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { WeaponIconsFolder });
            for (var i = 0; i < guids.Length; i++)
                EnsureSpriteImport(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        static Sprite LoadWeaponIcon(string letter, bool isPreset = false)
        {
            if (isPreset)
            {
                var presetIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponIconsFolder}/Pistol_{letter}_PreSet.png");
                if (presetIcon != null)
                    return presetIcon;
            }

            var processed = AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponIconsFolder}/Pistol_{letter}.png");
            if (processed != null)
                return processed;

            // Fallback: pack UI with materials (_1K_2), not the flat grey _1K_1 previews.
            var packPath = $"Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_{letter}_1K_2.png";
            EnsureSpriteImport(packPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(packPath);
        }

        static Sprite CapturePresetIcon(GameObject prefab, string letter)
        {
            if (prefab == null)
                return LoadWeaponIcon(letter);

            EnsureShopWeaponsFolder();
            var savePath = $"{WeaponIconsFolder}/Pistol_{letter}_PreSet.png";

            var rendered = RenderPrefabToTransparentPng(prefab, 512);
            if (rendered == null)
            {
                Debug.LogWarning($"Failed to render PreSet icon for {prefab.name}, falling back to AssetPreview.");
                rendered = CaptureAssetPreviewTransparent(prefab);
            }

            if (rendered == null)
                return LoadWeaponIcon(letter);

            var absolute = ToAbsolutePath(savePath);
            System.IO.File.WriteAllBytes(absolute, rendered.EncodeToPNG());
            Object.DestroyImmediate(rendered);

            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
            EnsureSpriteImport(savePath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(savePath);
        }

        static void EnsureShopWeaponsFolder()
        {
            if (AssetDatabase.IsValidFolder(WeaponIconsFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Aim/Sprites"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Aim"))
                    AssetDatabase.CreateFolder("Assets", "Aim");
                AssetDatabase.CreateFolder("Assets/Aim", "Sprites");
            }

            AssetDatabase.CreateFolder("Assets/Aim/Sprites", "ShopWeapons");
        }

        static string ToAbsolutePath(string assetPath)
        {
            return System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath)!.FullName,
                assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        static Texture2D RenderPrefabToTransparentPng(GameObject prefab, int size)
        {
            PreviewRenderUtility preview = null;
            GameObject instance = null;
            try
            {
                preview = new PreviewRenderUtility();
                preview.cameraFieldOfView = 25f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                preview.camera.allowHDR = false;
                preview.camera.allowMSAA = true;
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = 100f;
                preview.ambientColor = new Color(0.35f, 0.35f, 0.38f, 1f);
                preview.lights[0].intensity = 1.25f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                preview.lights[1].intensity = 0.65f;

                instance = Object.Instantiate(prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance);

                var bounds = CalculateBounds(instance);
                var extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                if (extent < 0.001f)
                    extent = 0.15f;

                var center = bounds.center;
                var distance = extent * 3.6f;

                // Side profile like pack UI (not rear): look along the shorter horizontal axis
                // so the longer axis (barrel) lies across the frame.
                var lookFrom = bounds.size.z >= bounds.size.x
                    ? new Vector3(-distance, extent * 0.1f, 0f)  // barrel along Z → camera on -X
                    : new Vector3(0f, extent * 0.1f, distance);  // barrel along X → camera on +Z

                preview.camera.transform.position = center + lookFrom;
                preview.camera.transform.LookAt(center);
                preview.camera.orthographic = true;
                preview.camera.orthographicSize = extent * 1.45f;

                var rect = new Rect(0f, 0f, size, size);
                preview.BeginStaticPreview(rect);
                preview.Render(true, true);
                var tex = preview.EndStaticPreview();
                if (tex == null)
                    return null;

                // EndStaticPreview texture is often non-readable — copy before alpha punch.
                var readable = CopyToReadableRgba(tex);
                Object.DestroyImmediate(tex);
                if (readable == null)
                    return null;

                MakeNearBlackBackgroundTransparent(readable);
                return readable;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PreSet icon render failed: {ex.Message}");
                return null;
            }
            finally
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
                preview?.Cleanup();
            }
        }

        static Texture2D CopyToReadableRgba(Texture source)
        {
            if (source == null)
                return null;

            var width = source.width;
            var height = source.height;
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static void MakeNearBlackBackgroundTransparent(Texture2D texture)
        {
            if (texture == null)
                return;

            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>(pixels.Length / 4);

            bool IsBackdrop(Color32 c)
            {
                // Opaque black / near-black plate from preview capture.
                return c.r <= 18 && c.g <= 18 && c.b <= 18;
            }

            void TryEnqueue(int x, int y)
            {
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                    return;

                var index = y * width + x;
                if (visited[index] || !IsBackdrop(pixels[index]))
                    return;

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                pixels[index] = new Color32(0, 0, 0, 0);
                var x = index % width;
                var y = index / width;
                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one * 0.2f);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static Texture2D CaptureAssetPreviewTransparent(GameObject prefab)
        {
            Texture2D preview = null;
            for (var i = 0; i < 60; i++)
            {
                preview = AssetPreview.GetAssetPreview(prefab);
                if (preview != null && !AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                    break;
                System.Threading.Thread.Sleep(30);
            }

            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(prefab);
            if (preview == null)
                return null;

            var width = preview.width;
            var height = preview.height;
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(preview, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            MakePreviewBackgroundTransparent(readable);
            MakeNearBlackBackgroundTransparent(readable);
            return readable;
        }

        static void MakePreviewBackgroundTransparent(Texture2D texture)
        {
            if (texture == null)
                return;

            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var bg = pixels[0];
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>(pixels.Length / 4);

            bool IsBackground(Color32 c)
            {
                var dr = Mathf.Abs(c.r - bg.r);
                var dg = Mathf.Abs(c.g - bg.g);
                var db = Mathf.Abs(c.b - bg.b);
                if (dr <= 24 && dg <= 24 && db <= 24)
                    return true;

                return c.r is >= 68 and <= 110
                       && c.g is >= 68 and <= 110
                       && c.b is >= 68 and <= 110
                       && Mathf.Abs(c.r - c.g) <= 10
                       && Mathf.Abs(c.g - c.b) <= 10;
            }

            void TryEnqueue(int x, int y)
            {
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                    return;

                var index = y * width + x;
                if (visited[index] || !IsBackground(pixels[index]))
                    return;

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                pixels[index] = new Color32(0, 0, 0, 0);
                var x = index % width;
                var y = index / width;
                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        static WeaponShopCatalog EnsureCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WeaponShopCatalog>(CatalogPath);
            var catalog = existing != null ? existing : ScriptableObject.CreateInstance<WeaponShopCatalog>();

            var entries = new List<(string id, string name, string path, string letter, int price, bool owned, bool preset)>();

            // Base pistols directly under Prefabs/Weapons (exclude subfolders).
            var baseGuids = AssetDatabase.FindAssets("t:Prefab", new[] { BaseWeaponsFolder });
            var baseIndex = 0;
            for (var i = 0; i < baseGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(baseGuids[i]);
                if (path.Replace('\\', '/').Contains("/Pistol_PreSet/"))
                    continue;

                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("Pistol_", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var letter = ExtractPistolLetter(fileName);
                var id = fileName.ToLowerInvariant().Replace(' ', '_');
                var price = baseIndex == 0 ? 0 : 50 * baseIndex;
                entries.Add((id, fileName.Replace('_', ' '), path, letter, price, baseIndex == 0, false));
                baseIndex++;
            }

            // Equipped variants from Prefabs/Weapons/Pistol_PreSet.
            var presetGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PreSetWeaponsFolder });
            for (var i = 0; i < presetGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(presetGuids[i]);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("Pistol_", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var letter = ExtractPistolLetter(fileName);
                var id = fileName.ToLowerInvariant().Replace(' ', '_');
                var display = fileName.Replace('_', ' ');
                var price = 250 + i * 50;
                entries.Add((id, display, path, letter, price, false, true));
            }

            entries.Sort((a, b) =>
            {
                if (a.preset != b.preset)
                    return a.preset ? 1 : -1;
                return string.CompareOrdinal(a.name, b.name);
            });

            if (entries.Count == 0)
            {
                Debug.LogError("Shop catalog: no weapon prefabs found in Weapons / Pistol_PreSet.");
                return catalog;
            }

            var so = new SerializedObject(catalog);
            var weaponsProp = so.FindProperty("weapons");
            weaponsProp.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var def = entries[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.path);
                if (prefab == null)
                    Debug.LogWarning($"Shop catalog missing prefab: {def.path}");

                var icon = def.preset
                    ? CapturePresetIcon(prefab, def.letter)
                    : LoadWeaponIcon(def.letter);

                var element = weaponsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = def.id;
                element.FindPropertyRelative("displayName").stringValue = def.name;
                element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                element.FindPropertyRelative("price").intValue = def.price;
                element.FindPropertyRelative("icon").objectReferenceValue = icon;
                element.FindPropertyRelative("ownedByDefault").boolValue = def.owned;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
            {
                var resourcesDir = "Assets/Aim/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesDir))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Aim"))
                        AssetDatabase.CreateFolder("Assets", "Aim");
                    AssetDatabase.CreateFolder("Assets/Aim", "Resources");
                }

                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Shop catalog rebuilt: {entries.Count} weapons " +
                      $"({entries.FindAll(e => !e.preset).Count} base + {entries.FindAll(e => e.preset).Count} PreSet).");
            return catalog;
        }

        static string ExtractPistolLetter(string fileName)
        {
            // Pistol_O / Pistol_O_PreSet -> O
            var parts = fileName.Split('_');
            return parts.Length >= 2 ? parts[1] : "O";
        }

        static ShopView CreateShopUi(Transform canvasTransform)
        {
            var root = new GameObject("ShopUI", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var shopButton = CreateCircleIconButton(
                root.transform,
                "ShopButton",
                new Vector2(1f, 1f),
                new Vector2(-84f, -18f),
                new Vector2(58f, 58f),
                ShopIconPath,
                30f);

            var dimmerGo = new GameObject("Dimmer", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            dimmerGo.transform.SetParent(root.transform, false);
            StretchFull(dimmerGo.GetComponent<RectTransform>());
            var dimmerImage = dimmerGo.GetComponent<Image>();
            dimmerImage.color = DimmerColor;
            dimmerImage.raycastTarget = true;
            var dimmerGroup = dimmerGo.GetComponent<CanvasGroup>();
            var dimmerButton = dimmerGo.GetComponent<Button>();
            dimmerButton.transition = Selectable.Transition.None;

            var panelRootGo = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelRootGo.transform.SetParent(root.transform, false);
            var panelRoot = panelRootGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            var canvasRect = canvasTransform as RectTransform;
            var canvasSize = canvasRect != null ? canvasRect.rect.size : new Vector2(1080f, 1920f);
            if (canvasSize.x < 2f || canvasSize.y < 2f)
                canvasSize = new Vector2(1080f, 1920f);
            var panelWidth = Mathf.Clamp(canvasSize.x * 0.9f, 320f, 520f);
            var panelHeight = Mathf.Clamp(canvasSize.y * 0.78f, 360f, 480f);
            panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRoot.anchoredPosition = Vector2.zero;
            var panelGroup = panelRootGo.GetComponent<CanvasGroup>();

            CreateStretchImage(panelRootGo.transform, "Shadow", new Vector2(8f, -10f), new Vector2(-8f, 10f), PanelShadow, "UI/Skin/Background.psd", true);
            CreateStretchImage(panelRootGo.transform, "Border", Vector2.zero, Vector2.zero, PanelBorder, "UI/Skin/Background.psd", true);
            var body = CreateStretchImage(panelRootGo.transform, "Body", new Vector2(6f, 6f), new Vector2(-6f, -6f), PanelBody, "UI/Skin/Background.psd", true);

            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(body.transform, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -78f);
            headerRect.offsetMax = Vector2.zero;
            var headerImage = headerGo.GetComponent<Image>();
            headerImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            headerImage.type = Image.Type.Sliced;
            headerImage.color = HeaderBar;

            var accentGo = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(headerGo.transform, false);
            var accentRect = accentGo.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(0.5f, 0f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = new Vector2(0f, 3f);
            var accentImage = accentGo.GetComponent<Image>();
            accentImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            accentImage.type = Image.Type.Sliced;
            accentImage.color = Accent;

            CreateLabel(headerGo.transform, "Title", "МАГАЗИН", new Vector2(0f, -22f), 32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            CreateLabel(headerGo.transform, "Section", "ОРУЖИЕ", new Vector2(0f, -48f), 16, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText);

            var closeButton = CreateCloseButton(body.transform);

            var scrollGo = new GameObject("WeaponsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            scrollGo.transform.SetParent(body.transform, false);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(16f, 16f);
            scrollRectTransform.offsetMax = new Vector2(-16f, -90f);
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.07f, 0.1f, 0.35f);
            scrollImage.raycastTarget = true;
            var mask = scrollGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = scrollRectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = CreateWeaponRowTemplate(contentGo.transform);
            rowTemplate.SetActive(false);
            rowTemplate.name = "WeaponRowTemplate";

            var view = root.AddComponent<ShopView>();
            SetObjectField(view, "shopButton", shopButton);
            SetObjectField(view, "closeButton", closeButton);
            SetObjectField(view, "dimmerButton", dimmerButton);
            SetObjectField(view, "dimmerGroup", dimmerGroup);
            SetObjectField(view, "panelGroup", panelGroup);
            SetObjectField(view, "panelRoot", panelRoot);
            SetObjectField(view, "weaponsContent", contentRect);
            SetObjectField(view, "weaponRowPrefab", rowTemplate);

            return view;
        }

        static GameObject CreateWeaponRowTemplate(Transform parent)
        {
            var card = CreateImage(parent, "WeaponRow", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(480f, 96f), RowCard, "UI/Skin/Background.psd", true);
            var layout = card.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 96f;
            layout.preferredHeight = 96f;

            var accent = CreateImage(card.transform, "SideAccent", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 96f), Accent, "UI/Skin/UISprite.psd", true);
            accent.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            accent.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);

            var iconBadge = CreateImage(card.transform, "IconBadge", new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(108f, 68f), new Color(0.08f, 0.1f, 0.14f, 0.95f), "UI/Skin/Background.psd", true);
            iconBadge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            iconBadge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            iconBadge.rectTransform.pivot = new Vector2(0f, 0.5f);

            var weaponIcon = CreateImage(iconBadge.transform, "WeaponIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 60f), Color.white, "UI/Skin/UISprite.psd", false);
            weaponIcon.sprite = LoadWeaponIcon("O");
            weaponIcon.preserveAspect = true;
            weaponIcon.raycastTarget = false;

            var name = CreateLabel(card.transform, "Name", "Оружие", new Vector2(-40f, 14f), 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            name.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.anchoredPosition = new Vector2(136f, 14f);
            name.rectTransform.sizeDelta = new Vector2(180f, 28f);

            var status = CreateLabel(card.transform, "Status", "Заблокировано", new Vector2(-40f, -14f), 14, FontStyle.Normal, TextAnchor.MiddleLeft, SoftText);
            status.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            status.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            status.rectTransform.pivot = new Vector2(0f, 0.5f);
            status.rectTransform.anchoredPosition = new Vector2(136f, -14f);
            status.rectTransform.sizeDelta = new Vector2(160f, 24f);

            // Price (coin + value) centered directly above Buy button.
            const float columnWidth = 120f;
            const float columnRight = -16f;

            var priceRow = new GameObject("PriceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            priceRow.transform.SetParent(card.transform, false);
            var priceRowRect = priceRow.GetComponent<RectTransform>();
            priceRowRect.anchorMin = new Vector2(1f, 0.5f);
            priceRowRect.anchorMax = new Vector2(1f, 0.5f);
            priceRowRect.pivot = new Vector2(1f, 0.5f);
            priceRowRect.anchoredPosition = new Vector2(columnRight, 18f);
            priceRowRect.sizeDelta = new Vector2(columnWidth, 24f);

            var priceLayout = priceRow.GetComponent<HorizontalLayoutGroup>();
            priceLayout.padding = new RectOffset(0, 0, 0, 0);
            priceLayout.spacing = 10f;
            priceLayout.childAlignment = TextAnchor.MiddleCenter;
            priceLayout.childControlWidth = false;
            priceLayout.childControlHeight = false;
            priceLayout.childForceExpandWidth = false;
            priceLayout.childForceExpandHeight = false;

            var priceFitter = priceRow.GetComponent<ContentSizeFitter>();
            priceFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            priceFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var priceIcon = CreateImage(priceRow.transform, "PriceIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f), Color.white, "UI/Skin/Knob.psd", false);
            priceIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            priceIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            priceIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            priceIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
            priceIcon.preserveAspect = true;
            priceIcon.raycastTarget = false;
            var priceIconLayout = priceIcon.gameObject.AddComponent<LayoutElement>();
            priceIconLayout.preferredWidth = 20f;
            priceIconLayout.preferredHeight = 20f;
            priceIconLayout.minWidth = 20f;

            var price = CreateLabel(priceRow.transform, "Price", "100", Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            price.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            price.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            price.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            price.rectTransform.sizeDelta = new Vector2(60f, 24f);
            var priceLayoutElement = price.gameObject.AddComponent<LayoutElement>();
            priceLayoutElement.preferredWidth = 60f;
            priceLayoutElement.preferredHeight = 24f;
            priceLayoutElement.minWidth = 48f;

            var actionGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            actionGo.transform.SetParent(card.transform, false);
            var actionRect = actionGo.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1f, 0.5f);
            actionRect.anchorMax = new Vector2(1f, 0.5f);
            actionRect.pivot = new Vector2(1f, 0.5f);
            actionRect.anchoredPosition = new Vector2(columnRight, -18f);
            actionRect.sizeDelta = new Vector2(columnWidth, 34f);
            var actionImage = actionGo.GetComponent<Image>();
            actionImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            actionImage.type = Image.Type.Sliced;
            actionImage.color = ButtonNormal;
            var actionButton = actionGo.GetComponent<Button>();
            actionButton.targetGraphic = actionImage;
            var colors = actionButton.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = Accent;
            colors.pressedColor = ButtonOwned;
            colors.disabledColor = new Color(0.25f, 0.28f, 0.32f, 0.85f);
            actionButton.colors = colors;

            var actionLabel = CreateLabel(actionGo.transform, "ActionLabel", "Купить", Vector2.zero, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            actionLabel.rectTransform.anchorMin = Vector2.zero;
            actionLabel.rectTransform.anchorMax = Vector2.one;
            actionLabel.rectTransform.offsetMin = Vector2.zero;
            actionLabel.rectTransform.offsetMax = Vector2.zero;

            return card.gameObject;
        }

        static Button CreateCircleIconButton(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            string iconPath,
            float iconSize)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var border = buttonGo.GetComponent<Image>();
            border.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            border.color = IconCircleBorder;
            border.raycastTarget = true;
            border.preserveAspect = true;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(buttonGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = size - new Vector2(6f, 6f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fill.color = IconCircle;
            fill.raycastTarget = false;
            fill.preserveAspect = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(fillGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = IconCircleBorder;
            colors.highlightedColor = Accent;
            colors.pressedColor = new Color(0.25f, 0.55f, 0.85f, 1f);
            button.colors = colors;
            return button;
        }

        static Button CreateCloseButton(Transform parent)
        {
            var buttonGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -14f);
            rect.sizeDelta = new Vector2(34f, 34f);

            var image = buttonGo.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = new Color(0.22f, 0.28f, 0.38f, 1f);

            CreateLabel(buttonGo.transform, "X", "✕", Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        static Image CreateStretchImage(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            string builtinSprite,
            bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            Color color,
            string builtinSprite,
            bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        static Text CreateLabel(
            Transform parent,
            string name,
            string content,
            Vector2 anchoredPos,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400f, 40f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            LanguageYgTextUtility.AttachToTextComponent(text, content);
            return text;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetObjectField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
