using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using System.Globalization;
using System.Linq;

public class TaxiPlot
{
	private static int MAX_LIST = 5000;

	public static class Headers
	{
		public const string
			Lat = "latitude",
			Lng = "longitude",
			Timestamp = "timestamp";
	}

	private List<Dictionary<string, object>> listDataPoints = new List<Dictionary<string, object>>();

	// Use this for initialization

	public void Init(string strJson)
	{
		if(!loadData(strJson))
			Debug.LogError("Error getting first data for Taxi");
	}

	public List<Dictionary<string, object>> GetLastData(string strJson, object objLastDT)
	{
		List<Dictionary<string, object>> listLastData = new List<Dictionary<string, object>>();
		string strDtLast;

		loadData(strJson);

		if ( listDataPoints.Count != 0 )
		{
			Dictionary<string, object> lastPt = listDataPoints.Last();
			strDtLast = (string)lastPt[Headers.Timestamp];
		}
		else
		{
			strDtLast = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
		}

		if ( objLastDT != null )
		{
			string strReqTime = (string)objLastDT;
			if ( strReqTime.Equals(strDtLast) )
				return listLastData;
			//Debug.Log(strReqTime + "<-->" + strDtLast);
		}


		for ( int i = listDataPoints.Count - 1; i >= 0; i-- )
		{
			Dictionary<string, object> point = listDataPoints[i];
			string strDt = (string)point[Headers.Timestamp];

			if ( strDtLast == strDt )
				listLastData.Add(point);
			else
				break;
		}
		return listLastData;
	}

	public List<Dictionary<string, object>> GetPastData(string strjson)
	{
		return loadPastData(strjson);
	}

	private bool loadData(string strJson)
	{
		if ( strJson == "" )
			return false;
		var data = JSON.Parse(strJson);
		if ( data == null )
			return false;

		if ( listDataPoints.Count != 0 )
		{
			Dictionary<string, object> lastPt = listDataPoints.Last();
			string strDtLast = (string)lastPt[Headers.Timestamp];
			if ( strDtLast.Equals(data["items"][0]["timestamp"].Value) )
				return false;
		}

		for ( int i = 0; i < data["features"][0]["geometry"]["coordinates"].Count; i++ )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Lng] = data["features"][0]["geometry"]["coordinates"][i][0].AsFloat;
			entry[Headers.Lat] = data["features"][0]["geometry"]["coordinates"][i][1].AsFloat;
			entry[Headers.Timestamp] = data["features"][0]["properties"]["timestamp"].Value;

			//limit growing of the list
			if ( listDataPoints.Count >= MAX_LIST )
				listDataPoints.RemoveAt(0);

			listDataPoints.Add(entry);
		}

		return true;
	}

	private List<Dictionary<string, object>> loadPastData(string strJson)
	{
		List<Dictionary<string, object>> listPastData = new List<Dictionary<string, object>>();
		if ( strJson == "" )
			return listPastData;

		var data = JSON.Parse(strJson);

		if ( data == null )
			return listPastData;

		for ( int i = 0; i < data["features"][0]["geometry"]["coordinates"].Count; i++ )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Lng] = data["features"][0]["geometry"]["coordinates"][i][0].AsFloat;
			entry[Headers.Lat] = data["features"][0]["geometry"]["coordinates"][i][1].AsFloat;
			entry[Headers.Timestamp] = data["features"][0]["properties"]["timestamp"].Value;

			listPastData.Add(entry);
		}

		return listPastData;
	}
}