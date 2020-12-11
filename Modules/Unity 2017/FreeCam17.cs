using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;
using UnityEngine;
using PoseMod;

namespace PoseMod
{
	public static class Module
	{
		public static void Init()
		{
			"Free cam initiated".Log();

			PoseMod.Core.Pm_Gameobject.AddComponent<FreeCam>();
		}
	}


	public class FreeCam : MonoBehaviour
	{
		public static bool Active
		{
			get
			{
				return _FreeCamGM != null && _FreeCamGM.gameObject.activeSelf;
			}
			set
			{
				"Free cam triggered".Log();

				if(value)
					FlyWithCam();
				else
					StopFlyWithCam();
			}
		}



		public void Update()
		{
			if(Input.GetKeyUp(KeyCode.F11))
			{
				Active = !Active;
			}
			if(Input.GetKeyUp(KeyCode.F12))
			{
				FreeCamRotation.NoDeltaTime = !FreeCamRotation.NoDeltaTime;
			}
		}

		public void OnGUI()
		{
			GUI.color = Color.red;
			GUI.Label(new Rect(0, 0, 1000, 30), "PoseMod Loaded");
		}


		public static Transform _FreeCamGM;
		public static Transform FreeCamGM
		{
			get
			{
				if(_FreeCamGM == null)
				{
					_FreeCamGM = new GameObject("PoseMod_FreeCam", typeof(FreeCamRotation)).transform;
					Camera _cam = _FreeCamGM.gameObject.AddComponent<Camera>();
					_cam.nearClipPlane = 0.01f;
					_cam.farClipPlane = 1000f;
				}

				return _FreeCamGM;
			}
		}


		public static void FlyWithCam()
		{
			FreeCamGM.gameObject.SetActive(true);
			//FreeCamGM.position = YanSim.Yan.RPGCamera.transform.position;
		}
		public static void StopFlyWithCam()
		{
			FreeCamGM.gameObject.SetActive(false);
		}

	}



	public class FreeCamRotation : MonoBehaviour
	{
		public static bool NoDeltaTime;

		public Transform Rotationer;

		public void Start()
		{
			if(!Rotationer)
				Rotationer = this.transform;
		}

		float mX = 0f;
		float mY = 0f;

		float _moveDelta;

		public void LateUpdate()
		{
			float v = Input.GetAxis ("Vertical");
			float h = Input.GetAxis ("Horizontal");
			float mW = Input.GetAxis ("Mouse ScrollWheel");

			float _cameraMoveSpeed;

			if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				_cameraMoveSpeed = 0.5f;
			}
			else
			{
				_cameraMoveSpeed = 0.1f;
			}

			if (Input.GetMouseButton (1))
			{
				mX += Input.GetAxis ("Mouse X") * 5f;
				mY += Input.GetAxis ("Mouse Y") * 5f;

				Rotationer.localRotation = Quaternion.AngleAxis (mX, Vector3.up);
				Rotationer.localRotation *= Quaternion.AngleAxis (mY, Vector3.left);
			}

			if(NoDeltaTime)
			{
				_moveDelta = 0.01f;
			}
			else
			{
				_moveDelta = Time.deltaTime;
				if(_moveDelta == 0)
					_moveDelta = 0.01f;
			}

			Vector3 forwardVec = v == 0f ? Rotationer.forward * mW * _moveDelta * 600 : Rotationer.forward * _cameraMoveSpeed * v;
			Rotationer.position += forwardVec;
			Rotationer.position += Rotationer.right * _cameraMoveSpeed * h;
		}
	}
}

