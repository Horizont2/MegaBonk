using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    [Header("Hero Database")]
    public HeroData[] heroes;

    [Header("Spawn Points & Camera")]
    public Transform pedestalPos;
    public Transform offscreenLeft;
    public Transform offscreenRight;

    [Header("Animation Settings")]
    public float swipeSpeed = 4f;
    public float rotationSpeed = 500f;

    [Header("Scene Navigation")]
    public string mainMenuSceneName = "MainMenu"; // Впиши сюди точну назву сцени меню

    [Header("UI Main Elements")]
    public TextMeshProUGUI heroNameText;
    public TextMeshProUGUI priceText;
    public GameObject diamondIconOnButton;
    public TextMeshProUGUI diamondBalanceText;

    [Header("UI Control Buttons")]
    public Button buyButton;
    public Button backButton;
    public Button leftArrow;
    public Button rightArrow;

    [Header("UI Sliders & Stats")]
    public Slider hpSlider;
    public Slider speedSlider;
    public Slider radiusSlider;
    public TextMeshProUGUI hpValueText;
    public TextMeshProUGUI speedValueText;
    public TextMeshProUGUI radiusValueText;
    public TextMeshProUGUI hpPercentText;
    public TextMeshProUGUI speedPercentText;
    public TextMeshProUGUI radiusPercentText;

    private int currentIndex = 0;
    private GameObject currentModel;
    private bool isSwapping = false;

    // КЛЮЧІ ДЛЯ ЗБЕРЕЖЕННЯ
    private const string DIAMONDS_KEY = "PlayerDiamonds";
    private const string SELECTED_HERO_KEY = "SelectedHeroID";

    private void Start()
    {
        // 1. Налаштування капіталу
        if (!PlayerPrefs.HasKey(DIAMONDS_KEY)) PlayerPrefs.SetInt(DIAMONDS_KEY, 0);

        // 2. Налаштування стартового героя (перший запуск)
        if (!PlayerPrefs.HasKey(SELECTED_HERO_KEY))
        {
            PlayerPrefs.SetInt(SELECTED_HERO_KEY, 0); // Робимо ID 0 обраним
            PlayerPrefs.SetInt("HeroUnlocked_0", 1);  // Розблоковуємо його
            PlayerPrefs.Save();
        }

        // 3. Спавн поточного обраного героя при вході в магазин
        // Шукаємо індекс героя, який зараз екіпірований
        int savedID = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0);
        for (int i = 0; i < heroes.Length; i++)
        {
            if (heroes[i].heroID == savedID)
            {
                currentIndex = i;
                break;
            }
        }

        if (heroes.Length > 0)
        {
            SpawnHero(currentIndex, pedestalPos.position);
            UpdateUI(false);
        }

        // Прив'язка кнопок
        if (leftArrow != null) leftArrow.onClick.AddListener(PreviousHero);
        if (rightArrow != null) rightArrow.onClick.AddListener(NextHero);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyOrSelectPressed);

        if (backButton != null) backButton.onClick.AddListener(GoToMainMenu);
    }

    private void Update()
    {
        HandleManualRotation();

        if (Input.GetKeyDown(KeyCode.RightArrow)) NextHero();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousHero();
    }

    private void HandleManualRotation()
    {
        if (currentModel != null && !isSwapping)
        {
            if (Input.GetMouseButton(0))
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                currentModel.transform.Rotate(Vector3.up, -rotX);
            }
        }
    }

    private void SpawnHero(int index, Vector3 position)
    {
        if (heroes[index].shopModelPrefab != null)
        {
            currentModel = Instantiate(heroes[index].shopModelPrefab, position, pedestalPos.rotation);
        }
    }

    public void NextHero()
    {
        if (isSwapping || heroes.Length <= 1) return;

        int oldIndex = currentIndex;
        currentIndex = (currentIndex + 1) % heroes.Length; // Оновлюємо індекс ДО анімації
        StartCoroutine(SwapAnimation(oldIndex, currentIndex, false));
    }

    public void PreviousHero()
    {
        if (isSwapping || heroes.Length <= 1) return;

        int oldIndex = currentIndex;
        currentIndex = (currentIndex - 1 + heroes.Length) % heroes.Length; // Оновлюємо індекс ДО анімації
        StartCoroutine(SwapAnimation(oldIndex, currentIndex, true));
    }

    private IEnumerator SwapAnimation(int oldIndex, int newIndex, bool swipeRight)
    {
        isSwapping = true;
        GameObject oldModel = currentModel;
        Vector3 oldTargetPos = swipeRight ? offscreenRight.position : offscreenLeft.position;
        Vector3 newStartPos = swipeRight ? offscreenLeft.position : offscreenRight.position;

        // Створюємо нову модель на основі НОВОГО індексу
        GameObject newModel = null;
        if (heroes[newIndex].shopModelPrefab != null)
        {
            newModel = Instantiate(heroes[newIndex].shopModelPrefab, newStartPos, pedestalPos.rotation);
        }

        currentModel = newModel;
        UpdateUI(true); // Оновлюємо інтерфейс уже для нового героя

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * swipeSpeed;
            float smoothStep = Mathf.SmoothStep(0f, 1f, progress);
            if (oldModel != null) oldModel.transform.position = Vector3.Lerp(pedestalPos.position, oldTargetPos, smoothStep);
            if (newModel != null) newModel.transform.position = Vector3.Lerp(newStartPos, pedestalPos.position, smoothStep);
            yield return null;
        }

        if (oldModel != null) Destroy(oldModel);
        isSwapping = false;
    }

    public void OnBuyOrSelectPressed()
    {
        if (isSwapping) return;

        HeroData currentHero = heroes[currentIndex];
        bool isBought = PlayerPrefs.GetInt("HeroUnlocked_" + currentHero.heroID, currentHero.price == 0 ? 1 : 0) == 1;

        if (!isBought)
        {
            int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY);
            if (myDiamonds >= currentHero.price)
            {
                PlayerPrefs.SetInt(DIAMONDS_KEY, myDiamonds - currentHero.price);
                PlayerPrefs.SetInt("HeroUnlocked_" + currentHero.heroID, 1);
                PlayerPrefs.SetInt(SELECTED_HERO_KEY, currentHero.heroID);
                PlayerPrefs.Save();
                UpdateUI(true);
            }
        }
        else
        {
            PlayerPrefs.SetInt(SELECTED_HERO_KEY, currentHero.heroID);
            PlayerPrefs.Save();
            UpdateUI(true);
        }
    }

    private void UpdateUI(bool animateText)
    {
        HeroData currentHero = heroes[currentIndex];

        // 1. Оновлення балансу
        int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY, 0);
        if (diamondBalanceText != null) diamondBalanceText.text = myDiamonds.ToString("N0");

        // 2. Стан кнопки (BUY / SELECT / EQUIPPED)
        bool isBought = PlayerPrefs.GetInt("HeroUnlocked_" + currentHero.heroID, currentHero.price == 0 ? 1 : 0) == 1;
        bool isSelected = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0) == currentHero.heroID;

        if (isSelected)
        {
            priceText.text = "EQUIPPED";
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.margin = new Vector4(0, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);
            buyButton.interactable = false;
        }
        else if (isBought)
        {
            priceText.text = "SELECT";
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.margin = new Vector4(0, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);
            buyButton.interactable = true;
        }
        else
        {
            priceText.text = "BUY FOR\n" + currentHero.price.ToString("N0");
            priceText.alignment = TextAlignmentOptions.Left; // Зліва направо
            priceText.margin = new Vector4(10, 0, 0, 0); // Мінімальний відступ зліва
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(true);
            buyButton.interactable = true;
        }

        // 3. Статистика
        if (heroNameText != null) heroNameText.text = currentHero.heroName;
        if (hpValueText != null) hpValueText.text = (currentHero.actualMaxHealth * currentHero.hpBarFill).ToString("F0") + "/" + currentHero.actualMaxHealth.ToString("F0");
        if (speedValueText != null) speedValueText.text = currentHero.actualMoveSpeed.ToString("F1") + " m/s";
        if (radiusValueText != null) radiusValueText.text = currentHero.actualBombRadius.ToString("F0") + "m";
        if (hpPercentText != null) hpPercentText.text = Mathf.RoundToInt(currentHero.hpBarFill * 100) + "%";
        if (speedPercentText != null) speedPercentText.text = Mathf.RoundToInt(currentHero.speedBarFill * 100) + "%";
        if (radiusPercentText != null) radiusPercentText.text = Mathf.RoundToInt(currentHero.radiusBarFill * 100) + "%";

        // 4. Слайдери
        if (hpSlider != null) hpSlider.value = currentHero.hpBarFill;
        if (speedSlider != null) speedSlider.value = currentHero.speedBarFill;
        if (radiusSlider != null) radiusSlider.value = currentHero.radiusBarFill;

        if (animateText)
        {
            StartCoroutine(PopText(heroNameText));
            StartCoroutine(PopText(priceText));
            StartCoroutine(PopText(hpValueText));
            StartCoroutine(PopText(speedValueText));
            StartCoroutine(PopText(radiusValueText));
        }
    }

    private IEnumerator PopText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) yield break;
        textComponent.transform.localScale = Vector3.one;
        Vector3 originalScale = Vector3.one;
        Vector3 popScale = new Vector3(1.2f, 1.2f, 1.2f);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.08f;
            textComponent.transform.localScale = Vector3.Lerp(originalScale, popScale, t);
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.15f;
            textComponent.transform.localScale = Vector3.Lerp(popScale, originalScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        textComponent.transform.localScale = originalScale;
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}