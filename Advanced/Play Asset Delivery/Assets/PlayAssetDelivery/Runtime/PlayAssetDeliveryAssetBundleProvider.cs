using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace AddressablesPlayAssetDelivery
{
    /// <summary>
    /// Ensures that the asset pack containing the AssetBundle is installed/downloaded before attemping to load the bundle.
    /// </summary>
    [DisplayName("Play Asset Delivery Provider")]
    public class PlayAssetDeliveryAssetBundleProvider : AssetBundleProvider
    {
		ProvideHandle m_ProviderInterface;
		private bool isDownloading;

        public async override void Provide(ProvideHandle providerInterface)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Debug.Log("LOAD FROM ASSET PACK");
            LoadFromAssetPack(providerInterface);
#else
            base.Provide(providerInterface);
#endif
        }

        async void LoadFromAssetPack(ProvideHandle providerInterface)
        {
			float targetTime = 1;
			float currentTime = 0;
			while (isDownloading)
			{
				await Task.Yield();
				currentTime += Time.deltaTime;
				if (currentTime > targetTime)
				{
					currentTime = 0;
					Debug.Log("Waiting for download to complete");
				}
			}
			m_ProviderInterface = providerInterface;

            string bundleName = Path.GetFileNameWithoutExtension(providerInterface.Location.InternalId);
            if (!PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack.ContainsKey(bundleName))
            {
                // Bundle is either assigned to the generated asset packs, or not assigned to any asset pack
                base.Provide(providerInterface);
            }
            else
            {
                // Bundle is assigned to a custom fast-follow or on-demand asset pack
                string assetPackName = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack[bundleName].AssetPackName;
                if (PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath.ContainsKey(assetPackName))
                {
                    // Asset pack is already downloaded
                    base.Provide(providerInterface);
                }
                else
                {
                    // Download the asset pack
                    DownloadRemoteAssetPack(assetPackName, providerInterface);
                }
            }
        }

        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);
        }

        void DownloadRemoteAssetPack(string assetPackName, ProvideHandle providerInterface)
        {
            // Note that most methods in the AndroidAssetPacks class are either direct wrappers of java APIs in Google's PlayCore plugin,
            // or depend on values that the PlayCore API returns. If the PlayCore plugin is missing, calling these methods will throw an InvalidOperationException exception.
            try
            {
				m_ProviderInterface = providerInterface;
                AndroidAssetPacks.DownloadAssetPackAsync(new string[] { assetPackName }, CheckDownloadStatus);
            }
            catch (InvalidOperationException ioe)
            {
                Debug.LogError($"Cannot retrieve state for asset pack '{assetPackName}'. PlayCore Plugin is not installed: {ioe.Message}");
				providerInterface.Complete(this, false, new Exception("exception"));
            }
        }

        void CheckDownloadStatus(AndroidAssetPackInfo info)
        {
            string message = "";
            if (info.status == AndroidAssetPackStatus.Failed)
                message = $"Failed to retrieve the state of asset pack '{info.name}'.";
            else if (info.status == AndroidAssetPackStatus.Unknown)
                message = $"Asset pack '{info.name}' is unavailable for this application. This can occur if the app was not installed through Google Play.";
            else if (info.status == AndroidAssetPackStatus.Canceled)
                message = $"Cancelled asset pack download request '{info.name}'.";
            else if (info.status == AndroidAssetPackStatus.WaitingForWifi)
                AndroidAssetPacks.RequestToUseMobileDataAsync(OnRequestToUseMobileDataComplete);
            else if (info.status == AndroidAssetPackStatus.Completed)
            {
                string assetPackPath = AndroidAssetPacks.GetAssetPackPath(info.name);

				Debug.Log("Download completed for pack " + info.name + "; Path = " + assetPackPath);
                if (!string.IsNullOrEmpty(assetPackPath))
                {
                    // Asset pack was located on device. Proceed with loading the bundle.
                    PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath.Add(info.name, assetPackPath);
                    base.Provide(m_ProviderInterface);
                }
                else
                    message = $"Downloaded asset pack '{info.name}' but cannot locate it on device.";
            }

            if (!string.IsNullOrEmpty(message))
            {
                Debug.LogError(message);
                m_ProviderInterface.Complete(this, false, new Exception("exception"));
            }
        }

        void OnRequestToUseMobileDataComplete(AndroidAssetPackUseMobileDataRequestResult result)
        {
            if (!result.allowed)
            {
                Debug.LogError("Request to use mobile data was denied.");
                m_ProviderInterface.Complete(this, false, new Exception("exception"));
            }
        }
    }
}
