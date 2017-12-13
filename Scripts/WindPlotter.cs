using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class WindPlotter : MonoBehaviour, PlotterInterface
{

	public float UpdateRate = 60.0f;
	public int LayersUpdateRateMin = 5;

	public GameObject PointPrefab;
	public GameObject PointHolder;
	public GameObject RealTimeHolder;
	public float plotScale = 50;
	public float cubeSize = 0.05f;
	public float cubeSizeMultipler = 10;

	private static float imageScaling = 540.0f / 870.0f;
	//private static int MAX_CHILD = 100;
	private static int MAX_LAYER = 10;
	private static float WIND_H = 0.4f;
	private static float WIND_S = 0.5f;
	private static float WIND_A = 0.8f;
	//private static float HIDDEN = 0.05f;
	//private static float HIDDEN_LOCATION = 50;
	private static float maxLat = 1.474726f;
	private static float minLat = 1.179198f;
	private static float maxLng = 104.086948f;
	private static float minLng = 103.615349f;
	private static string SPEED_URL = "https://api.data.gov.sg/v1/environment/wind-speed?date_time=";
	private static string DIR_URL = "https://api.data.gov.sg/v1/environment/wind-direction?date_time=";

	private List<Dictionary<string, object>> listData;
	private List<Dictionary<string, object>> listRealTime;
	private float maxSpeed;
	private float minSpeed;
	private DateTime lastPushUp = DateTime.Now;
	private string strLastUpdateDt = "";
	private WindPlot plot = new WindPlot();
	private vrCommand m_cmdLoadFirstData = null;
	private vrCommand m_cmdAutoPopulate = null;
	private vrCommand m_cmdUpdatePlot = null;

	// Use this for initialization
	void Start () {
		m_cmdLoadFirstData = new vrCommand("WindPlotCmdLoadFirstData", cmdLoadFirstData);
		m_cmdAutoPopulate = new vrCommand("WindPlotCmdAutoPopulate", cmdAutoPopulate);
		m_cmdUpdatePlot = new vrCommand("WindPlotCmdUpdate", cmdUpdatePlot);

		listData = new List<Dictionary<string, object>>();
		listRealTime = new List<Dictionary<string, object>>();
		if ( MiddleVR.VRClusterMgr.IsServer() || ( !MiddleVR.VRClusterMgr.IsCluster() ) )
		{
			vrValue iVal = vrValue.CreateList();
			iVal.AddListItem(WebApiCall.CallWeb(SPEED_URL));
			iVal.AddListItem(WebApiCall.CallWeb(DIR_URL));
			m_cmdLoadFirstData.Do(iVal);
		}
		autoPopulate();
		InvokeRepeating("updatePlot", UpdateRate, UpdateRate);
	}

	public bool TurnAlpha(GameObject go=null)
	{
		if ( go == null )
		{
			PointHolder.SetActive(!PointHolder.activeSelf);
			RealTimeHolder.SetActive(!RealTimeHolder.activeSelf);
		}
		else
			go.SetActive(!go.activeSelf);
		
			/*
		if ( PointHolder.transform.position.z == HIDDEN_LOCATION )
			PointHolder.transform.Translate(0, 0, -HIDDEN_LOCATION);
		else
			PointHolder.transform.Translate(0, 0, HIDDEN_LOCATION);

		if ( RealTimeHolder.transform.position.z == HIDDEN_LOCATION )
			RealTimeHolder.transform.Translate(0, 0, -HIDDEN_LOCATION);
		else
			RealTimeHolder.transform.Translate(0, 0, HIDDEN_LOCATION);
			*/

		//RealTimeHolder.SetActive(!RealTimeHolder.activeSelf);
		//PointHolder.SetActive(!PointHolder.activeSelf);
		/*
		for ( var i = 0; i < PointHolder.transform.childCount; i++ )
			PointHolder.transform.GetChild(i).GetComponent<Renderer>().enabled = !PointHolder.transform.GetChild(i).GetComponent<Renderer>().enabled;
		for ( var i = 0; i < RealTimeHolder.transform.childCount; i++ )
			RealTimeHolder.transform.GetChild(i).GetComponent<Renderer>().enabled = !RealTimeHolder.transform.GetChild(i).GetComponent<Renderer>().enabled;
		*/


		/*
		for ( var i = 0; i < RealTimeHolder.transform.childCount; i++ )
			RealTimeHolder.transform.GetChild(i).gameObject.SetActive(!RealTimeHolder.transform.GetChild(i).gameObject.activeSelf);
		*/
		/*
		for ( var i = 0; i < PointHolder.transform.childCount; i++ )
		{
			PointHolder.transform.GetChild(i).gameObject.SetActive(!PointHolder.transform.GetChild(i).gameObject.activeSelf);
			if ( PointHolder.transform.GetChild(i).GetComponent<Renderer>().enabled )
				PointHolder.transform.GetChild(i).GetComponent<Renderer>().enabled = false;
			else
			{
				PointHolder.transform.GetChild(i).GetComponent<Renderer>().enabled = true;
			}
			Color currentColor = PointHolder.transform.GetChild(i).GetComponent<Renderer>().material.color;
			if ( currentColor.a == WIND_A )
				currentColor.a = HIDDEN;
			else
				currentColor.a = WIND_A;
			PointHolder.transform.GetChild(i).GetComponent<Renderer>().material.color = currentColor;

		}
		*/
		return true;
	}

	public string GetDimension()
	{
		return "Wind";
	}

	private void updatePlot()
	{
		if ( MiddleVR.VRClusterMgr.IsServer() || ( !MiddleVR.VRClusterMgr.IsCluster() ) )
		{
			vrValue iVal = vrValue.CreateList();
			iVal.AddListItem(WebApiCall.CallWeb(SPEED_URL));
			iVal.AddListItem(WebApiCall.CallWeb(DIR_URL));
			m_cmdUpdatePlot.Do(iVal);
		}
	}

	private void clearRealTime()
	{
		foreach ( Transform child in RealTimeHolder.transform )
			Destroy(child.gameObject);
	}

	private void pushPointsUp()
	{
		for ( int i = RealTimeHolder.transform.childCount - 1; i >= 0; i-- )
		{
			Transform child = RealTimeHolder.transform.GetChild(i);
			child.parent = PointHolder.transform;
		}

		for ( int i = PointHolder.transform.childCount-1; i >=0; i-- )
		{
			PointHolder.transform.GetChild(i).transform.localPosition += new Vector3(0, cubeSize, 0);
			if ( PointHolder.transform.GetChild(i).transform.localPosition.y / cubeSize > MAX_LAYER )
			{
				Destroy(PointHolder.transform.GetChild(i).gameObject);
			}
		}
	}

	private void autoPopulate()
	{
		DateTime dt = DateTime.Now.Add(new TimeSpan(-1, 0, 0));
		lastPushUp = dt;

		if ( MiddleVR.VRClusterMgr.IsServer() || ( !MiddleVR.VRClusterMgr.IsCluster() ) )
		{
			int layerNum = 1;
			while ( layerNum < MAX_LAYER )
			{
				dt = dt.AddMinutes(-LayersUpdateRateMin);
				vrValue iVal = vrValue.CreateList();
				iVal.AddListItem(layerNum);
				iVal.AddListItem(WebApiCall.CallWeb(SPEED_URL, dt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)));
				iVal.AddListItem(WebApiCall.CallWeb(DIR_URL, dt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)));
				m_cmdAutoPopulate.Do(iVal);
				layerNum++;
			}
		}
	}

	private vrValue cmdLoadFirstData(vrValue iVal)
	{
		string strSpeedResponse = iVal[0].GetString();
		string strDirResponse = iVal[1].GetString();
		plot.Init(strSpeedResponse, strDirResponse);
		return null;
	}

	private vrValue cmdUpdatePlot(vrValue iVal)
	{
		List<Dictionary<string, object>> listLastData;
		string strSpeedResponse = iVal[0].GetString();
		string strDirResponse = iVal[1].GetString();

		if ( !strLastUpdateDt.Equals("") )
		{
			listLastData = plot.GetLastData(strSpeedResponse, strDirResponse, strLastUpdateDt);
		}
		else
		{
			Debug.LogError("Should never reach here... ");
			listLastData = plot.GetLastData(strSpeedResponse, strDirResponse, null);
		}

		Debug.Log("wind recevied " + listLastData.Count + " @ " + DateTime.Now);

		if ( listLastData.Count != 0 )
		{
			strLastUpdateDt = (string)listLastData.Last()[WindPlot.Headers.Timestamp];
			if ( DateTime.Now - lastPushUp > new TimeSpan(0, LayersUpdateRateMin, 0) )
			{
				lastPushUp = DateTime.Now;
				pushPointsUp();
				listData.AddRange(listRealTime);
				listRealTime.Clear();
			}

			// averaging the data before update layer
			foreach ( Dictionary<string, object> pt in listLastData )
			{
				if ( listRealTime.Exists(p => Convert.ToString(p[WindPlot.Headers.Id]).Equals((string)pt[WindPlot.Headers.Id])) )
				{
					Dictionary<string, object> realPt = listRealTime.Find(p => Convert.ToString(p[WindPlot.Headers.Id]).Equals((string)pt[WindPlot.Headers.Id]));
					realPt[WindPlot.Headers.Speed] = (float)( (float)realPt[WindPlot.Headers.Speed] + (float)pt[WindPlot.Headers.Speed] ) / 2.0f;
					realPt[WindPlot.Headers.Dir] = (float)( (float)realPt[WindPlot.Headers.Dir] + (float)pt[WindPlot.Headers.Dir] ) / 2.0f;
					realPt[WindPlot.Headers.Timestamp] = pt[WindPlot.Headers.Timestamp];
				}
				else
				{
					listRealTime.Add(pt);
				}
			}

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listRealTime).ToList();
			maxSpeed = Convert.ToSingle(listConcat.Max(item => item[WindPlot.Headers.Speed]));
			minSpeed = Convert.ToSingle(listConcat.Min(item => item[WindPlot.Headers.Speed]));

			clearRealTime();
			for ( int i = 0; i < listRealTime.Count; i++ )
			{
				plotPt(listRealTime[i], RealTimeHolder);
			}
		}

		return null;
	}

	private vrValue cmdAutoPopulate(vrValue iVal)
	{
		int layerNum = iVal[0].GetInt();
		string strSpeedResponse = iVal[1].GetString();
		string strDirResponse = iVal[2].GetString();

		List<Dictionary<string, object>> listTempData = plot.GetPastData(strSpeedResponse, strDirResponse);
		if ( listTempData.Count != 0 )
		{
			strLastUpdateDt = (string)listTempData.Last()[WindPlot.Headers.Timestamp];

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listTempData).ToList();
			maxSpeed = Convert.ToSingle(listConcat.Max(item => item[WindPlot.Headers.Speed]));
			minSpeed = Convert.ToSingle(listConcat.Min(item => item[WindPlot.Headers.Speed]));

			for ( int i = 0; i < listTempData.Count; i++ )
			{
				plotPt(listTempData[i], PointHolder, layerNum, true);
				listData.Insert(0, listTempData[i]);
			}
			listTempData.Clear();
		}
		return null;
	}

	private void plotPt(Dictionary<string, object> dataPt, GameObject goParent, int layerNum = 1, bool insertFirst = false)
	{
		float y, size;
		float z =
			( (float)dataPt[WindPlot.Headers.Lat] - minLat )
			/ ( maxLat - minLat );

		float x =
			( (float)dataPt[WindPlot.Headers.Lng] - minLng )
			/ ( maxLng - minLng );

		x -= 0.5f;
		z -= 0.5f;
		z *= imageScaling;

		GameObject dataPoint = Instantiate(
				PointPrefab,
				new Vector3(x * plotScale, cubeSize * layerNum, z * plotScale),
				Quaternion.identity);

		dataPoint.transform.parent = goParent.transform;
		dataPoint.transform.Translate(goParent.transform.position);
		if ( insertFirst )
			dataPoint.transform.SetAsFirstSibling();

		if ( System.Convert.ToSingle(dataPt[WindPlot.Headers.Speed]) == 0 )
		{
			y = 0;
			size = 0;
		}
		else
		{
			y = ( (float)dataPt[WindPlot.Headers.Speed] - minSpeed )
				/ ( maxSpeed - minSpeed );
			y += 0.1f; // make the min point some size. to differential from missing data. 
			size = y * cubeSize * cubeSizeMultipler;
		}

		dataPoint.transform.localScale = new Vector3(cubeSize, cubeSize, size);
		Vector3 centerBeforeTranslate = dataPoint.transform.position;
		dataPoint.transform.Translate(0, 0, size / 2);
		dataPoint.transform.RotateAround(centerBeforeTranslate, new Vector3(0, 1, 0), (float)dataPt[WindPlot.Headers.Dir]);

		string strName = (string)dataPt[WindPlot.Headers.Name];
		dataPoint.transform.name = strName;

		Color pointColor = Color.HSVToRGB(WIND_H, WIND_S, y);
		pointColor.a = WIND_A;
		dataPoint.GetComponent<Renderer>().material.color = pointColor;

		string strDetails = "Station: " + (string)dataPt[WindPlot.Headers.Name]
							+ "\nLat: " + dataPt[WindPlot.Headers.Lat].ToString()
							+ "\nLng: " + dataPt[WindPlot.Headers.Lng].ToString()
							+ "\nSpeed: " + dataPt[WindPlot.Headers.Speed].ToString()
							+ "\nDir: " + dataPt[WindPlot.Headers.Dir].ToString()
							+ "\nTime: " + (string)dataPt[WindPlot.Headers.Timestamp];
		dataPoint.GetComponent<DataDetails>().fullDetails = strDetails;
		dataPoint.GetComponent<DataDetails>().dimension = "Wind";
	}
}
