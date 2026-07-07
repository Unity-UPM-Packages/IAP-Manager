using UnityEngine;

namespace TheLegends.Base.IAP
{
    [CreateAssetMenu(fileName = "IAPSettings", menuName = "DataAsset/IAPSettings")]
    public class IAPSettings : ScriptableObject
    {
        public const string ResDir = "Assets/TripSoft/IAP/Resources";
        public const string FileName = "IAPSettings";
        public const string FileExtension = ".asset";

        private static IAPSettings _instance;
        public static IAPSettings Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = Resources.Load<IAPSettings>(FileName);
                return _instance;
            }
        }

        public bool isAutoInit = true;
        public SkuID skuIDs;
        public bool isUseValidator = true;
    }
}
