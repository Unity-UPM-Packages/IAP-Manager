using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace TheLegends.Base.IAP
{
    [Serializable]
    public class SkuID
    {
        public List<SkuIDData> skuIDDatas = new List<SkuIDData>();
    }

    [Serializable]
    public class SkuIDData
    {
        public string skuID;
        public ProductType productType;
    }
}
