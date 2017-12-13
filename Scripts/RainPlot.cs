using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using System.Globalization;
using System.Linq;

public class RainPlot
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
			Rain = "rain",
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
			Debug.LogError("Error getting first data for Rain");
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

		for ( int i = 0; i < data["metadata"]["stations"].Count; i++ )
		{
			stations station = new stations();
			station.id = data["metadata"]["stations"][i]["id"].Value;
			station.name = data["metadata"]["stations"][i]["name"].Value;
			station.lat = data["metadata"]["stations"][i]["location"]["latitude"].AsFloat;
			station.lng = data["metadata"]["stations"][i]["location"]["longitude"].AsFloat;

			if ( !listStations.Exists(item => item.id == Convert.ToString(station.id)) )
			{
				listStations.Add(station);
			}
		}

		Debug.Log("total rain stations - " + listStations.Count);
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

		for ( int i = 0; i < data["items"][0]["readings"].Count; i++ )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Id] = data["items"][0]["readings"][i]["station_id"].Value;

			if ( !listStations.Exists(item => item.id == (string)entry[Headers.Id]) )
			{
				Debug.Log(entry[Headers.Id] + " doesnt match");
				storeStations(strJson);
			}

			stations station = listStations.Find(item => item.id == (string)entry[Headers.Id]);
			entry[Headers.Name] = station.name;
			entry[Headers.Lat] = station.lat;
			entry[Headers.Lng] = station.lng;
			entry[Headers.Rain] = data["items"][0]["readings"][i]["value"].AsFloat;
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

		for ( int i = 0; i < data["items"][0]["readings"].Count; i++ )
		{
			var entry = new Dictionary<string, object>();

			entry[Headers.Id] = data["items"][0]["readings"][i]["station_id"].Value;

			if ( !listStations.Exists(item => item.id == (string)entry[Headers.Id]) )
			{
				Debug.Log(entry[Headers.Id] + " doesnt match");
				storeStations(strJson);
			}

			stations station = listStations.Find(item => item.id == (string)entry[Headers.Id]);
			entry[Headers.Name] = station.name;
			entry[Headers.Lat] = station.lat;
			entry[Headers.Lng] = station.lng;
			entry[Headers.Rain] = data["items"][0]["readings"][i]["value"].AsFloat;
			entry[Headers.Timestamp] = data["items"][0]["timestamp"].Value;

			listPastData.Add(entry);
		}

		return listPastData;
	}
}