using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowDetails : MonoBehaviour
{

	public GameObject canvasMenuPreFab;
	public GameObject canvasMenuHolder;
	public GameObject scaler;
	public Avpl.InputKey key_plus;
	public Avpl.InputKey key_minus;
	public Avpl.InputKey button_select;
	public Avpl.InputKey button_menu;
	public Avpl.InputKey button_menuCycle;
	public Avpl.InputKey button_menuSelect;
	public Avpl.InputKey button_test;
	public Text txtDetails;

	private int menuSelected = 1;
	private int scaleFactor = 1;
	private UIInputTarget[] allTargets;


	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if ( button_select.IsToggled() )
		{
			allTargets = GetComponentsInChildren<UIInputTarget>();
			List<UIInputTarget> RaycastHits = new List<UIInputTarget>();
			//This loop performs the UI "raycast" using the ray given from GetSelectionRay();
			foreach ( UIInputTarget target in allTargets )
			{
				//Skip objects that are inactive
				if ( !target.gameObject.activeInHierarchy )
					continue;

				Vector3 hitPos;
				if ( RayIntersectsRectTransform(target.RectTransform, Avpl.AvplStatic.GetRay(), out hitPos) )
				{
					RaycastHits.Add(target);
				}
			}

			RaycastHit hit;
			if ( Physics.Raycast(Avpl.AvplStatic.GetRay(), out hit) )
			{
				GameObject hitObject = hit.collider.gameObject;
				if ( RaycastHits.Count != 0 )
				{
					RaycastHits = RaycastHits.OrderByDescending(x => x.Graphic.depth).ToList();
					if (
						( Avpl.AvplStatic.wandRay.transform.position.z - RaycastHits[0].transform.position.z ) >
						 ( Avpl.AvplStatic.wandRay.transform.position.z - hitObject.transform.position.z )
					)
					{
						RaycastHits[0].OnClick();
						return;
					}
				}
				//Debug.Log("UI depth = " + ( Avpl.AvplStatic.wandRay.transform.position - RaycastHits[0].transform.position));
				//Debug.Log("Game depth = " + ( Avpl.AvplStatic.wandRay.transform.position - hitObject.transform.position ));
				if ( hitObject.GetComponent<DataDetails>()
					&& hitObject.GetComponent<Renderer>().enabled )
				{
					txtDetails.text = (string)hitObject.GetComponent<DataDetails>().fullDetails;
					GameObject txtDetailCanvas = GameObject.Find("DetailCanvas");

					txtDetailCanvas.transform.rotation = Quaternion.identity;
					txtDetailCanvas.transform.position = Avpl.AvplStatic.wandRay.transform.position;
					Vector3 vDir = Avpl.AvplStatic.wandRay.transform.position - hitObject.transform.position;
					vDir.Normalize();
					vDir *= -0.5f;
					txtDetailCanvas.transform.Translate(vDir);
					txtDetailCanvas.transform.rotation = Avpl.AvplStatic.wandRay.transform.rotation;
					txtDetailCanvas.transform.Translate(Avpl.AvplStatic.wandRay.transform.up * 0.3f);

					//txtDetailCanvas.transform.rotation = Quaternion.identity;
					//txtDetailCanvas.transform.position = Avpl.AvplStatic.wandRay.transform.position;
					//txtDetailCanvas.transform.Translate(0, 0.3f, 0.3f);
					//txtDetailCanvas.transform.rotation = Avpl.AvplStatic.wandRay.transform.rotation;
					//txtDetailCanvas.transform.Translate(Avpl.AvplStatic.wandRay.transform.forward * 0.5f);
				}
				else
				{
					Debug.Log("hit - " + hit.collider.bounds);
					Debug.Log("i am hit instead - " + hitObject);
				}
			}
			else
			{
				if ( RaycastHits.Count != 0 )
				{
					RaycastHits = RaycastHits.OrderByDescending(x => x.Graphic.depth).ToList();
					RaycastHits[0].OnClick();
				}
			}
		}
		else if ( button_menu.IsToggled() )
		{
			RaycastHit hit;
			if ( Physics.Raycast(Avpl.AvplStatic.GetRay(), out hit) )
			{
				GameObject hitObject = hit.collider.gameObject;
				if ( hitObject.transform.parent.transform.parent.transform.GetComponent<PlotterInterface>() != null
					&& hitObject.GetComponent<Renderer>().enabled )
				{
					GameObject menuCanvas = Instantiate(canvasMenuPreFab, canvasMenuHolder.transform.position, Quaternion.identity, canvasMenuHolder.transform);
					canvasMenuHolder.SetActive(true);

					// not sure why default menu is scale to 100X
					menuCanvas.transform.localScale = new Vector3(1, 1, 1);

					float x = ( -canvasMenuHolder.GetComponent<RectTransform>().rect.width / 2 )
								+ ( menuCanvas.GetComponent<RectTransform>().rect.width * ( canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length - 1 ) );
					Vector3 pos = menuCanvas.transform.TransformPoint(x,
																		canvasMenuHolder.GetComponent<RectTransform>().rect.height / 2,
																		0);
					menuCanvas.transform.Translate(pos.x, pos.y, 1.0f * menuCanvas.transform.localToWorldMatrix.m00);

					menuCanvas.GetComponentInChildren<Text>().text = hitObject.transform.parent.transform.parent.GetComponent<PlotterInterface>().GetDimension();
					menuCanvas.GetComponentInChildren<MenuFunction>().TurnAlphaB = hitObject.transform.parent.transform.parent.GetComponent<PlotterInterface>().TurnAlpha;
					menuCanvas.name = hitObject.transform.parent.transform.parent.GetComponent<PlotterInterface>().GetDimension();
					hitObject.transform.parent.transform.parent.GetComponent<PlotterInterface>().TurnAlpha();
				}
			}
		}
		else if ( button_menuCycle.IsToggled() )
		{
			if ( canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length > 1 )
			{
				GameObject canvasMenuHighlight = GameObject.Find("CanvasMenuHighlight");

				menuSelected++;
				if ( menuSelected > canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length )
				{
					menuSelected = 1;
					float x = -canvasMenuHighlight.GetComponent<RectTransform>().rect.width * ( canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length - 1 );
					x *= canvasMenuHighlight.transform.localToWorldMatrix.m00;
					canvasMenuHighlight.transform.Translate(x, 0, 0);
				}
				else
				{
					float x = canvasMenuHighlight.GetComponent<RectTransform>().rect.width;
					x *= canvasMenuHighlight.transform.localToWorldMatrix.m00;
					canvasMenuHighlight.transform.Translate(x, 0, 0);
				}

				Debug.Log(menuSelected + " position");

			}
		}
		else if ( button_menuSelect.IsToggled() )
		{
			if ( canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length > 0 )
			{
				canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>()[menuSelected - 1].TurnAlphaB(null);
				Destroy(canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>()[menuSelected - 1].transform.parent.gameObject);
				if ( menuSelected > canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length - 1 )
				{
					menuSelected--;
					if ( menuSelected < 1 )
					{
						canvasMenuHolder.SetActive(false);
						menuSelected = 1;
					}
					else
					{
						GameObject canvasMenuHighlight = GameObject.Find("CanvasMenuHighlight");
						float x = -canvasMenuHighlight.GetComponent<RectTransform>().rect.width;
						x *= canvasMenuHighlight.transform.localToWorldMatrix.m00;
						canvasMenuHighlight.transform.Translate(x, 0, 0);
					}
				}
				else
				{
					for ( int i = menuSelected; i < canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>().Length; i++ )
					{
						GameObject menuCanvas = canvasMenuHolder.transform.GetComponentsInChildren<MenuFunction>()[i].transform.parent.gameObject;
						float x = -menuCanvas.GetComponent<RectTransform>().rect.width;
						x *= menuCanvas.transform.localToWorldMatrix.m00;
						menuCanvas.transform.Translate(x, 0, 0);
					}
				}
			}
		}
		else if ( key_plus.IsToggled() )
		{
			scaleFactor++;
			scaler.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
		}
		else if ( key_minus.IsToggled() )
		{
			if (scaleFactor > 1)
				scaleFactor--;
			scaler.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
		}
	}

	public static bool RayIntersectsRectTransform(RectTransform rectTransform, Ray ray, out Vector3 worldPos)
	{
		Vector3[] corners = new Vector3[4];
		rectTransform.GetWorldCorners(corners);
		Plane plane = new Plane(corners[0], corners[1], corners[2]);

		float enter;
		if ( !plane.Raycast(ray, out enter) )
		{
			worldPos = Vector3.zero;
			return false;
		}

		Vector3 intersection = ray.GetPoint(enter);

		Vector3 BottomEdge = corners[3] - corners[0];
		Vector3 LeftEdge = corners[1] - corners[0];
		float BottomDot = Vector3.Dot(intersection - corners[0], BottomEdge);
		float LeftDot = Vector3.Dot(intersection - corners[0], LeftEdge);
		if ( BottomDot < BottomEdge.sqrMagnitude && // Can use sqrMag because BottomEdge is not normalized
			LeftDot < LeftEdge.sqrMagnitude &&
				BottomDot >= 0 &&
				LeftDot >= 0 )
		{
			worldPos = corners[0] + LeftDot * LeftEdge / LeftEdge.sqrMagnitude + BottomDot * BottomEdge / BottomEdge.sqrMagnitude;
			return true;
		}
		else
		{
			worldPos = Vector3.zero;
			return false;
		}
	}


}
