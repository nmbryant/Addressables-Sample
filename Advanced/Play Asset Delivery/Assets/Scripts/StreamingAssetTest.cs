using AddressablesPlayAssetDelivery;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Android;
using UnityEngine.UI;

public class StreamingAssetTest : MonoBehaviour
{
	public AssetReference reference;
	public RawImage image;
	public RawImage image2;

	private void Start()
	{
		TestPAD();
	}

	IEnumerator LoadTexture()
	{
		var url = Path.Combine(Application.streamingAssetsPath, "Balcony_Day.png");
		var localFile = new WWW(url);
		yield return localFile;

		var texture = localFile.texture;
		image.texture = texture;
	}

	void LoadTextureKey()
	{
		Debug.Log("FastFollowTest - LoadTextureKey Called");
		var addressableKey = "Assets/Backgrounds/Train_Day.png";
		var texture = Addressables.LoadAssetAsync<Texture2D>(addressableKey).WaitForCompletion();
		image2.texture = texture;
	}

	void TestPAD()
	{
		Debug.Log("TestPAD called");
		var bundleNameToAssetPack = PlayAssetDeliveryRuntimeData.Instance.BundleNameToAssetPack;
		foreach (var bundle in bundleNameToAssetPack)
		{
			Debug.Log("BUNDLE NAME = " + bundle.Key + "; ASSET PACK = " + bundle.Value);
		}

		var assetPackNameToDownloadPath = PlayAssetDeliveryRuntimeData.Instance.AssetPackNameToDownloadPath;
		foreach (var bundle in bundleNameToAssetPack)
		{
			Debug.Log("ASSET PACK NAME = " + bundle.Key + "; DOWNLOAD PATH = " + bundle.Value);
			var assetPackPath = AndroidAssetPacks.GetAssetPackPath(bundle.Key);
			Debug.Log("ASSET PACK PATH = " + assetPackPath);
		}

		var texture = Addressables.LoadAssetAsync<Texture2D>("Assets/Backgrounds/Balcony_Day.png").WaitForCompletion();
		image.texture = texture;
	}
}
