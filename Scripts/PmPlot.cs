using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using System.Globalization;
using System.Linq;


public class PmPlot 
{
	private static int MAX_LIST = 500;

	public struct stations
	{
		public string id;
		public string name;
		public float lat;
		public float lng;
	};

	public static class Headers
	{
		public const string
			Id = "id",
			Name = "name",
			Lat = "latitude",
			Lng = "longitude",
			PM = "PM",
			Timestamp = "timestamp";
	}

	private List<stations> listStations = new List<stations>();
	private List<Dictionary<string, object>> listDataPoints = new List<Dictionary<string, object>>();

	// Use this for initialization

	public void Init(string strJson)
	{
		if ( storeStations(strJson) )
			loadData(strJson);
		else
			Debug.LogError("Error getting first data for PM");
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

	private bool storeStations(string strJson)
	{
		if ( strJson == "" )
			return false;
		var data = JSON.Parse(strJson);
		if ( data == null )
			return false;

		for ( int i = 0; i < data["region_metadata"].Count; i++ )
		{
			stations station = new stations();
			station.id = data["region_metadata"][i]["name"].Value;
			station.name = data["region_metadata"][i]["name"].Value;
			station.lat = data["region_metadata"][i]["label_location"]["latitude"].AsFloat;
			station.lng = data["region_metadata"][i]["label_location"]["longitude"].AsFloat;

			if ( !listStations.Exists(item => item.id == Convert.ToString(station.id)) )
			{
				listStations.Add(station);
			}
		}

		Debug.Log("total PM stations - " + listStations.Count);
		return true;
	}

	private bool loadData(string strJson)
	{
		//Debug.Log("i got this json loadData @ " + DateTime.Now + " - " + strJson);
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

		foreach ( stations st in listStations )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Id] = st.id;
			entry[Headers.Name] = st.name;
			entry[Headers.Lat] = st.lat;
			entry[Headers.Lng] = st.lng;
			entry[Headers.PM] = data["items"][0]["readings"]["pm25_one_hourly"][st.name].AsFloat;
			entry[Headers.Timestamp] = data["items"][0]["timestamp"].Value;

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
		//Debug.Log("i got this json loadPastData @ " + DateTime.Now + " - " + strJson );
		if ( strJson == "" )
			return listPastData;

		var data = JSON.Parse(strJson);

		if ( data == null )
			return listPastData;

		foreach ( stations st in listStations )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Id] = st.id;
			entry[Headers.Name] = st.name;
			entry[Headers.Lat] = st.lat;
			entry[Headers.Lng] = st.lng;
			entry[Headers.PM] = data["items"][0]["readings"]["pm25_one_hourly"][st.name].AsFloat;
			entry[Headers.Timestamp] = data["items"][0]["timestamp"].Value;

			listPastData.Add(entry);
		}

		return listPastData;
	}
}