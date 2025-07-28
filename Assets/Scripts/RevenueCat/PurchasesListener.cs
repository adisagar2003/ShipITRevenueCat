using System;
using System.Collections.Generic;
using UnityEngine;

public class PurchasesListener : Purchases.UpdatedCustomerInfoListener
{
    public delegate void PurchaseCompletedDelegate(bool success);
    public event PurchaseCompletedDelegate OnPurchaseCompleted;
    public Purchases.Package currentPackage;

    // Update BeginPurchase method
    public void BeginPurchase(Purchases.Package package)
    {
        var purchases = GetComponent<Purchases>();
        purchases.PurchasePackage(package, (purchaseResult) =>
        {
            if (!purchaseResult.UserCancelled)
            {
                if (purchaseResult.Error != null)
                {
                    // show error
                    OnPurchaseCompleted?.Invoke(false);
                }
                else
                {
                    // show updated Customer Info
                    OnPurchaseCompleted?.Invoke(true);
                }
            }
            else
            {
                // user cancelled, don't show an error
                OnPurchaseCompleted?.Invoke(false);
            }
        });
    }

    public override void CustomerInfoReceived(Purchases.CustomerInfo customerInfo)
    {
        // display new CustomerInfo
    }

    private void Start()
    {
        var purchases = GetComponent<Purchases>();
        purchases.SetLogLevel(Purchases.LogLevel.Debug); // Use logLevel instead of SetDebugLogsEnabled
        purchases.GetOfferings((offerings, error) =>
        {
            if (error != null)
            {
                // show error
            }
            else
            {
                // show offering
            }
        });
    }

    void RestoreClicked()
    {
        var purchases = GetComponent<Purchases>();
        purchases.RestorePurchases((customerInfo, error) =>
        {
            if (error != null)
            {
                // show error
            }
            else
            {
                // show updated Customer Info
            }
        });
    }
}
