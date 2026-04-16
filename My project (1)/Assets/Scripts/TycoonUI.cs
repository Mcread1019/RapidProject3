using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Layout:
///   TOP BAR    – resource stockpile display (name, amount / max, fill bar)
///   LEFT PANEL – scrollable building shop
///   CENTER     – empire grid (visual representation of purchased buildings)
///   BOTTOM BAR – empire level label + progress bar
/// </summary>
public class TycoonUI : MonoBehaviour
{
    // ── References set during Init ────────────────────────────────────────
    private ResourceManager rm;
    private BuildingManager bm;

    // Resource bar widgets (one per ResourceType)
    private readonly Dictionary<ResourceType, ResourceWidget> resourceWidgets = new();

    // Shop button widgets (one per BuildingDefinition)
    private readonly List<ShopRow> shopRows = new();

    // Empire grid cells
    private readonly List<GameObject> gridCells = new();
    private const int GridColumns   = 8;
    private const int MaxGridCells  = 80;

    // Bottom bar
    private TextMeshProUGUI empireNameLabel;
    private Image           empireProgressFill;
    private TextMeshProUGUI empireLevelLabel;

    // Canvas root
    private Canvas canvas;

    // ── Colours / style constants ─────────────────────────────────────────
    private static readonly Color PanelBg      = new Color(0.10f, 0.10f, 0.12f, 0.95f);
    private static readonly Color HeaderBg     = new Color(0.06f, 0.06f, 0.08f, 1.00f);
    private static readonly Color CellEmpty    = new Color(0.15f, 0.15f, 0.18f, 1.00f);
    private static readonly Color ButtonActive = new Color(0.22f, 0.55f, 0.22f, 1.00f);
    private static readonly Color ButtonLocked = new Color(0.35f, 0.35f, 0.35f, 1.00f);
    private static readonly Color TextLight    = new Color(0.92f, 0.92f, 0.92f, 1.00f);
    private static readonly Color TextDim      = new Color(0.55f, 0.55f, 0.60f, 1.00f);
    private static readonly Color GoldBar      = new Color(1.00f, 0.78f, 0.08f, 1.00f);

    // ─────────────────────────────────────────────────────────────────────
    //  Initialization
    // ─────────────────────────────────────────────────────────────────────

    public void Init(ResourceManager resourceManager, BuildingManager buildingManager)
    {
        rm = resourceManager;
        bm = buildingManager;

        BuildCanvas();
        BuildTopBar();
        BuildShopPanel();
        BuildEmpireGrid();
        BuildBottomBar();

        // Wire up events
        rm.OnResourceChanged  += OnResourceChanged;
        bm.OnBuildingPurchased += OnBuildingPurchased;
        bm.OnTick              += OnTick;

        RefreshAllResources();
        RefreshShop();
        RefreshEmpireBar();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Canvas & root panels
    // ─────────────────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var go = new GameObject("TycoonCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

      
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TOP BAR – resource display
    // ─────────────────────────────────────────────────────────────────────

    private void BuildTopBar()
    {
        float barHeight = 150f;

        var bar = MakePanel(canvas.transform, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -barHeight), new Vector2(0f, 0f));
        SetColor(bar, HeaderBg);

        // Title label on the far left
        var title = MakeText(bar.transform, "Title", "TYCOON EMPIRE",
            new Vector2(0f, 0f), new Vector2(240f, barHeight));
        title.fontSize         = 28;
        title.fontStyle        = FontStyles.Bold;
        title.color            = GoldBar;
        title.alignment        = TextAlignmentOptions.Center;
        AnchorFill(title.rectTransform, 0f, 0f, 240f / 1920f, 1f);

        // One resource widget per resource type, evenly spaced
        var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
        float slotWidth  = (1f - 240f / 1920f) / resourceTypes.Length;

        for (int i = 0; i < resourceTypes.Length; i++)
        {
            var rt    = resourceTypes[i];
            float xMin = 240f / 1920f + i * slotWidth;
            float xMax = xMin + slotWidth;

            var slot = MakePanel(bar.transform, $"ResSlot_{rt}",
                new Vector2(xMin, 0f), new Vector2(xMax, 1f),
                Vector2.zero, Vector2.zero);
            SetColor(slot, Color.clear);

            var widget = new ResourceWidget();
            widget.Build(slot.transform, rt, barHeight);
            resourceWidgets[rt] = widget;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  LEFT PANEL – building shop
    // ─────────────────────────────────────────────────────────────────────

    private void BuildShopPanel()
    {
        float topBarHeight  = 150f;
        float bottomBarH    = 100f;
        float panelWidth    = 480f;

        var panel = MakePanel(canvas.transform, "ShopPanel",
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, bottomBarH), new Vector2(panelWidth, -topBarHeight));
        SetColor(panel, PanelBg);

        // Header
        var header = MakePanel(panel.transform, "ShopHeader",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -56f), new Vector2(0f, 0f));
        SetColor(header, HeaderBg);
        var hLabel = MakeText(header.transform, "ShopTitle", "BUILDINGS",
            Vector2.zero, Vector2.zero);
        hLabel.fontSize   = 24;
        hLabel.fontStyle  = FontStyles.Bold;
        hLabel.color      = GoldBar;
        hLabel.alignment  = TextAlignmentOptions.Center;
        AnchorFill(hLabel.rectTransform, 0f, 0f, 1f, 1f);

        // Scroll view for shop rows
        var scrollGO = new GameObject("ShopScroll");
        scrollGO.transform.SetParent(panel.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollRT   = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(0f, 0f);
        scrollRT.offsetMax = new Vector2(0f, -56f);

        // Viewport — use RectMask2D (clips by rect, no stencil/alpha dependency)
        var viewport = MakePanel(scrollGO.transform, "Viewport",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        SetColor(viewport, Color.clear);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Content container
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT  = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset(10, 10, 8, 8);
        vlg.spacing              = 8f;
        vlg.childControlWidth    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight   = true;   // VLG must set heights or rows stay 0px tall
        vlg.childForceExpandHeight = false;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content              = contentRT;
        scrollRect.vertical             = true;
        scrollRect.horizontal           = false;
        scrollRect.scrollSensitivity    = 30f;
        scrollRect.verticalNormalizedPosition = 1f; // start scrolled to top

        // Build one row per building definition
        foreach (var def in BuildingDatabase.All)
        {
            var row = new ShopRow();
            row.Build(content.transform, def);
            row.OnBuyClicked = () => TryBuy(def);
            shopRows.Add(row);
        }

        // Force an immediate layout pass so rows have correct sizes on frame 1
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CENTER – empire grid
    // ─────────────────────────────────────────────────────────────────────

    private void BuildEmpireGrid()
    {
        float topBarHeight = 150f;
        float bottomBarH   = 100f;
        float shopWidth    = 480f;

        var gridPanel = MakePanel(canvas.transform, "EmpireGrid",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(shopWidth, bottomBarH), new Vector2(0f, -topBarHeight));
        SetColor(gridPanel, new Color(0.07f, 0.07f, 0.09f, 1f));

        // Title
        var titlePanel = MakePanel(gridPanel.transform, "GridHeader",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -44f), new Vector2(0f, 0f));
        SetColor(titlePanel, HeaderBg);
        var tl = MakeText(titlePanel.transform, "GridTitle", "YOUR EMPIRE",
            Vector2.zero, Vector2.zero);
        tl.fontSize  = 20;
        tl.fontStyle = FontStyles.Bold;
        tl.color     = TextDim;
        tl.alignment = TextAlignmentOptions.Center;
        AnchorFill(tl.rectTransform, 0f, 0f, 1f, 1f);

        // Grid content area
        var gridContent = new GameObject("GridContent");
        gridContent.transform.SetParent(gridPanel.transform, false);
        var gcRT = gridContent.AddComponent<RectTransform>();
        gcRT.anchorMin = Vector2.zero;
        gcRT.anchorMax = Vector2.one;
        gcRT.offsetMin = new Vector2(10f, 10f);
        gcRT.offsetMax = new Vector2(-10f, -44f);

        var glg = gridContent.AddComponent<GridLayoutGroup>();
        glg.padding         = new RectOffset(6, 6, 6, 6);
        glg.spacing         = new Vector2(8f, 8f);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = GridColumns;
        glg.childAlignment  = TextAnchor.UpperLeft;
        glg.cellSize        = new Vector2(90f, 90f);

        // Pre-create all cells
        for (int i = 0; i < MaxGridCells; i++)
        {
            var cell = new GameObject($"Cell_{i}");
            cell.transform.SetParent(gridContent.transform, false);
            var img = cell.AddComponent<Image>();
            img.color = CellEmpty;

            // Label inside the cell
            var lbl = MakeText(cell.transform, "CellLabel", "",
                Vector2.zero, Vector2.zero);
            lbl.fontSize  = 16;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color     = TextLight;
            lbl.alignment = TextAlignmentOptions.Center;
            AnchorFill(lbl.rectTransform, 0f, 0f, 1f, 1f);

            gridCells.Add(cell);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  BOTTOM BAR – empire progress
    // ─────────────────────────────────────────────────────────────────────

    private void BuildBottomBar()
    {
        float barHeight = 100f;

        var bar = MakePanel(canvas.transform, "BottomBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, barHeight));
        SetColor(bar, HeaderBg);

        // Empire name label (left)
        empireNameLabel = MakeText(bar.transform, "EmpireName", "",
            Vector2.zero, Vector2.zero);
        empireNameLabel.fontSize  = 26;
        empireNameLabel.fontStyle = FontStyles.Bold;
        empireNameLabel.color     = GoldBar;
        empireNameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        AnchorFill(empireNameLabel.rectTransform, 0f, 0f, 0.25f, 1f);
        empireNameLabel.rectTransform.offsetMin = new Vector2(20f, 0f);

        // Level label (right of name)
        empireLevelLabel = MakeText(bar.transform, "LevelLabel", "",
            Vector2.zero, Vector2.zero);
        empireLevelLabel.fontSize  = 18;
        empireLevelLabel.color     = TextDim;
        empireLevelLabel.alignment = TextAlignmentOptions.MidlineLeft;
        AnchorFill(empireLevelLabel.rectTransform, 0.25f, 0f, 0.45f, 1f);

        // Progress bar background
        var pbBg = MakePanel(bar.transform, "ProgressBg",
            new Vector2(0.45f, 0.2f), new Vector2(0.95f, 0.8f),
            Vector2.zero, Vector2.zero);
        SetColor(pbBg, new Color(0.20f, 0.20f, 0.22f, 1f));

        // Progress bar fill
        var fillGO = new GameObject("ProgressFill");
        fillGO.transform.SetParent(pbBg.transform, false);
        empireProgressFill       = fillGO.AddComponent<Image>();
        empireProgressFill.color = GoldBar;
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fillRT.pivot     = new Vector2(0f, 0.5f);

        // Helper label inside progress bar
        var progressLabel = MakeText(pbBg.transform, "ProgressLabel",
            "Expand your empire", Vector2.zero, Vector2.zero);
        progressLabel.fontSize  = 14;
        progressLabel.color     = TextLight;
        progressLabel.alignment = TextAlignmentOptions.Center;
        AnchorFill(progressLabel.rectTransform, 0f, 0f, 1f, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Event handlers
    // ─────────────────────────────────────────────────────────────────────

    private void OnResourceChanged(ResourceType type, float amount, float max)
    {
        if (resourceWidgets.TryGetValue(type, out var w))
            w.Refresh(amount, max);
        RefreshShop();
    }

    private void OnBuildingPurchased(BuildingDefinition def)
    {
        RefreshGrid();
        RefreshEmpireBar();
    }

    private void OnTick(IReadOnlyList<PlacedBuilding> buildings)
    {
        // Grid and empire bar are already up to date via OnBuildingPurchased;
        // the resource widgets refresh via OnResourceChanged from the ResourceManager.
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Refresh methods
    // ─────────────────────────────────────────────────────────────────────

    private void RefreshAllResources()
    {
        foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
        {
            if (resourceWidgets.TryGetValue(rt, out var w))
                w.Refresh(rm.Get(rt), rm.GetMax(rt));
        }
    }

    private void RefreshShop()
    {
        foreach (var row in shopRows)
            row.RefreshAffordability(rm);
    }

    private void RefreshGrid()
    {
        var buildings = bm.PlacedBuildings;
        for (int i = 0; i < gridCells.Count; i++)
        {
            var cell = gridCells[i];
            var img  = cell.GetComponent<Image>();
            var lbl  = cell.GetComponentInChildren<TextMeshProUGUI>();

            if (i < buildings.Count)
            {
                img.color = buildings[i].Definition.TileColor;
                lbl.text  = buildings[i].Definition.TileLabel;
            }
            else
            {
                img.color = CellEmpty;
                lbl.text  = "";
            }
        }
    }

    private void RefreshEmpireBar()
    {
        int   level    = bm.EmpireLevel();
        float progress = bm.EmpireLevelProgress();

        empireNameLabel.text  = BuildingManager.LevelName(level);
        empireLevelLabel.text = $"Level {level}  ({bm.TotalBuildingCount} buildings)";

        var fillRT = empireProgressFill.GetComponent<RectTransform>();
        fillRT.anchorMax = new Vector2(progress, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Buy action
    // ─────────────────────────────────────────────────────────────────────

    private void TryBuy(BuildingDefinition def)
    {
        if (bm.TryPurchase(def))
        {
            RefreshAllResources();
            RefreshShop();
            RefreshGrid();
            RefreshEmpireBar();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UI helper factories
    // ─────────────────────────────────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        var rt    = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name,
        string text, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = 16;
        tmp.color    = TextLight;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta      = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        return tmp;
    }

    private static void SetColor(GameObject go, Color c)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static void AnchorFill(RectTransform rt,
        float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Inner classes (widgets)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>One resource column in the top bar.</summary>
    private class ResourceWidget
    {
        private TextMeshProUGUI nameLabel;
        private TextMeshProUGUI amountLabel;
        private Image           fillBar;

        public void Build(Transform parent, ResourceType rt, float parentHeight)
        {
            // Use an Image on the container so Unity creates a proper RectTransform
            var col = new GameObject($"ResCol_{rt}");
            col.transform.SetParent(parent, false);
            col.AddComponent<Image>().color = Color.clear;
            var colRT = col.GetComponent<RectTransform>();
            colRT.anchorMin = Vector2.zero;
            colRT.anchorMax = Vector2.one;
            colRT.offsetMin = new Vector2(4f, 4f);
            colRT.offsetMax = new Vector2(-4f, -4f);

            // Colour accent strip at top (6px)
            var strip = new GameObject("Strip");
            strip.transform.SetParent(col.transform, false);
            var stripImg = strip.AddComponent<Image>();
            stripImg.color = ResourceInfo.GetColor(rt);
            var stripRT = strip.GetComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0f, 1f);
            stripRT.anchorMax = new Vector2(1f, 1f);
            stripRT.offsetMin = new Vector2(0f, -6f);
            stripRT.offsetMax = Vector2.zero;

            // Resource name — parent first, then AddComponent (TMP requirement)
            nameLabel = MakeText(col.transform, "Name", ResourceInfo.GetName(rt),
                Vector2.zero, Vector2.zero);
            nameLabel.fontSize  = 17;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color     = ResourceInfo.GetColor(rt);
            nameLabel.alignment = TextAlignmentOptions.Center;
            AnchorFill(nameLabel.rectTransform, 0f, 0.55f, 1f, 0.95f);

            // Amount label — parent first
            amountLabel = MakeText(col.transform, "Amount", "0 / 200",
                Vector2.zero, Vector2.zero);
            amountLabel.fontSize  = 18;
            amountLabel.color     = TextLight;
            amountLabel.alignment = TextAlignmentOptions.Center;
            AnchorFill(amountLabel.rectTransform, 0f, 0.22f, 1f, 0.58f);

            // Progress bar background
            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(col.transform, false);
            barBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);
            var barBgRT = barBg.GetComponent<RectTransform>();
            barBgRT.anchorMin = new Vector2(0.05f, 0f);
            barBgRT.anchorMax = new Vector2(0.95f, 0.22f);
            barBgRT.offsetMin = new Vector2(0f, 4f);
            barBgRT.offsetMax = new Vector2(0f, -2f);

            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(barBg.transform, false);
            fillBar       = fill.AddComponent<Image>();
            fillBar.color = ResourceInfo.GetColor(rt);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot     = new Vector2(0f, 0.5f);
        }

        public void Refresh(float amount, float max)
        {
            amountLabel.text = $"{(int)amount} / {(int)max}";
            float t = max > 0 ? Mathf.Clamp01(amount / max) : 0f;
            fillBar.rectTransform.anchorMax = new Vector2(t, 1f);
        }
    }

    /// <summary>One row in the building shop scroll list.</summary>
    private class ShopRow
    {
        public Action OnBuyClicked;

        private BuildingDefinition def;
        private Button             buyButton;
        private Image              buyButtonImg;
        private TextMeshProUGUI    buyButtonLabel;
        private TextMeshProUGUI    costLabel;

        public void Build(Transform parent, BuildingDefinition buildingDef)
        {
            def = buildingDef;

            var row = new GameObject($"ShopRow_{def.Id}");
            row.transform.SetParent(parent, false);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = new Color(0.14f, 0.14f, 0.17f, 1f);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 120f;
            rowLE.flexibleWidth   = 1f;

            // Building name
            var nl = MakeText(row.transform, "Name", def.Name, Vector2.zero, Vector2.zero);
            nl.fontSize  = 19;
            nl.fontStyle = FontStyles.Bold;
            nl.color     = TextLight;
            nl.alignment = TextAlignmentOptions.MidlineLeft;
            AnchorFill(nl.rectTransform, 0f, 0.60f, 0.70f, 1f);
            nl.rectTransform.offsetMin = new Vector2(14f, 0f);

            // Colour swatch (left accent strip)
            var swatch = new GameObject("Swatch");
            swatch.transform.SetParent(row.transform, false);
            var swImg = swatch.AddComponent<Image>();
            swImg.color = def.TileColor;
            var swRT = swatch.GetComponent<RectTransform>();
            swRT.anchorMin = new Vector2(0f, 0.08f);
            swRT.anchorMax = new Vector2(0f, 0.92f);
            swRT.offsetMin = new Vector2(4f,  0f);
            swRT.offsetMax = new Vector2(12f, 0f);

            // Description
            var dl = MakeText(row.transform, "Desc", def.Description, Vector2.zero, Vector2.zero);
            dl.fontSize  = 13;
            dl.color     = TextDim;
            dl.alignment = TextAlignmentOptions.MidlineLeft;
            AnchorFill(dl.rectTransform, 0f, 0.33f, 0.70f, 0.62f);
            dl.rectTransform.offsetMin = new Vector2(14f, 0f);

            // Production / consumption summary
            var pl = MakeText(row.transform, "Prod", def.ProductionText(), Vector2.zero, Vector2.zero);
            pl.fontSize  = 13;
            pl.color     = new Color(0.50f, 0.85f, 0.50f, 1f);
            pl.alignment = TextAlignmentOptions.MidlineLeft;
            AnchorFill(pl.rectTransform, 0f, 0.05f, 0.70f, 0.35f);
            pl.rectTransform.offsetMin = new Vector2(14f, 0f);

            // Cost label (below name)
            costLabel = MakeText(row.transform, "Cost", def.CostText(), Vector2.zero, Vector2.zero);
            costLabel.fontSize  = 13;
            costLabel.color     = new Color(0.90f, 0.70f, 0.30f, 1f);
            costLabel.alignment = TextAlignmentOptions.MidlineLeft;
            AnchorFill(costLabel.rectTransform, 0f, 0.60f, 0.70f, 1f);
            costLabel.rectTransform.offsetMin = new Vector2(14f, 0f);
            costLabel.rectTransform.offsetMax = new Vector2(0f, -28f);  // below name

            // Buy button
            var btnGO = new GameObject("BuyBtn");
            btnGO.transform.SetParent(row.transform, false);
            buyButtonImg       = btnGO.AddComponent<Image>();
            buyButtonImg.color = ButtonActive;
            buyButton          = btnGO.AddComponent<Button>();
            buyButton.targetGraphic = buyButtonImg;
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.71f, 0.12f);
            btnRT.anchorMax = new Vector2(0.97f, 0.88f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;

            buyButtonLabel = MakeText(btnGO.transform, "BtnLabel", "BUY",
                Vector2.zero, Vector2.zero);
            buyButtonLabel.fontSize  = 18;
            buyButtonLabel.fontStyle = FontStyles.Bold;
            buyButtonLabel.color     = Color.white;
            buyButtonLabel.alignment = TextAlignmentOptions.Center;
            AnchorFill(buyButtonLabel.rectTransform, 0f, 0f, 1f, 1f);

            buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke());
        }

        public void RefreshAffordability(ResourceManager rm)
        {
            bool canAfford = rm.CanAfford(def.Cost);
            buyButtonImg.color      = canAfford ? ButtonActive : ButtonLocked;
            buyButton.interactable  = canAfford;
        }
    }
}
