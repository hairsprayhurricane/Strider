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

    private VisualElement root;
    private VisualElement weaponPanel;
    private VisualElement itemPanel;
    private VisualElement ammoPanel;

    private readonly List<VisualElement> weaponSlots = new();
    private int activeSlotIndex = -1;

    private Coroutine[] slideCoroutines = new Coroutine[3];
    private bool isOpen = false;

    void Awake()
    {
        root = uiDocument.rootVisualElement;
        BuildPanels();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) Open();
        if (Input.GetKeyUp(KeyCode.Tab))   Close();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchWeapon(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchWeapon(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchWeapon(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchWeapon(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SwitchWeapon(8);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SwitchWeapon(9);

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

        // ТОЧНЫЕ размеры из USS (960px, 630px, 180px)
        PositionWeaponPanel(weaponPanel);
        PositionItemPanel(itemPanel);
        PositionAmmoPanel(ammoPanel);

        // Спрятать за экран
        SetTranslate(weaponPanel, 0, -Screen.height);
        SetTranslate(itemPanel,   0,  Screen.height);
        SetTranslate(ammoPanel,   Screen.width, 0);

        root.Add(weaponPanel);
        root.Add(itemPanel);
        root.Add(ammoPanel);

        BuildWeaponSlots();
    }

    void PositionWeaponPanel(VisualElement panel)
    {
        panel.style.position = Position.Absolute;
        panel.style.top = new StyleLength(0f);
        panel.style.left = Length.Percent(50);
        panel.style.marginLeft = new StyleLength(-480f);  // 960px / 2
    }

    void PositionItemPanel(VisualElement panel)
    {
        panel.style.position = Position.Absolute;
        panel.style.bottom = new StyleLength(0f);
        panel.style.left = Length.Percent(50);
        panel.style.marginLeft = new StyleLength(-315f);  // 630px / 2
    }

    void PositionAmmoPanel(VisualElement panel)
    {
        panel.style.position = Position.Absolute;
        panel.style.right = new StyleLength(0f);
        panel.style.top = Length.Percent(50);
        panel.style.marginTop = new StyleLength(-90f);    // 180px / 2
    }


    public enum PositionEdge { Top, Bottom, Right, Left }


    void BuildWeaponSlots()
    {
        VisualElement container = weaponPanel.Q("slots-container");
        if (container == null) return;

        container.Clear();
        weaponSlots.Clear();

        for (int i = 1; i <= 9; i++)
        {
            var slot = new VisualElement();
            slot.AddToClassList("weapon-slot");
            slot.AddToClassList("weapon-slot--empty");

            var num  = new Label(i.ToString()); num.name  = "slot-number"; num.AddToClassList("slot-number");
            var name = new Label("— empty —");  name.name = "weapon-name"; name.AddToClassList("weapon-name"); name.AddToClassList("weapon-name--empty");
            var ammo = new Label("");            ammo.name = "weapon-ammo"; ammo.AddToClassList("weapon-ammo");

            slot.Add(num); slot.Add(name); slot.Add(ammo);
            container.Add(slot);
            weaponSlots.Add(slot);

            int captured = i;
            slot.RegisterCallback<ClickEvent>(_ => SwitchWeapon(captured));
        }
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
        Vector2 from = OffscreenOffset(dir);
        Vector2 to   = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 pos = Vector2.Lerp(from, to, t);
            SetTranslate(panel, pos.x, pos.y);
            yield return null;
        }
        SetTranslate(panel, 0, 0);
    }

    IEnumerator SlideOut(VisualElement panel, SlideDirection dir)
    {
        Vector2 from = Vector2.zero;
        Vector2 to   = OffscreenOffset(dir);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            Vector2 pos = Vector2.Lerp(from, to, t);
            SetTranslate(panel, pos.x, pos.y);
            yield return null;
        }
        SetTranslate(panel, to.x, to.y);
    }

    void StopCoroutines()
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
        StopCoroutines();
        RefreshAll();
        slideCoroutines[0] = StartCoroutine(SlideIn(weaponPanel, SlideDirection.FromTop));
        slideCoroutines[1] = StartCoroutine(SlideIn(itemPanel,   SlideDirection.FromBottom));
        slideCoroutines[2] = StartCoroutine(SlideIn(ammoPanel,   SlideDirection.FromRight));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StopCoroutines();
        slideCoroutines[0] = StartCoroutine(SlideOut(weaponPanel, SlideDirection.FromTop));
        slideCoroutines[1] = StartCoroutine(SlideOut(itemPanel,   SlideDirection.FromBottom));
        slideCoroutines[2] = StartCoroutine(SlideOut(ammoPanel,   SlideDirection.FromRight));
    }

    public void Toggle() { if (isOpen) Close(); else Open(); }
    public bool IsOpen => isOpen;

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
        for (int step = 1; step <= 9; step++)
        {
            int candidate = ((current - 1 + dir * step + 9) % 9) + 1;
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
    }

    void RefreshWeaponSlots()
    {
        GunController gc = GunController.Instance;
        if (gc == null) return;

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            int slotIndex      = i + 1;
            VisualElement slot = weaponSlots[i];
            Label nameLabel    = slot.Q<Label>("weapon-name");
            Label ammoLabel    = slot.Q<Label>("weapon-ammo");
            PlayerGun gun      = gc.TryGetWeaponAt(slotIndex);

            if (gun != null)
            {
                slot.RemoveFromClassList("weapon-slot--empty");
                nameLabel.RemoveFromClassList("weapon-name--empty");
                nameLabel.text = gun.GetGunName();
                ammoLabel.text = gun.GetAmmoDisplayString(gc);
            }
            else
            {
                slot.AddToClassList("weapon-slot--empty");
                nameLabel.AddToClassList("weapon-name--empty");
                nameLabel.text = "— empty —";
                ammoLabel.text = "";
            }
        }

        SetActiveSlot(gc.GetCurrentGunIndex());
    }

    void SetActiveSlot(int index)
    {
        if (activeSlotIndex >= 1 && activeSlotIndex - 1 < weaponSlots.Count)
            weaponSlots[activeSlotIndex - 1].RemoveFromClassList("weapon-slot--active");

        activeSlotIndex = index;

        if (index >= 1 && index - 1 < weaponSlots.Count)
            weaponSlots[index - 1].AddToClassList("weapon-slot--active");
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
}