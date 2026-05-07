using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CanvasSelectorController : MonoBehaviour
{
    public enum SlideDirection { FromTop, FromBottom, FromLeft, FromRight }

    [SerializeField] private UIDocument uiDocument;

    [Header("UXML Assets")]
    [SerializeField] private VisualTreeAsset weaponSelectorAsset;
    [SerializeField] private VisualTreeAsset itemSelectorAsset;
    [SerializeField] private VisualTreeAsset ammunitionInfoAsset;

    [Header("Settings")]
    public float slideDuration = 0.4f;

    // ---- Константы масштабирования слотов ----
    // Целевая ширина панели (суммарная), к которой стремимся при любом числе оружий.
    private const float TARGET_PANEL_WIDTH  = 1600f;
    private const float SLOT_GAP            = 6f;
    private const float PANEL_PADDING_H     = 42f;   // 21px * 2 (padding-left + padding-right)

    // Минимальный и максимальный размеры слота
    private const float SLOT_MIN_WIDTH  = 178f;
    private const float SLOT_MIN_HEIGHT = 74f;
    private const float SLOT_MAX_WIDTH  = 420f;
    private const float SLOT_MAX_HEIGHT = 130f;

    // При каком числе оружий включаем «большой» вариант шрифтов/иконок
    private const int LARGE_THRESHOLD = 4;

    private VisualElement root;
    private VisualElement weaponPanel;
    private VisualElement itemPanel;
    private VisualElement ammoPanel;

    private readonly List<VisualElement> weaponSlots = new();
    private int activeSlotIndex = -1;

    private readonly List<VisualElement> itemSlots = new();
    private int selectedItemIndex = 0;

    public Coroutine[] slideCoroutines = new Coroutine[3];
    public bool isOpen = false;

    private bool isAltMode = false;
    private Coroutine altCoroutine;

    private VisualElement hintsPanel;
    private Coroutine hintCoroutine;

    private bool dragPending   = false;
    private bool isDragging    = false;
    private bool justDragged   = false;
    private int  dragSourceIdx = -1;
    private int  dropTargetIdx = -1;
    private Vector2 dragStartPos;
    private VisualElement dragGhost;
    private const float DRAG_THRESHOLD = 6f;

    private PlayerHealth playerHealth;

    private static CanvasSelectorController _instance;
    public static CanvasSelectorController Instance => _instance;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        root = uiDocument.rootVisualElement;
        BuildPanels();
    }

    void Start()
    {
        playerHealth = PlayerController.Instance.playerHealth;
    }

    void Update()
    {
        if (playerHealth.isDead) return;

        if (Input.GetKeyDown(KeyCode.Tab)) Open();
        if (Input.GetKeyUp(KeyCode.Tab))   Close();

        // Q-режим: только если полный инвентарь закрыт
        if (!isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Q)) OpenAltMode();
            if (Input.GetKeyUp(KeyCode.Q))   CloseAltMode();
        }

        if (Input.GetKeyDown(KeyCode.F) && !isOpen && !isAltMode)
        {
            var inv = PlayerInventory.Instance;
            if (inv != null && inv.HasItems)
            {
                int idx  = Mathf.Clamp(selectedItemIndex, 0, inv.items.Count - 1);
                PlayerItem item = inv.items[idx];
                if (item.isThrowable)
                    ThrowableItemHandler.Instance.BeginAiming(item);
                else
                    QuickUseItem();
            }
        }
        if (Input.GetKeyUp(KeyCode.F) && !isOpen && !isAltMode)
            ThrowableItemHandler.Instance.OnKeyUp();

        // F + 1–9 while aiming: switch item on the fly without triggering weapon switch
        bool fItemSwitched = false;
        if (Input.GetKey(KeyCode.F) && !isOpen && !isAltMode)
        {
            for (int k = 0; k < 9; k++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + k)))
                {
                    var inv = PlayerInventory.Instance;
                    if (inv != null && k < inv.items.Count)
                    {
                        selectedItemIndex = k;
                        UpdateItemSelection();
                        PlayerItem newItem = inv.items[k];
                        if (newItem.isThrowable)
                            ThrowableItemHandler.Instance.BeginAiming(newItem);
                        else
                        {
                            ThrowableItemHandler.Instance.CancelAiming();
                            QuickUseItem();
                        }
                    }
                    fItemSwitched = true;
                    break;
                }
            }
        }

        if (isAltMode && Input.GetKeyDown(KeyCode.F))
        {
            var inv = PlayerInventory.Instance;
            if (inv != null && inv.HasItems)
            {
                int idx = Mathf.Clamp(selectedItemIndex, 0, inv.items.Count - 1);
                PlayerItem item = inv.items[idx];
                if (item.isThrowable)
                    ThrowableItemHandler.Instance.BeginAiming(item);
                else
                    QuickUseItem();
            }
            CloseAltMode();
        }
        if (isAltMode && Input.GetKeyUp(KeyCode.F))
            ThrowableItemHandler.Instance.OnKeyUp();

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.LeftBracket))  SelectItemDelta(-1);
            if (Input.GetKeyDown(KeyCode.RightBracket)) SelectItemDelta(+1);
            if (Input.GetKeyDown(KeyCode.F))            UseSelectedItem();
        }

        // Alt+1-9: выбор предмета по индексу (без использования)
        bool altItemUsed = false;
        if (isAltMode)
        {
            for (int k = 0; k < 9; k++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + k)))
                {
                    var inv = PlayerInventory.Instance;
                    if (inv != null && k < inv.items.Count)
                    {
                        selectedItemIndex = k;
                        UpdateItemSelection();
                    }
                    altItemUsed = true;
                    break;
                }
            }
        }

        // Переключение оружий 1-9 блокируется в Alt-режиме, если предмет уже использован, или при зажатом F
        if (!isAltMode && !altItemUsed && !fItemSwitched)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchWeapon(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchWeapon(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchWeapon(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchWeapon(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) SwitchWeapon(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) SwitchWeapon(9);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) SwitchWeaponNext();
        if (scroll < 0f) SwitchWeaponPrev();
    }

    // ----------------------------------------------------------------
    //  Build
    // ----------------------------------------------------------------

    void BuildPanels()
    {
        weaponPanel = weaponSelectorAsset.Instantiate();
        itemPanel   = itemSelectorAsset.Instantiate();
        ammoPanel   = ammunitionInfoAsset.Instantiate();

        PositionItemPanel(itemPanel);
        PositionAmmoPanel(ammoPanel);

        // Weapon panel позиционируется после BuildWeaponSlots,
        // где узнаём реальное число оружий.
        SetTranslate(weaponPanel, 0, -Screen.height);
        SetTranslate(itemPanel,   0,  Screen.height);
        SetTranslate(ammoPanel,   Screen.width, 0);

        root.Add(weaponPanel);
        root.Add(itemPanel);
        root.Add(ammoPanel);

        BuildHintsPanel();
        SetTranslate(hintsPanel, -Screen.width, 0);
        root.Add(hintsPanel);

        BuildWeaponSlots();
        InitItemSlots();
    }

    // ----------------------------------------------------------------
    //  Slot size calculation
    // ----------------------------------------------------------------

    /// <summary>
    /// Вычисляет размер одного слота так, чтобы все слоты вместе
    /// занимали примерно TARGET_PANEL_WIDTH.
    /// </summary>
    static (float slotW, float slotH) CalcSlotSize(int count)
    {
        if (count <= 0) count = 1;

        // Доступная ширина для слотов = target - padding - gaps
        float available = TARGET_PANEL_WIDTH - PANEL_PADDING_H - SLOT_GAP * (count - 1);
        float slotW = available / count;
        slotW = Mathf.Clamp(slotW, SLOT_MIN_WIDTH, SLOT_MAX_WIDTH);

        // Высота масштабируется пропорционально ширине относительно минимума
        float ratio  = (slotW - SLOT_MIN_WIDTH) / (SLOT_MAX_WIDTH - SLOT_MIN_WIDTH);
        float slotH  = Mathf.Lerp(SLOT_MIN_HEIGHT, SLOT_MAX_HEIGHT, ratio);

        return (Mathf.Round(slotW), Mathf.Round(slotH));
    }

    /// <summary>
    /// Суммарная ширина панели с учётом padding и gap для count слотов.
    /// </summary>
    static float CalcPanelWidth(int count, float slotW)
    {
        return slotW * count + SLOT_GAP * Mathf.Max(0, count - 1) + PANEL_PADDING_H;
    }

    // ----------------------------------------------------------------
    //  Positioning
    // ----------------------------------------------------------------

    void PositionWeaponPanel(VisualElement panel, float panelWidth)
    {
        panel.style.position  = Position.Absolute;
        panel.style.top       = new StyleLength(0f);
        panel.style.left      = Length.Percent(50);
        panel.style.marginLeft = new StyleLength(-panelWidth * 0.5f);
    }

    void PositionItemPanel(VisualElement panel)
    {
        panel.style.position   = Position.Absolute;
        panel.style.bottom     = new StyleLength(0f);
        panel.style.left       = Length.Percent(50);
        panel.style.marginLeft = new StyleLength(-315f);  // 630px / 2
    }

    void PositionAmmoPanel(VisualElement panel)
    {
        panel.style.position  = Position.Absolute;
        panel.style.right     = new StyleLength(0f);
        panel.style.top       = Length.Percent(50);
        panel.style.marginTop = new StyleLength(-90f);    // 180px / 2
    }

    // ----------------------------------------------------------------
    //  Build slots
    // ----------------------------------------------------------------

    void BuildWeaponSlots()
    {
        VisualElement container = weaponPanel.Q("slots-container");
        if (container == null) return;
        container.Clear();
        weaponSlots.Clear();

        GunController gc    = GunController.Instance;
        int weaponCount     = CountPlayerWeapons(gc);
        bool large          = weaponCount <= LARGE_THRESHOLD;

        var (slotW, slotH)  = CalcSlotSize(weaponCount);
        float panelWidth    = CalcPanelWidth(weaponCount, slotW);
        float leftW         = large ? 70f : 50f;
        float iconSize      = large ? 64f : 44f;

        // Позиционируем панель по реальной ширине
        PositionWeaponPanel(weaponPanel, panelWidth);
        weaponPanel.style.width = new StyleLength(panelWidth);

        for (int i = 1; i <= GunController.MAX_WEAPONS; i++)
        {
            if (gc != null && gc.TryGetWeaponAt(i) == null) continue; // показываем только полученные

            var slot = new VisualElement();
            slot.AddToClassList("weapon-slot");

            slot.style.width  = new StyleLength(slotW);
            slot.style.height = new StyleLength(slotH);

            // Номер горячей клавиши
            var numLbl = new Label(i.ToString());
            numLbl.AddToClassList("weapon-slot-number");
            numLbl.style.position                = Position.Absolute;
            numLbl.style.top                     = new StyleLength(3f);
            numLbl.style.right                   = new StyleLength(5f);
            numLbl.style.fontSize                = large ? 13 : 10;
            numLbl.style.color                   = new Color(1f, 0.85f, 0.2f);
            numLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            numLbl.pickingMode                   = PickingMode.Ignore;
            slot.Add(numLbl);

            // Левая колонка
            var left = new VisualElement();
            left.AddToClassList("weapon-left");
            left.style.width = new StyleLength(leftW);

            var icon = new VisualElement();
            icon.name = "weapon-icon";
            icon.AddToClassList("weapon-icon");
            icon.AddToClassList("weapon-icon--empty");
            icon.style.width     = new StyleLength(iconSize);
            icon.style.height    = new StyleLength(iconSize);
            icon.style.minWidth  = new StyleLength(iconSize);
            icon.style.minHeight = new StyleLength(iconSize);

            left.Add(icon);

            // Правая колонка
            var info = new VisualElement();
            info.name = "weapon-info";
            info.AddToClassList("weapon-info");

            var nameLbl = new Label("— empty —");
            nameLbl.name = "weapon-name";
            nameLbl.AddToClassList("weapon-name");
            nameLbl.AddToClassList("weapon-name--empty");
            if (large) nameLbl.AddToClassList("weapon-name--large");

            var ammoLbl = new Label("");
            ammoLbl.name = "weapon-ammo";
            ammoLbl.AddToClassList("weapon-ammo");
            if (large) ammoLbl.AddToClassList("weapon-ammo--large");

            info.Add(nameLbl);
            info.Add(ammoLbl);

            slot.Add(left);
            slot.Add(info);

            if (weaponSlots.Count > 0)
                slot.style.marginLeft = new StyleLength(SLOT_GAP);

            container.Add(slot);
            weaponSlots.Add(slot);

            int captured = i;
            slot.RegisterCallback<ClickEvent>(_ => SwitchWeapon(captured));
        }
    }

    /// <summary>Считает количество оружий у игрока (не null слоты).</summary>
    int CountPlayerWeapons(GunController gc)
    {
        if (gc == null) return 1;
        int count = 0;
        for (int i = 1; i <= GunController.MAX_WEAPONS; i++)
            if (gc.TryGetWeaponAt(i) != null) count++;
        return Mathf.Max(count, 1);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    void SetTranslate(VisualElement panel, float x, float y)
    {
        panel.style.translate = new StyleTranslate(new Translate(x, y));
    }

    Vector2 OffscreenOffset(SlideDirection dir)
    {
        return dir switch
        {
            SlideDirection.FromTop    => new Vector2(0,            -Screen.height),
            SlideDirection.FromBottom => new Vector2(0,             Screen.height),
            SlideDirection.FromRight  => new Vector2(Screen.width,  0),
            SlideDirection.FromLeft   => new Vector2(-Screen.width, 0),
            _ => Vector2.zero
        };
    }

    // ----------------------------------------------------------------
    //  Animation
    // ----------------------------------------------------------------

    IEnumerator SlideIn(VisualElement panel, SlideDirection dir)
    {
        ShaderVolumeManager.Instance.SetScanlinesIntensity(1);
        StartCoroutine(ShaderVolumeManager.Instance.ColorFilterIntensityCoroutine(-1, slideDuration));
        GunController.Instance.isWeaponHidden = true;
        GunController.Instance.DeactivateLastWeapon();

        Vector2 from = OffscreenOffset(dir);
        Vector2 to   = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t   = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 p = Vector2.Lerp(from, to, t);
            SetTranslate(panel, p.x, p.y);
            yield return null;
        }
        SetTranslate(panel, 0, 0);

        Time.timeScale = 0.1f;
    }

    public IEnumerator SlideOut(VisualElement panel, SlideDirection dir)
    {
        Time.timeScale = 1;

        ShaderVolumeManager.Instance.SetScanlinesDefaultIntensity();
        StartCoroutine(ShaderVolumeManager.Instance.ColorFilterIntensityCoroutine(0, 0.1f));
        GunController.Instance.isWeaponHidden = false;
        GunController.Instance.ActivateLastWeapon();

        Vector2 from = Vector2.zero;
        Vector2 to   = OffscreenOffset(dir);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t   = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 p = Vector2.Lerp(from, to, t);
            SetTranslate(panel, p.x, p.y);
            yield return null;
        }
        SetTranslate(panel, to.x, to.y);
    }

    public void StopCoroutines()
    {
        foreach (var c in slideCoroutines)
            if (c != null) StopCoroutine(c);
    }

    // ----------------------------------------------------------------
    //  Control
    // ----------------------------------------------------------------

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        // Если Alt-режим был активен — гасим его без анимации, itemPanel остаётся на месте
        bool itemAlreadyVisible = isAltMode;
        if (isAltMode)
        {
            isAltMode = false;
            if (altCoroutine != null) StopCoroutine(altCoroutine);
            RemoveNumberHints();
            SetTranslate(itemPanel, 0, 0);
        }

        StopCoroutines();
        BuildWeaponSlots();
        RefreshAll();

        slideCoroutines[0] = StartCoroutine(SlideIn(weaponPanel, SlideDirection.FromTop));
        if (!itemAlreadyVisible)
            slideCoroutines[1] = StartCoroutine(SlideIn(itemPanel, SlideDirection.FromBottom));
        slideCoroutines[2] = StartCoroutine(SlideIn(ammoPanel, SlideDirection.FromRight));

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(SlideInSimple(hintsPanel, SlideDirection.FromLeft));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StopCoroutines();
        slideCoroutines[0] = StartCoroutine(SlideOut(weaponPanel, SlideDirection.FromTop));
        slideCoroutines[1] = StartCoroutine(SlideOut(itemPanel,   SlideDirection.FromBottom));
        slideCoroutines[2] = StartCoroutine(SlideOut(ammoPanel,   SlideDirection.FromRight));

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(SlideOutSimple(hintsPanel, SlideDirection.FromLeft));
    }

    public void Toggle() { if (isOpen) Close(); else Open(); }
    public bool IsOpen   => isOpen;

    // ----------------------------------------------------------------
    //  Alt-режим (только панель предметов)
    // ----------------------------------------------------------------

    void OpenAltMode()
    {
        if (isOpen || isAltMode) return;
        isAltMode = true;
        RefreshItemSlots(); // AddNumberHints вызывается внутри
        if (altCoroutine != null) StopCoroutine(altCoroutine);
        altCoroutine = StartCoroutine(SlideInSimple(itemPanel, SlideDirection.FromBottom));
    }

    void CloseAltMode()
    {
        if (!isAltMode) return;
        isAltMode = false;
        RemoveNumberHints();
        if (altCoroutine != null) StopCoroutine(altCoroutine);
        altCoroutine = StartCoroutine(SlideOutSimple(itemPanel, SlideDirection.FromBottom));
    }

    void AddNumberHints()
    {
        for (int i = 0; i < itemSlots.Count && i < 9; i++)
        {
            var hint = new Label((i + 1).ToString());
            hint.AddToClassList("item-slot-number");
            hint.style.position              = Position.Absolute;
            hint.style.top                   = new StyleLength(2f);
            hint.style.left                  = new StyleLength(4f);
            hint.style.fontSize              = 11;
            hint.style.color                 = Color.yellow;
            hint.style.unityFontStyleAndWeight = FontStyle.Bold;
            itemSlots[i].Add(hint);
        }
    }

    void RemoveNumberHints()
    {
        foreach (var slot in itemSlots)
        {
            var hints = slot.Query<Label>(className: "item-slot-number").ToList();
            foreach (var hint in hints)
                hint.RemoveFromHierarchy();
        }
    }

    IEnumerator SlideInSimple(VisualElement panel, SlideDirection dir)
    {
        Vector2 from = OffscreenOffset(dir);
        Vector2 to   = Vector2.zero;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t   = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 p = Vector2.Lerp(from, to, t);
            SetTranslate(panel, p.x, p.y);
            yield return null;
        }
        SetTranslate(panel, 0, 0);
    }

    IEnumerator SlideOutSimple(VisualElement panel, SlideDirection dir)
    {
        Vector2 from = Vector2.zero;
        Vector2 to   = OffscreenOffset(dir);
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t   = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 p = Vector2.Lerp(from, to, t);
            SetTranslate(panel, p.x, p.y);
            yield return null;
        }
        SetTranslate(panel, to.x, to.y);
    }

    // ----------------------------------------------------------------
    //  Weapons
    // ----------------------------------------------------------------

    public void SwitchWeapon(int index)
    {
        GunController gc = GunController.Instance;
        if (gc == null || gc.TryGetWeaponAt(index) == null) return;
        gc.SwitchWeapon(index);
        SetActiveSlot(index);
        RefreshAmmo();
    }

    public void SwitchWeaponNext() { int i = FindAdjacent(+1); if (i != -1) SwitchWeapon(i); }
    public void SwitchWeaponPrev() { int i = FindAdjacent(-1); if (i != -1) SwitchWeapon(i); }

    int FindAdjacent(int dir)
    {
        GunController gc = GunController.Instance;
        if (gc == null) return -1;
        int current = Mathf.Max(gc.GetCurrentGunIndex(), 1);
        for (int step = 1; step <= GunController.MAX_WEAPONS; step++)
        {
            int candidate = ((current - 1 + dir * step + GunController.MAX_WEAPONS)
                             % GunController.MAX_WEAPONS) + 1;
            if (gc.TryGetWeaponAt(candidate) != null) return candidate;
        }
        return -1;
    }

    // ----------------------------------------------------------------
    //  Refresh
    // ----------------------------------------------------------------

    void RefreshAll()
    {
        RefreshWeaponSlots();
        RefreshAmmo();
        RefreshItemSlots();
    }

    // Маппинг: индекс в GunController → элемент слота в weaponSlots.
    // weaponSlots содержит только реальные оружия в порядке возрастания индекса.
    void RefreshWeaponSlots()
    {
        GunController gc = GunController.Instance;
        if (gc == null) return;

        int slotVisual = 0;
        for (int i = 1; i <= GunController.MAX_WEAPONS; i++)
        {
            PlayerGun gun = gc.TryGetWeaponAt(i);
            if (gun == null) continue;

            if (slotVisual >= weaponSlots.Count) break;
            VisualElement slot = weaponSlots[slotVisual];
            slotVisual++;

            Label nameLbl = slot.Q<Label>("weapon-name");
            Label ammoLbl = slot.Q<Label>("weapon-ammo");
            VisualElement icon = slot.Q<VisualElement>("weapon-icon");

            slot.RemoveFromClassList("weapon-slot--empty");
            nameLbl.RemoveFromClassList("weapon-name--empty");
            nameLbl.text = gun.GetGunName();
            ammoLbl.text = gun.GetAmmoDisplayString(gc);

            if (icon != null)
            {
                icon.Clear();
                icon.RemoveFromClassList("weapon-icon--not-found");

                if (gun.weaponIcon != null)
                {
                    icon.RemoveFromClassList("weapon-icon--empty");
                    icon.style.backgroundImage = new StyleBackground(gun.weaponIcon);
                }
                else
                {
                    icon.style.backgroundImage = StyleKeyword.Null;
                    icon.AddToClassList("weapon-icon--not-found");

                    var notFoundLabel = new Label("NOT\nFOUND");
                    notFoundLabel.AddToClassList("weapon-icon-not-found-label");
                    icon.Add(notFoundLabel);

                    icon.RemoveFromClassList("weapon-icon--empty");
                }
            }
        }

        SetActiveSlot(gc.GetCurrentGunIndex());
    }

    /// <summary>
    /// Устанавливает активный слот. Поскольку weaponSlots теперь содержат
    /// только реальные оружия, нужно найти визуальный индекс по gun index.
    /// </summary>
    void SetActiveSlot(int gunIndex)
    {
        // Снять выделение со старого слота
        if (activeSlotIndex >= 0 && activeSlotIndex < weaponSlots.Count)
            weaponSlots[activeSlotIndex].RemoveFromClassList("weapon-slot--active");

        activeSlotIndex = GunIndexToVisualIndex(gunIndex);

        if (activeSlotIndex >= 0 && activeSlotIndex < weaponSlots.Count)
            weaponSlots[activeSlotIndex].AddToClassList("weapon-slot--active");
    }

    /// <summary>Переводит gun index (1-based, может быть разреженным)
    /// в индекс визуального слота (0-based, плотный).</summary>
    int GunIndexToVisualIndex(int gunIndex)
    {
        GunController gc = GunController.Instance;
        if (gc == null) return -1;

        int visual = 0;
        for (int i = 1; i <= GunController.MAX_WEAPONS; i++)
        {
            if (gc.TryGetWeaponAt(i) == null) continue;
            if (i == gunIndex) return visual;
            visual++;
        }
        return -1;
    }

    void RefreshAmmo()
    {
        GunController gc = GunController.Instance;
        if (gc == null) return;
        UpdateAmmoRow(gc, AmmoType.Lead,      "bar-lead",      "count-lead");
        UpdateAmmoRow(gc, AmmoType.BuckShot,  "bar-buckshot",  "count-buckshot");
        UpdateAmmoRow(gc, AmmoType.Rocket,    "bar-rocket",    "count-rocket");
        UpdateAmmoRow(gc, AmmoType.Energetic, "bar-energetic", "count-energetic");
    }

    void UpdateAmmoRow(GunController gc, AmmoType type, string barName, string countName)
    {
        int current = gc.GetAmmo(type);
        int max     = gc.GetMaxAmmo(type);

        VisualElement bar   = ammoPanel.Q(barName);
        Label         count = ammoPanel.Q<Label>(countName);

        if (bar   != null && max > 0) bar.style.width = Length.Percent(100f * current / max);
        if (count != null)            count.text      = $"{current} / {max}";
    }

    // ----------------------------------------------------------------
    //  Items
    // ----------------------------------------------------------------

    // Вызывается один раз при старте — находит слоты из UXML и вешает клики
    void InitItemSlots()
    {
        itemSlots.Clear();
        var slots = itemPanel.Query(className: "item-slot").ToList();
        foreach (var slot in slots)
            itemSlots.Add(slot);

        for (int i = 0; i < itemSlots.Count; i++)
        {
            int captured = i;

            itemSlots[i].RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!isOpen) return;
                var inv = PlayerInventory.Instance;
                if (inv == null || captured >= inv.items.Count) return;
                BeginDrag(captured, evt.position);
            });

            itemSlots[i].RegisterCallback<ClickEvent>(_ =>
            {
                if (justDragged) { justDragged = false; return; }
                var inv = PlayerInventory.Instance;
                if (inv == null || captured >= inv.items.Count) return;
                selectedItemIndex = captured;
                UpdateItemSelection();
            });
        }

        root.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragPending && !isDragging) return;
            if (dragPending && Vector2.Distance(evt.position, dragStartPos) > DRAG_THRESHOLD)
                CommitDrag(evt.position);
            if (isDragging)
                UpdateDrag(evt.position);
        });

        root.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (isDragging)
                EndDrag(evt.position);
            else if (dragPending)
            {
                dragPending    = false;
                dragSourceIdx  = -1;
            }
        });

        // Убираем плашку NOT IMPLEMENTED
        itemPanel.Q<Label>(className: "not-implemented-badge")?.RemoveFromHierarchy();
    }

    // Обновляет визуал слотов по текущему инвентарю
    void RefreshItemSlots()
    {
        var inv = PlayerInventory.Instance;

        for (int i = 0; i < itemSlots.Count; i++)
        {
            var slot = itemSlots[i];
            slot.Clear();

            if (i > 0)
                slot.style.marginLeft = new StyleLength(4f);

            if (inv != null && i < inv.items.Count)
            {
                PlayerItem item = inv.items[i];
                slot.RemoveFromClassList("item-slot--empty");

                if (item.itemIcon != null)
                {
                    slot.style.backgroundImage = new StyleBackground(item.itemIcon);
                }
                else
                {
                    slot.style.backgroundImage = StyleKeyword.Null;
                    var nameLbl = new Label(item.itemName);
                    nameLbl.style.fontSize        = 9;
                    nameLbl.style.color           = Color.white;
                    nameLbl.style.unityTextAlign  = TextAnchor.MiddleCenter;
                    nameLbl.style.whiteSpace      = WhiteSpace.Normal;
                    nameLbl.style.width           = Length.Percent(100);
                    nameLbl.style.height          = Length.Percent(100);
                    slot.Add(nameLbl);
                }
            }
            else
            {
                slot.AddToClassList("item-slot--empty");
                slot.style.backgroundImage = StyleKeyword.Null;
                var placeholder = new Label("?");
                placeholder.AddToClassList("item-slot-placeholder");
                slot.Add(placeholder);
            }
        }

        UpdateItemSelection();
        if (isOpen || isAltMode) AddNumberHints();
    }

    void UpdateItemSelection()
    {
        var inv = PlayerInventory.Instance;
        for (int i = 0; i < itemSlots.Count; i++)
        {
            bool isSelected = i == selectedItemIndex && inv != null && i < inv.items.Count;
            if (isSelected)
                itemSlots[i].AddToClassList("item-slot--selected");
            else
                itemSlots[i].RemoveFromClassList("item-slot--selected");
        }
    }

    void SelectItemDelta(int delta)
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || inv.items.Count == 0) return;

        selectedItemIndex = (selectedItemIndex + delta + inv.items.Count) % inv.items.Count;
        UpdateItemSelection();
    }

    void UseSelectedItem()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || inv.items.Count == 0) return;

        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, inv.items.Count - 1);
        inv.items[selectedItemIndex].Use();

        if (isOpen) RefreshItemSlots();
    }

    void QuickUseItem()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || !inv.HasItems) return;

        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, inv.items.Count - 1);
        PlayerItem item = inv.items[selectedItemIndex];
        LogInterface.Add($"Used: {item.itemName}", Color.green);
        item.Use();
    }

    // ----------------------------------------------------------------
    //  Drag-and-drop (перестановка предметов)
    // ----------------------------------------------------------------

    void BeginDrag(int sourceIndex, Vector2 startPos)
    {
        dragPending   = true;
        dragSourceIdx = sourceIndex;
        dragStartPos  = startPos;
    }

    void CommitDrag(Vector2 mousePos)
    {
        dragPending = false;
        isDragging  = true;

        itemSlots[dragSourceIdx].AddToClassList("item-slot--dragging");
        CreateDragGhost(dragSourceIdx, mousePos);
    }

    void UpdateDrag(Vector2 mousePos)
    {
        // Двигаем призрак за курсором
        float w = dragGhost.resolvedStyle.width;
        float h = dragGhost.resolvedStyle.height;
        dragGhost.style.left = mousePos.x - (w > 0 ? w * 0.5f : 40f);
        dragGhost.style.top  = mousePos.y - (h > 0 ? h * 0.5f : 40f);

        // Подсвечиваем слот под курсором
        int newTarget = FindSlotAtPosition(mousePos);
        if (newTarget == dropTargetIdx) return;

        if (dropTargetIdx >= 0 && dropTargetIdx < itemSlots.Count)
            itemSlots[dropTargetIdx].RemoveFromClassList("item-slot--drop-target");

        dropTargetIdx = newTarget;

        if (dropTargetIdx >= 0 && dropTargetIdx < itemSlots.Count)
            itemSlots[dropTargetIdx].AddToClassList("item-slot--drop-target");
    }

    void EndDrag(Vector2 mousePos)
    {
        int targetIndex = FindSlotAtPosition(mousePos);

        // Убираем визуальные состояния
        itemSlots[dragSourceIdx].RemoveFromClassList("item-slot--dragging");
        if (dropTargetIdx >= 0 && dropTargetIdx < itemSlots.Count)
            itemSlots[dropTargetIdx].RemoveFromClassList("item-slot--drop-target");

        dragGhost?.RemoveFromHierarchy();
        dragGhost = null;

        // Перестановка предметов
        var inv = PlayerInventory.Instance;
        if (targetIndex >= 0 && targetIndex != dragSourceIdx
            && inv != null
            && dragSourceIdx < inv.items.Count
            && targetIndex  < inv.items.Count)
        {
            var moved = inv.items[dragSourceIdx];
            inv.items.RemoveAt(dragSourceIdx);
            inv.items.Insert(targetIndex, moved);

            // Обновляем selectedItemIndex под перемещение
            if      (selectedItemIndex == dragSourceIdx)                              selectedItemIndex = targetIndex;
            else if (dragSourceIdx < selectedItemIndex && targetIndex >= selectedItemIndex) selectedItemIndex--;
            else if (dragSourceIdx > selectedItemIndex && targetIndex <= selectedItemIndex) selectedItemIndex++;

            RefreshItemSlots();
        }

        justDragged   = true;
        isDragging    = false;
        dragPending   = false;
        dragSourceIdx = -1;
        dropTargetIdx = -1;
    }

    void CreateDragGhost(int sourceIndex, Vector2 mousePos)
    {
        var sourceSlot = itemSlots[sourceIndex];
        var inv        = PlayerInventory.Instance;

        dragGhost = new VisualElement();
        dragGhost.AddToClassList("item-slot");
        dragGhost.style.position = Position.Absolute;
        dragGhost.style.opacity  = 0.75f;
        dragGhost.style.scale    = new StyleScale(new Scale(new Vector3(1.1f, 1.1f, 1f)));
        dragGhost.pickingMode    = PickingMode.Ignore;

        float w = sourceSlot.resolvedStyle.width;
        float h = sourceSlot.resolvedStyle.height;
        if (w <= 0) w = 80f;
        if (h <= 0) h = 80f;
        dragGhost.style.width  = w;
        dragGhost.style.height = h;

        if (inv != null && sourceIndex < inv.items.Count)
        {
            PlayerItem item = inv.items[sourceIndex];
            if (item.itemIcon != null)
                dragGhost.style.backgroundImage = new StyleBackground(item.itemIcon);

            var nameLbl = new Label(item.itemName);
            nameLbl.style.fontSize       = 9;
            nameLbl.style.color          = Color.white;
            nameLbl.style.unityTextAlign = TextAnchor.LowerCenter;
            nameLbl.style.whiteSpace     = WhiteSpace.Normal;
            dragGhost.Add(nameLbl);
        }

        dragGhost.style.left = mousePos.x - w * 0.5f;
        dragGhost.style.top  = mousePos.y - h * 0.5f;

        root.Add(dragGhost);
    }

    int FindSlotAtPosition(Vector2 mousePos)
    {
        var inv      = PlayerInventory.Instance;
        int maxIndex = inv != null ? inv.items.Count : 0;
        for (int i = 0; i < itemSlots.Count && i < maxIndex; i++)
            if (itemSlots[i].worldBound.Contains(mousePos))
                return i;
        return -1;
    }

    // ----------------------------------------------------------------
    //  Hints panel
    // ----------------------------------------------------------------

    void BuildHintsPanel()
    {
        hintsPanel = new VisualElement();
        hintsPanel.style.position            = Position.Absolute;
        hintsPanel.style.bottom              = new StyleLength(10f);
        hintsPanel.style.left                = new StyleLength(10f);
        hintsPanel.style.backgroundColor     = new StyleColor(new Color(0f, 0f, 0f, 0.55f));
        hintsPanel.style.paddingTop          = new StyleLength(10f);
        hintsPanel.style.paddingBottom       = new StyleLength(10f);
        hintsPanel.style.paddingLeft         = new StyleLength(14f);
        hintsPanel.style.paddingRight        = new StyleLength(14f);
        hintsPanel.style.borderTopLeftRadius     = new StyleLength(5f);
        hintsPanel.style.borderTopRightRadius    = new StyleLength(5f);
        hintsPanel.style.borderBottomLeftRadius  = new StyleLength(5f);
        hintsPanel.style.borderBottomRightRadius = new StyleLength(5f);
        hintsPanel.pickingMode = PickingMode.Ignore;

        (string key, string action)[] rows =
        {
            ("TAB",           "Open-Close Inventory"),
            ("Q",             "Items menu (Hold)"),
            ("Q + 1–9 / LMC", "Select Item"),
            ("F",             "Use Selected Item"),
            ("1–9 OR SCROLL", "Change Weapon"),
            ("LMC + drag",    "Change Item Position"),
        };

        foreach (var (key, action) in rows)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = new StyleLength(4f);
            row.pickingMode         = PickingMode.Ignore;

            var keyLbl = new Label(key);
            keyLbl.style.color                   = new Color(1f, 0.85f, 0.2f, 1f);
            keyLbl.style.unityFontStyleAndWeight  = FontStyle.Bold;
            keyLbl.style.fontSize                 = 11;
            keyLbl.style.minWidth                 = new StyleLength(85f);
            keyLbl.pickingMode                    = PickingMode.Ignore;

            var sep = new Label("→");
            sep.style.color        = new Color(1f, 1f, 1f, 0.4f);
            sep.style.fontSize     = 11;
            sep.style.marginLeft   = new StyleLength(4f);
            sep.style.marginRight  = new StyleLength(8f);
            sep.pickingMode        = PickingMode.Ignore;

            var actLbl = new Label(action);
            actLbl.style.color     = new Color(1f, 1f, 1f, 0.75f);
            actLbl.style.fontSize  = 11;
            actLbl.pickingMode     = PickingMode.Ignore;

            row.Add(keyLbl);
            row.Add(sep);
            row.Add(actLbl);
            hintsPanel.Add(row);
        }
    }

    public void OnItemPickedUp()
    {
        if (isOpen || isAltMode)
            RefreshItemSlots();
    }
}