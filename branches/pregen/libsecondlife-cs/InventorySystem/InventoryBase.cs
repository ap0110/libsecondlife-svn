using System;

using libsecondlife;
using libsecondlife.AssetSystem;

namespace libsecondlife.InventorySystem
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    abstract public class InventoryBase
    {
        protected InventoryManager IManager;

        internal string name;

        internal InventoryBase(InventoryManager manager)
        {
            if (manager == null)
            {
                throw new Exception("Inventory Manager cannot be null");
            }
            IManager = manager;
        }

        abstract public string ToXML(bool outputAssets);

        protected string xmlSafe(string str)
        {
            if (str != null)
            {
                string clean = str.Replace("&", "&amp;");
                clean = clean.Replace("<", "&lt;");
                clean = clean.Replace(">", "&gt;");
                clean = clean.Replace("'", "&apos;");
                clean = clean.Replace("\"", "&quot;");
                return clean;
            }
            else
            {
                return "";
            }
        }

    }
}
