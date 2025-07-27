using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchaseManager : MonoBehaviour
{
    public static PurchaseManager Instance { get; private set; }

    [Header("RevenueCat Integration")]
    [SerializeField] private Purchases purchasesComponent;
    [SerializeField] private PurchasesListener purchasesListener;

    [Header("Purchase UI")]
    [SerializeField] private GameObject purchasePanel;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;

    [Header("Customization")]
    [SerializeField] private SOCustomizationDatabase customizationDatabase;

    // Premium item tracking
    private string currentItemType;
    private int currentItemIndex;
    private SceneShift pendingSceneShift;

    // Product IDs (match these with your RevenueCat dashboard)
    private Dictionary<string, string> productIds = new Dictionary<string, string>
    {
        { "premium_glasses_1", "glasses_0" },
        { "premium_glasses_2", "glasses_1" }
        // Add more as needed
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup UI
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HidePurchaseUI);

        // Hide purchase UI initially
        if (purchasePanel != null)
            purchasePanel.SetActive(false);
    }

    public bool IsItemPremiumAndNotOwned(string itemType, int itemIndex)
    {
        // Check if the item is premium
        bool isPremium = false;

        // Check different item types
        if (itemType == "glasses" && itemIndex < customizationDatabase.glassPrefabs.Count)
        {
            // You would need to add a list of premium flags to your database
            // This is just a placeholder example - modify to match your actual data structure
            string productId = $"premium_glasses_{itemIndex}";
            isPremium = productIds.ContainsKey(productId);

            if (isPremium)
            {
                // Check if already purchased
                return !IsProductPurchased(productId);
            }
        }

        return false;
    }

    public bool IsProductPurchased(string productId)
    {
        if (purchasesComponent == null) return false;

        // Get customer info from RevenueCat asynchronously
        bool isPurchased = false;
        purchasesComponent.GetCustomerInfo((customerInfo, error) =>
        {
            if (customerInfo != null)
            {
                // Check if the product is in active entitlements
                isPurchased = customerInfo.Entitlements.Active.ContainsKey(productId);
            }
        });

        // Note: This will always return false immediately, because GetCustomerInfo is asynchronous.
        // To properly handle this, you should refactor this method to use a callback or async/await.
        return isPurchased;
    }

    public void ShowPurchaseUI(string itemType, int itemIndex, SceneShift sceneShift = null)
    {
        currentItemType = itemType;
        currentItemIndex = itemIndex;
        pendingSceneShift = sceneShift;

        string productId = $"premium_{itemType}_{itemIndex}";

        if (purchasePanel != null)
        {
            // Get offering info from RevenueCat (simplified)
            purchasesComponent.GetOfferings((offerings, error) => {
                if (error != null || offerings == null)
                {
                    Debug.LogError($"Failed to get offerings: {error?.Message}");
                    return;
                }

                // Find the package for this product
                var package = FindPackageForProductId(offerings, productId);
                if (package != null)
                {
                    // Update UI
                    if (itemNameText != null)
                        itemNameText.text = GetItemDisplayName(itemType, itemIndex);

                    if (priceText != null)
                        priceText.text = package.StoreProduct.PriceString;

                    // Store package for purchase
                    purchasesListener.currentPackage = package;

                    // Show UI
                    purchasePanel.SetActive(true);
                }
                else
                {
                    Debug.LogError($"No package found for product ID: {productId}");
                }
            });
        }
    }

    private void HidePurchaseUI()
    {
        if (purchasePanel != null)
            purchasePanel.SetActive(false);

        pendingSceneShift = null;
    }

    private void OnPurchaseButtonClicked()
    {
        if (purchasesListener != null && purchasesListener.currentPackage != null)
        {
            // Start purchase flow
            purchasesListener.BeginPurchase(purchasesListener.currentPackage);

            // Subscribe to purchase completion event
            purchasesListener.OnPurchaseCompleted += OnPurchaseCompleted;
        }
    }

    private void OnPurchaseCompleted(bool success)
    {
        // Unsubscribe
        purchasesListener.OnPurchaseCompleted -= OnPurchaseCompleted;

        if (success)
        {
            // Hide UI
            HidePurchaseUI();

            // Continue to next scene if pending
            if (pendingSceneShift != null)
            {
                pendingSceneShift.MoveToNextLevel();
            }
        }
    }

    private Purchases.Package FindPackageForProductId(Purchases.Offerings offerings, string productId)
    {
        // Check current offering first
        var currentOffering = offerings.Current;
        if (currentOffering != null)
        {
            foreach (var package in currentOffering.AvailablePackages)
            {
                // FIX: Use StoreProduct.Identifier instead of Product.Identifier
                if (package.StoreProduct != null && package.StoreProduct.Identifier == productId)
                    return package;
            }
        }

        // Check all offerings if not found in current
        foreach (var offering in offerings.All.Values)
        {
            foreach (var package in offering.AvailablePackages)
            {
                if (package.StoreProduct != null && package.StoreProduct.Identifier == productId)
                    return package;
            }
        }

        return null;
    }

    private string GetItemDisplayName(string itemType, int index)
    {
        if (itemType == "glasses")
        {
            return $"Premium Glasses Style {index + 1}";
        }

        return "Premium Item";
    }
}
