﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class RainPlotter : MonoBehaviour, PlotterInterface
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
	private static float RAIN_H = 0.56f;
	private static float RAIN_S = 0.5f;
	private static float RAIN_A = 0.8f;
	//private static float HIDDEN = 0.05f;
	//private static float HIDDEN_LOCATION = 50;
	private static float maxLat = 1.474726f;
	private static float minLat = 1.179198f;
	private static float maxLng = 104.086948f;
	private static float minLng = 103.615349f;
	private static string URL = "https://api.data.gov.sg/v1/environment/rainfall?date_time=";

	private List<Dictionary<string, object>> listData;
	private List<Dictionary<string, object>> listRealTime;
	private float maxValue;
	private float minValue;
	private DateTime lastPushUp = DateTime.Now;
	private string strLastUpdateDt = "";
	private RainPlot plot = new RainPlot();
	private vrCommand m_cmdLoadFirstData = null;
	private vrCommand m_cmdAutoPopulate = null;
	private vrCommand m_cmdUpdatePlot = null;



	// Use this for initialization
	void Start()
	{
		m_cmdLoadFirstData = new vrCommand("RainPlotCmdLoadFirstData", cmdLoadFirstData);
		m_cmdAutoPopulate = new vrCommand("RainPlotCmdAutoPopulate", cmdAutoPopulate);
		m_cmdUpdatePlot = new vrCommand("RainPlotCmdUpdate", cmdUpdatePlot);

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
		return "Rain";
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
			//if ( ( PointHolder.transform.childCount - i ) > MAX_CHILD )
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

		Debug.Log("rain recevied " + listLastData.Count + " @ " + DateTime.Now);

		if ( listLastData.Count != 0 )
		{
			strLastUpdateDt = (string)listLastData.Last()[RainPlot.Headers.Timestamp];
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
				if ( listRealTime.Exists(p => Convert.ToString(p[RainPlot.Headers.Id]).Equals((string)pt[RainPlot.Headers.Id])) )
				{
					Dictionary<string, object> realPt = listRealTime.Find(p => Convert.ToString(p[RainPlot.Headers.Id]).Equals((string)pt[RainPlot.Headers.Id]));
					realPt[RainPlot.Headers.Rain] = (float)( (float)realPt[RainPlot.Headers.Rain] + (float)pt[RainPlot.Headers.Rain] ) / 2.0f;
					realPt[RainPlot.Headers.Timestamp] = pt[RainPlot.Headers.Timestamp];
				}
				else
				{
					listRealTime.Add(pt);
				}
			}

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listRealTime).ToList();
			maxValue = (float)( listConcat.Max(item => item[RainPlot.Headers.Rain]) );
			minValue = (float)( listConcat.Min(item => item[RainPlot.Headers.Rain]) );

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
			strLastUpdateDt = (string)listTempData.Last()[RainPlot.Headers.Timestamp];

			List<Dictionary<string, object>> listConcat;
			listConcat = listData.Concat(listTempData).ToList();
			maxValue = Convert.ToSingle(listConcat.Max(item => item[RainPlot.Headers.Rain]));
			minValue = Convert.ToSingle(listConcat.Min(item => item[RainPlot.Headers.Rain]));

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
		( (float)dataPt[RainPlot.Headers.Lat] - minLat )
		/ ( maxLat - minLat );

		float x =
			( (float)dataPt[RainPlot.Headers.Lng] - minLng )
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

		if ( System.Convert.ToSingle(dataPt[RainPlot.Headers.Rain]) == 0 )
			y = 0;
		else
		{
			y =
			( (float)dataPt[RainPlot.Headers.Rain] - minValue )
			/ ( maxValue - minValue );
			y += 0.1f; // make the min point some size. to differential from missing data. 
		}

		float size = y * cubeSize * cubeSizeMultipler;
		dataPoint.transform.localScale = new Vector3(size, cubeSize / 2.0f, size);
		//Vector3 centerBeforeTranslate = dataPoint.transform.position;
		dataPoint.transform.Translate(0, 0, -size / 2);

		string strName = (string)dataPt[RainPlot.Headers.Name];
		dataPoint.transform.name = strName;

		Color pointColor = Color.HSVToRGB(RAIN_H, RAIN_S, y);
		pointColor.a = RAIN_A;
		dataPoint.GetComponent<Renderer>().material.color = pointColor;

		string strDetails = "Station: " + (string)dataPt[RainPlot.Headers.Name]
							+ "\nLat: " + dataPt[RainPlot.Headers.Lat].ToString()
							+ "\nLng: " + dataPt[RainPlot.Headers.Lng].ToString()
							+ "\nRain: " + dataPt[RainPlot.Headers.Rain].ToString()
							+ "\nTime: " + (string)dataPt[RainPlot.Headers.Timestamp];
		dataPoint.GetComponent<DataDetails>().fullDetails = strDetails;
		dataPoint.GetComponent<DataDetails>().dimension = "Rain";

	}

}
