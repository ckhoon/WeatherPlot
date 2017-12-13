using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class HumidityPlotter : MonoBehaviour, PlotterInterface
{
	public float UpdateRate = 30.0f;
	public int LayersUpdateRateMin = 2;

	public GameObject PointPrefab;
	public GameObject PointHolder;
	public GameObject RealTimeHolder;
	public float plotScale = 50;
	public float cubeSize = 0.05f;
	public float cubeSizeMultipler = 10;

	private static float imageScaling = 540.0f / 870.0f;
	//private static int MAX_CHILD = 100;
	private static int MAX_LAYER = 10;
	private static float HUMIDITY_H = 0.78f;
	private static float HUMIDITY_S = 0.5f;
	private static float HUMIDITY_A = 0.8f;
	//private static float HIDDEN = 0.05f;
	//private static float HIDDEN_LOCATION = 50;
	private static float maxLat = 1.474726f;
	private static float minLat = 1.179198f;
	private static float maxLng = 104.086948f;
	private static float minLng = 103.615349f;
	private static string URL = "https://api.data.gov.sg/v1/environment/relative-humidity?date_time=";

	private List<Dictionary<string, object>> listData;
	private List<Dictionary<string, object>> listRealTime;
	private float maxValue;
	private float minValue;
	private DateTime lastPushUp = DateTime.Now;
	private string strLastUpdateDt = "";
	private HumidityPlot plot = new HumidityPlot();
	private vrCommand m_cmdLoadFirstData = null;
	private vrCommand m_cmdAutoPopulate = null;
	private vrCommand m_cmdUpdatePlot = null;



	// Use this for initialization
	void Start()
	{
		m_cmdLoadFirstData = new vrCommand("HumidityPlotCmdLoadFirstData", cmdLoadFirstData);
		m_cmdAutoPopulate = new vrCommand("HumidityPlotCmdAutoPopulate", cmdAutoPopulate);
		m_cmdUpdatePlot = new vrCommand("HumidityPlotCmdUpdate", cmdUpdatePlot);

		listData = new List<Dictionary<string, object>>();
		listRealTime = new List<Dictionary<string, object>>();
		if ( MiddleVR.VRClusterMgr.IsServer() || ( !MiddleVR.VRClusterMgr.IsCluster() ) )
		{
			vrValue iVal;
			iVal = new vrValue(WebApiCall.CallWeb(URL));
			m_cmdLoadFirstData.Do(iVal);
		}
		autoPopulate();
		InvokeRepeating("updatePlot", UpdateRate, UpdateRate);
	}

	public bool TurnAlpha(GameObject go = null)
	{
		if ( go == null )
		{
			PointHolder.SetActive(!PointHolder.activeSelf);
			RealTimeHolder.SetActive(!RealTimeHolder.activeSelf);
		}
		else
			go.SetActive(!go.activeSelf);

		return true;
	}

	public string GetDimension()
	{
		return "Humidity";
	}


	private void updatePlot()
	{
		if ( MiddleVR.VRClusterMgr.IsServer() || ( !MiddleVR.VRClusterMgr.IsCluster() ) )
		{
			vrValue iVal = vrValue.CreateList();
			iVal.AddListItem(WebApiCall.CallWeb(URL));
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
		for ( int i = PointHolder.transform.childCount - 1; i >= 0; i-- )
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
				iVal.AddListItem(WebApiCall.CallWeb(URL, dt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)));
				m_cmdAutoPopulate.Do(iVal);
				layerNum++;
			}
		}
	}

	private vrValue cmdLoadFirstData(vrValue iVal)
	{
		string strResponse = iVal.GetString();
		plot.Init(strResponse);
		return null;
	}

	private vrValue cmdUpdatePlot(vrValue iVal)
	{
		List<Dictionary<string, object>> listLastData;
		string strResponse = iVal[0].GetString();

		if ( !strLastUpdateDt.Equals("") )
		{
			listLastData = plot.GetLastData(strResponse, strLastUpdateDt);
		}
		else
		{
			Debug.LogError("Should never reach here... ");
			listLastData = plot.GetLastData(strResponse, null);
		}

		Debug.Log("humidity recevied " + listLastData.Count + " @ " + DateTime.Now);

		if ( listLastData.Count != 0 )
		{
			strLastUpdateDt = (string)listLastData.Last()[HumidityPlot.Headers.Timestamp];
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
				if ( listRealTime.Exists(p => Convert.ToString(p[HumidityPlot.Headers.Id]).Equals((string)pt[HumidityPlot.Headers.Id])) )
				{
					Dictionary<string, object> realPt = listRealTime.Find(p => Convert.ToString(p[HumidityPlot.Headers.Id]).Equals((string)pt[HumidityPlot.Headers.Id]));
					realPt[HumidityPlot.Headers.Humidity] = (float)( (float)realPt[HumidityPlot.Headers.Humidity] + (float)pt[HumidityPlot.Headers.Humidity] ) / 2.0f;
					realPt[HumidityPlot.Headers.Timestamp] = pt[HumidityPlot.Headers.Timestamp];
				}
				else
				{
					listRealTime.Add(pt);
				}
			}

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listRealTime).ToList();
			maxValue = (float)( listConcat.Max(item => item[HumidityPlot.Headers.Humidity]) );
			minValue = (float)( listConcat.Min(item => item[HumidityPlot.Headers.Humidity]) );

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
		string strResponse = iVal[1].GetString();
		List<Dictionary<string, object>> listTempData = plot.GetPastData(strResponse);
		if ( listTempData.Count != 0 )
		{
			strLastUpdateDt = (string)listTempData.Last()[HumidityPlot.Headers.Timestamp];

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listTempData).ToList();
			maxValue = Convert.ToSingle(listConcat.Max(item => item[HumidityPlot.Headers.Humidity]));
			minValue = Convert.ToSingle(listConcat.Min(item => item[HumidityPlot.Headers.Humidity]));

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
		float y;
		float z =
		( (float)dataPt[HumidityPlot.Headers.Lat] - minLat )
		/ ( maxLat - minLat );

		float x =
			( (float)dataPt[HumidityPlot.Headers.Lng] - minLng )
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

		if ( System.Convert.ToSingle(dataPt[HumidityPlot.Headers.Humidity]) == 0 )
			y = 0;
		else
		{
			y =
			( (float)dataPt[HumidityPlot.Headers.Humidity] - minValue )
			/ ( maxValue - minValue );
			y += 0.1f; // make the min point some size. to differential from missing data. 
		}
		float size = y * cubeSize * cubeSizeMultipler;
		dataPoint.transform.localScale = new Vector3(size, cubeSize / 2.0f, size);
		//Vector3 centerBeforeTranslate = dataPoint.transform.position;
		dataPoint.transform.Translate(size / 2, 0, 0);

		string strName = (string)dataPt[HumidityPlot.Headers.Name];
		dataPoint.transform.name = strName;

		Color pointColor = Color.HSVToRGB(HUMIDITY_H, HUMIDITY_S, y);
		pointColor.a = HUMIDITY_A;
		dataPoint.GetComponent<Renderer>().material.color = pointColor;

		string strDetails = "Station: " + (string)dataPt[HumidityPlot.Headers.Name]
							+ "\nLat: " + dataPt[HumidityPlot.Headers.Lat].ToString()
							+ "\nLng: " + dataPt[HumidityPlot.Headers.Lng].ToString()
							+ "\nHumidity: " + dataPt[HumidityPlot.Headers.Humidity].ToString()
							+ "\nTime: " + (string)dataPt[HumidityPlot.Headers.Timestamp];
		dataPoint.GetComponent<DataDetails>().fullDetails = strDetails;
		dataPoint.GetComponent<DataDetails>().dimension = "Humidity";

	}

}
