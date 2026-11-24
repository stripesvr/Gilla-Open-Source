// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public static class GillaPurchases
{
    public enum Result
    {
        Bought = 0,
        Cancelled = 1,
        Failed = 2
    }

    // Redux: coin prices for the add-ons that the original sold for real money through Meta's
    // store. The server (CloudScript BuyAddon) has the same table and is the one that decides.
    public static int CoinPrice(string sku)
    {
        switch (sku)
        {
            case "gilla_size_menu": return 2500;
            case "gilla_punch_menu": return 2500;
            case "gilla_op_menu": return 5000;
            case "gilla_bundle_og": return 5000;
            case "gilla_bundle_vip": return 10000;
        }
        return -1;
    }

    public const string SizeSku = "gilla_size_menu";
    public const string OpSku = "gilla_op_menu";
    public const string PunchSku = "gilla_punch_menu";
    private static string busySku;
    private static float busySince;
    private static float backAt = -1f;
    private const float AnswerWithin = 8;
    private const float LongestCheckout = 180;
    public static event Action<string> CheckoutOpened;
    private const float RefetchEvery = 60;
    private static readonly Dictionary<string, string> prices = new Dictionary<string, string>();
    private static int asking;
    private static bool oneByOne;
    private static float nextFetch;

    public static bool Busy
    {
        get
        {
            Unstick();
            return busySku != null;
        }
    }

    public static bool XpBoost
    {
        get
        {
            for (int i = 0; i < CosmeticCatalog.BundleCount; i++)
            {
                var bundle = CosmeticCatalog.BundleAt(i);
                if (bundle.xpBoost && Coins.OwnsName(bundle.sku))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static string[] Skus()
    {
        List<string> list = new List<string>();
        list.Add(SizeSku);
        list.Add(OpSku);
        list.Add(PunchSku);
        for (int i = 0; i < CosmeticCatalog.BundleCount; i++)
        {
            if (!string.IsNullOrEmpty(CosmeticCatalog.BundleAt(i).sku))
            {
                list.Add(CosmeticCatalog.BundleAt(i).sku);
            }
        }
        return list.ToArray();
    }

    private static void Unstick()
    {
        if (busySku != null)
        {
            float now = Time.unscaledTime;
            float since;
            float limit;
            if (backAt >= 0f)
            {
                since = backAt;
                limit = AnswerWithin;
            }
            else
            {
                since = busySince;
                limit = LongestCheckout;
            }
            if (limit < now - since)
            {
                string sku = busySku;
                busySku = null;
                if (Core.IsInitialized())
                {
                    Debug.LogWarning("[GillaPurchases] no answer from Meta's checkout for " + sku + ": asking the server to check what's been bought");
                    GillaCloud.RedeemPurchases(sku);
                }
            }
        }
    }

    private static void OnFocus(bool focused)
    {
        if (busySku != null)
        {
            backAt = focused ? Time.unscaledTime : -1f;
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        busySku = null;
        backAt = -1f;
        oneByOne = false;
        UnityEngine.Application.focusChanged -= OnFocus;
        UnityEngine.Application.focusChanged += OnFocus;
        asking = 0;
        nextFetch = 0f;
        prices.Clear();
    }

    public static string PriceOf(string sku)
    {
        string result = null;
        if (!string.IsNullOrEmpty(sku) && !Core.IsInitialized())
        {
            // Redux: no Meta store, so the price is in coins.
            int coins = CoinPrice(sku);
            return coins > 0 ? coins.ToString("N0") + " COINS" : null;
        }
        if (!string.IsNullOrEmpty(sku))
        {
            if (UnityEngine.Application.isEditor)
            {
                var bundle = CosmeticCatalog.BundleWithSku(sku);
                result = "$0.00";
                if (bundle != null)
                {
                    result = null;
                    if (!string.IsNullOrEmpty(bundle.usd))
                    {
                        result = bundle.usd;
                    }
                }
            }
            else
            {
                string price;
                if (prices.TryGetValue(sku, out price))
                {
                    result = price;
                }
                else
                {
                    result = null;
                }
            }
        }
        return result;
    }

    public static void FetchPrices()
    {
        if (asking < 1 && !UnityEngine.Application.isEditor && Core.IsInitialized() && nextFetch <= Time.unscaledTime)
        {
            nextFetch = Time.unscaledTime + RefetchEvery;
            string[] skus = Skus();
            if (!oneByOne)
            {
                AskPrices(skus);
                return;
            }
            for (int i = 0; i < skus.Length; i++)
            {
                string key = skus[i];
                if (!prices.ContainsKey(key))
                {
                    AskPrices(new string[] { key });
                }
            }
        }
    }

    private static void AskPrices(string[] skus)
    {
        asking++;
        IAP.GetProductsBySKU(skus).OnComplete(message =>
        {
            asking--;
            if (message.IsError)
            {
                if (skus.Length < 2)
                {
                    return;
                }
                oneByOne = true;
                Debug.unityLogger.Log(LogType.Warning, "[GillaPurchases] couldn't read the prices all at once, asking for each: " + message.GetError().Message);
                return;
            }
            foreach (Product product in message.Data)
            {
                prices[product.Sku] = product.FormattedPrice;
            }
        });
    }

    public static void Buy(string sku, Action<Result> done)
    {
        if (!Busy)
        {
            if (Core.IsInitialized())
            {
                busySku = sku;
                busySince = Time.unscaledTime;
                backAt = -1f;
                if (CheckoutOpened != null)
                {
                    CheckoutOpened(sku);
                }
                IAP.LaunchCheckoutFlow(sku).OnComplete(message =>
                {
                    if (busySku == sku)
                    {
                        busySku = null;
                    }
                    if (!message.IsError)
                    {
                        Debug.Log("[GillaPurchases] bought " + sku);
                        GillaCloud.RedeemPurchases(sku);
                        if (done != null)
                        {
                            done(Result.Bought);
                        }
                        return;
                    }
                    string err = message.GetError().Message ?? "";
                    bool cancelled = err.Contains("user_canceled");
                    Debug.Log("[GillaPurchases] " + sku + " wasn't bought: " + err);
                    if (!cancelled)
                    {
                        GillaCloud.RedeemPurchases(sku);
                    }
                    if (done != null)
                    {
                        done(cancelled ? Result.Cancelled : Result.Failed);
                    }
                });
                return;
            }
            // Redux: no Meta store, so buy it with coins on the server.
            int coins = CoinPrice(sku);
            if (coins > 0 && Coins.Balance >= coins && GillaCloud.SignedIn)
            {
                busySku = sku;
                busySince = Time.unscaledTime;
                backAt = Time.unscaledTime;
                GillaCloud.BuyAddon(sku, ok =>
                {
                    if (busySku == sku)
                    {
                        busySku = null;
                    }
                    Debug.Log("[GillaPurchases] " + sku + (ok ? " bought with coins" : " wasn't bought"));
                    if (done != null)
                    {
                        done(ok ? Result.Bought : Result.Failed);
                    }
                });
                return;
            }
            if (done != null)
            {
                done(Result.Failed);
            }
        }
    }

    public static void RedeemOwned()
    {
        if (!UnityEngine.Application.isEditor && Core.IsInitialized())
        {
            IAP.GetViewerPurchases().OnComplete(message =>
            {
                bool unsure;
                if (message.IsError || message.Data == null || message.Data.HasNextPage)
                {
                    string detail = " in one page";
                    if (message.IsError)
                    {
                        detail = ": " + message.GetError().Message;
                    }
                    Debug.LogWarning("[GillaPurchases] couldn't read what's been bought" + detail + ": the server's asked about all of it");
                    unsure = true;
                }
                else
                {
                    unsure = false;
                }
                HashSet<string> owned = new HashSet<string>();
                if (!unsure)
                {
                    foreach (Purchase purchase in message.Data)
                    {
                        owned.Add(purchase.Sku);
                    }
                }
                string[] skus = Skus();
                for (int i = 0; i < skus.Length; i++)
                {
                    string sku = skus[i];
                    if (unsure || owned.Contains(sku))
                    {
                        if (!Have(sku))
                        {
                            GillaCloud.RedeemPurchases(sku);
                        }
                    }
                }
            });
        }
    }

    private static bool Have(string sku)
    {
        var bundle = CosmeticCatalog.BundleWithSku(sku);
        if (bundle == null)
        {
            return ModMenuAccess.Owns(ModMenuAccess.ItemOf(sku));
        }
        if (!Coins.OwnsName(sku))
        {
            if (!Coins.OwnsAll(bundle))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(bundle.tag))
            {
                return Coins.OwnsName(bundle.tag);
            }
        }
        return true;
    }
}
