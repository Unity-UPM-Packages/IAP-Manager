using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing.Security;

namespace TheLegends.Base.IAP
{
    /// <summary>
    /// Handles local receipt validation for both Google Play and Apple App Store.
    /// Also manages processed transactions to prevent duplicate rewards.
    /// </summary>
    public class LocalReceiptValidator
    {
        private CrossPlatformValidator m_Validator;
        private const string k_ProcessedTransactionPrefix = "processed_transaction_";

        /// <summary>
        /// Initializes the validator. This should be called only once.
        /// It requires GooglePlayTangle and AppleTangle classes to be generated.
        /// </summary>
        public LocalReceiptValidator()
        {
#if !UNITY_EDITOR
            // We require both tangle data to initialize the validator.
            // Even if you only build for one platform, both classes must exist.
            // You can get a fake GooglePlayTangle.cs from Unity's official IAP samples
            // if you are only building for Apple and vice-versa.
            m_Validator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#endif
        }

        /// <summary>
        /// Validates a receipt and extracts the transaction ID.
        /// </summary>
        /// <param name="receipt">The receipt string from the purchase event.</param>
        /// <param name="transactionId">The unique transaction ID if validation is successful.</param>
        /// <returns>True if the receipt is valid, false otherwise.</returns>
        public bool Validate(string receipt, out string transactionId)
        {
            transactionId = null;

            if (m_Validator == null)
            {
                Debug.LogError("[IAPManager] LocalReceiptValidator is not initialized. Validation will be skipped. This is normal in the Editor.");
                // In the editor, we don't have a validator. We can't validate, but we can't fail either.
                // For editor testing, we can simulate a transaction ID.
#if UNITY_EDITOR
                transactionId = "editor_fake_transaction_" + System.Guid.NewGuid();
                return true;
#else
                return false;
#endif
            }

            try
            {
                // This is the core validation call.
                // It will throw an IAPSecurityException if the receipt is invalid.
                var result = m_Validator.Validate(receipt);

                // If validation is successful, get the first receipt and its transaction ID.
                if (result != null && result.Length > 0)
                {
                    transactionId = result.First().transactionID;
                    Debug.Log($"[IAPManager] Receipt validation successful. Transaction ID: {transactionId}");
                    return true;
                }

                Debug.LogWarning("[IAPManager] Receipt validation returned no results.");
                return false;
            }
            catch (IAPSecurityException ex)
            {
                Debug.LogError($"[IAPManager] Invalid receipt: {ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IAPManager] An unexpected error occurred during receipt validation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a transaction has already been processed and rewarded.
        /// </summary>
        /// <param name="transactionId">The transaction ID to check.</param>
        /// <returns>True if the transaction was already processed.</returns>
        public bool IsTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return true; // Invalid transaction ID, treat as processed to be safe.
            }
            return PlayerPrefs.HasKey(k_ProcessedTransactionPrefix + transactionId);
        }

        /// <summary>
        /// Marks a transaction as processed to prevent duplicate rewards.
        /// </summary>
        /// <param name="transactionId">The transaction ID to mark.</param>
        public void MarkTransactionAsProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return;
            }
            
            // We only need to store the key. The value doesn't matter.
            PlayerPrefs.SetInt(k_ProcessedTransactionPrefix + transactionId, 1);
            PlayerPrefs.Save();
            Debug.Log($"[IAPManager] Transaction {transactionId} marked as processed.");
        }
    }
}