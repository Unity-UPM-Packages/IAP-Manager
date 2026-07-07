using System;
using System.Collections.Generic;
using System.Linq;
using TheLegends.Base.UnitySingleton;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TheLegends.Base.IAP
{
    public class IAPManager : PersistentMonoSingleton<IAPManager>
    {
        // --- Events ---
        public static event Action<Product> OnPurchaseSuccess;
        public static event Action<Product, PurchaseFailureReason> OnPurchaseFailure;
        public static event Action OnRestoreCompleted;
        public static event Action<string, bool> OnEntitlementStateChanged;

        // --- Private Members ---
        private StoreController m_StoreController;
        private LocalReceiptValidator m_ReceiptValidator;
        private readonly Dictionary<string, bool> m_EntitlementStates = new Dictionary<string, bool>();

        // --- Unity Methods ---
        protected override void Awake()
        {
            base.Awake();
            if (IAPSettings.Instance.isAutoInit)
            {
                InitializeIAP();
            }
        }

        public void Init()
        {
            InitializeIAP();
        }

        // --- Initialization ---
        private async void InitializeIAP()
        {
            m_ReceiptValidator = new LocalReceiptValidator();
            m_StoreController = UnityIAPServices.StoreController();
            m_StoreController.ProcessPendingOrdersOnPurchasesFetched(true);

            m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
            m_StoreController.OnProductsFetched += OnProductsFetched;
            m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
            m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
            m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnCheckEntitlement += OnCheckEntitlement;

            Debug.Log("[IAPManager] Connecting to store...");
            await m_StoreController.Connect();

            FetchInitialProducts();
        }

        private void FetchInitialProducts()
        {
            var skuIDs = IAPSettings.Instance.skuIDs;
            var initialProductsToFetch = new List<ProductDefinition>();

            foreach (var skuData in skuIDs.skuIDDatas)
            {
                initialProductsToFetch.Add(new ProductDefinition(skuData.skuID, skuData.productType));
            }

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        // --- IAP Callbacks ---
        private void OnPurchasePending(PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.LogError("[IAPManager] Could not find product in pending order. Confirming to clear queue.");
                m_StoreController.ConfirmPurchase(order);
                return;
            }

            if (IAPSettings.Instance.isUseValidator)
            {
                if (m_ReceiptValidator.Validate(order.Info.Receipt, out string transactionId))
                {
                    if (m_ReceiptValidator.IsTransactionProcessed(transactionId))
                    {
                        Debug.Log($"[IAPManager] Transaction {transactionId} has already been processed. Skipping reward.");
                    }
                    else
                    {
                        Debug.Log($"[IAPManager] Valid transaction {transactionId}. Granting reward for product {product.definition.id}.");
                        OnPurchaseSuccess?.Invoke(product);
                        RefreshEntitlementStatus(product.definition.id);
                        m_ReceiptValidator.MarkTransactionAsProcessed(transactionId);
                    }
                }
                else
                {
                    Debug.LogError($"[IAPManager] Invalid receipt for product {product.definition.id}. Not granting reward.");
                    OnPurchaseFailure?.Invoke(product, PurchaseFailureReason.SignatureInvalid);
                }
            }
            else
            {
                Debug.LogWarning("[IAPManager] Receipt validation is disabled. Granting reward without validation.");
                OnPurchaseSuccess?.Invoke(product);
                RefreshEntitlementStatus(product.definition.id);
            }

            m_StoreController.ConfirmPurchase(order);
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {
            Debug.LogError($"[IAPManager] Failed to fetch purchases: {description.Message}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            Debug.Log("[IAPManager] Existing purchases fetched successfully.");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            Debug.LogError($"[IAPManager] Purchase failed - Product: '{product?.definition.id}', Reason: {order.FailureReason}");
            OnPurchaseFailure?.Invoke(product, order.FailureReason);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            var product = GetFirstProductInOrder(order);
            if (order is ConfirmedOrder)
            {
                Debug.Log($"[IAPManager] Purchase confirmed for product: {product?.definition.id}");
            }
            else if (order is FailedOrder failedOrder)
            {
                Debug.LogError($"[IAPManager] Purchase confirmation failed for product: {product?.definition.id} with reason: {failedOrder.FailureReason}");
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAPManager] Products fetched successfully: {products.Count} products found.");

            foreach (var product in products)
            {
                Debug.Log($"[IAPManager] Fetched {product.definition.id}");
                RefreshEntitlementStatus(product.definition.id);
                // if (product.definition.type == ProductType.Subscription || product.definition.type == ProductType.NonConsumable)
                // {
                //     Debug.Log($"[IAPManager] Found restorable product, checking entitlement for: {product.definition.id}");
                //     m_StoreController.CheckEntitlement(product);
                // }
            }

            m_StoreController.FetchPurchases();
        }

        private void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            Debug.LogError($"[IAPManager] Failed to fetch products: {failure.FailureReason}");
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogWarning($"[IAPManager] Store disconnected: {description.message}");
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            bool isEntitled = entitlement.Status == EntitlementStatus.FullyEntitled;

            m_EntitlementStates[entitlement.Product.definition.id] = isEntitled;

            OnEntitlementStateChanged?.Invoke(entitlement.Product.definition.id, isEntitled);

            Debug.Log($"[IAPManager] Entitlement status for '{entitlement.Product.definition.id}' updated to: {isEntitled}");
        }

        // --- Helper Methods ---
        private Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        // --- Public Methods ---
        public void PurchaseProduct(string productId)
        {
            var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (product == null)
            {
                Debug.LogError($"[IAPManager] Product with ID '{productId}' not found.");
                OnPurchaseFailure?.Invoke(null, PurchaseFailureReason.ProductUnavailable);
                return;
            }

            m_StoreController.PurchaseProduct(product);
        }

        public void RestorePurchases()
        {
            Debug.Log("[IAPManager] Restoring purchases...");
            m_StoreController.RestoreTransactions((success, error) =>
            {
                if (success)
                {
                    Debug.Log("[IAPManager] Restore process completed successfully.");
                    OnRestoreCompleted?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[IAPManager] Restore process failed with error: {error}");
                }
            });
        }

        public bool IsEntitled(string productId)
        {
            return m_EntitlementStates.TryGetValue(productId, out bool isEntitled) && isEntitled;
        }

        public void RefreshEntitlementStatus(string productId)
        {
            var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (product != null && (product.definition.type == ProductType.Subscription || product.definition.type == ProductType.NonConsumable))
            {
                m_StoreController.CheckEntitlement(product);
            }
            else
            {
                Debug.LogWarning($"[IAPManager] Could not find a restorable product with ID '{productId}' to refresh status.");
            }
        }

        public ProductMetadata GetProductMetadata(string productId)
        {
            var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            return product?.metadata;
        }

    }
}
