using System;
using System.Net;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

public class WebApiCall
{
	public static string CallWeb(string url, string dt="")
	{
		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

		if (dt.Equals(""))
			dt = DateTime.Now.Add(new TimeSpan(-1, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss",
									CultureInfo.InvariantCulture);
		string strUrl = url + dt;
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strUrl);

		request.Method = "GET";
		request.Headers.Add("api-key", "hvsj9w3Rl0x875yzXBGhahGc9fxyqa4o");

		HttpWebResponse response = (HttpWebResponse)request.GetResponse();
		if ( response.StatusCode == HttpStatusCode.OK )
		{
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			reader.Close();
			response.Close();
			//Debug.Log("respond from web - " + responseFromServer);
			return responseFromServer;
		}
		return "";
	}

	// to sovle unity webcall using other port number issue
	public static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool isOk = true;
		// If there are errors in the certificate chain, look at each error to determine the cause.
		if ( sslPolicyErrors != SslPolicyErrors.None )
		{
			for ( int i = 0; i < chain.ChainStatus.Length; i++ )
			{
				if ( chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown )
				{
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build((X509Certificate2)certificate);
					if ( !chainIsValid )
					{
						isOk = false;
					}
				}
			}
		}
		return isOk;
	}

}