using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFunction : MonoBehaviour {

	public delegate bool DelegateTurnAlpha();
	public Func<GameObject, bool> TurnAlphaB;
	// Use this for initialization

	public bool TurnAlphaC(Func<bool> myMethodName)
	{
		Debug.Log("from delegate " + myMethodName());
		return true;
	}

}
