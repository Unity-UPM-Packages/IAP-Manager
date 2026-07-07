using TheLegends.Base.IAP;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace com.thelegends.iap.manager
{
    public class DemoManager : MonoBehaviour
    {
        public Button initBtn;
        public Button buyConsumableBtn;
        public Button buyNonConsumableBtn;
        public Button buySubscriptionBtn;
        public Button isOwnedBtn;
        public Button isSubscribedBtn;
        public Button restoreBtn;

        void OnEnable()
        {
            initBtn.onClick.AddListener(() =>
            {
                IAPManager.Instance.Init();
                IAPManager.OnPurchaseSuccess += OnPurchaseSuccess;
            });

            buyConsumableBtn.onClick.AddListener(() =>
            {
                IAPManager.Instance.PurchaseProduct("com.thelegends.iap.consumable");
            });

            buyNonConsumableBtn.onClick.AddListener(() =>
            {
                IAPManager.Instance.PurchaseProduct("com.thelegends.iap.non_consumable");
                IAPManager.Instance.RefreshEntitlementStatus("com.thelegends.iap.non_consumable");
            });

            buySubscriptionBtn.onClick.AddListener(() =>
            {
                IAPManager.Instance.PurchaseProduct("com.thelegends.iap.subscription");
                IAPManager.Instance.RefreshEntitlementStatus("com.thelegends.iap.subscription");
            });

            isOwnedBtn.onClick.AddListener(() =>
            {
                bool isOwned = IAPManager.Instance.IsEntitled("com.thelegends.iap.non_consumable");
                Debug.Log("Is Non-Consumable Owned: " + isOwned);
            });

            isSubscribedBtn.onClick.AddListener(() =>
            {
                bool isSubscribed = IAPManager.Instance.IsEntitled("com.thelegends.iap.subscription");
                Debug.Log("Is Subscription Active: " + isSubscribed);
            });
            
            restoreBtn.onClick.AddListener(() =>
            {
                IAPManager.Instance.RestorePurchases();
            });
        }

        void OnDisable()
        {

        }
        
        void OnPurchaseSuccess(Product product)
        {
            switch (product.definition.id)
            {
                case "com.thelegends.iap.consumable":
                    Debug.Log("Consumable purchased!");
                    break;
                case "com.thelegends.iap.non_consumable":
                    Debug.Log("Non-Consumable purchased!");
                    break;
                case "com.thelegends.iap.subscription":
                    Debug.Log("Subscription purchased!");
                    break;
            }
        }

    }
}
