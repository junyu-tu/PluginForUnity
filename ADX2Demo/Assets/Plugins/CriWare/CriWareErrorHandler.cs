/****************************************************************************
 *
 * Copyright (c) 2012 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if !(!UNITY_EDITOR && UNITY_IOS && ENABLE_MONO)
#define CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
#endif

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// \addtogroup CRIWARE_UNITY_COMPONENT
/// @{

/*JP
 * \brief CRIWAREエラーオブジェクト
 * \par 説明:
 * CRIWAREライブラリの初期化を行うためのコンポーネントです。<br>
 */
[AddComponentMenu("CRIWARE/Error Handler")]
public class CriWareErrorHandler : CriMonoBehaviour{
	/*JP
	 * \brief コンソールデバッグ出力を有効にするかどうか
	 * \par 注意:
	 * Unityデバッグウィンドウだけでなく、コンソールデバッグ出力を有効にするかどうか [deprecated]
	 * PCの場合はデバッグウィンドウに出力されます。
	 */
	public bool enableDebugPrintOnTerminal = false;

	/*JP エラー発生時に強制的にクラッシュさせるかどうか(デバッグ用) */
	public bool enableForceCrashOnError = false;

	/*JP シーンチェンジ時にエラーハンドラを削除するかどうか */
	public bool dontDestroyOnLoad = true;

	/*JP エラーメッセージ */
	public static string errorMessage { get; set; }

	/* オブジェクト作成時の処理 */
	protected override void Awake() {
		/* 初期化カウンタの更新 */
		initializationCount++;
		if (initializationCount != 1) {
			/* 多重初期化は許可しない */
			GameObject.Destroy(this);
			return;
		}

		base.Awake();
		/* エラー処理の初期化 */
		CRIWARED9F2C763();
		CRIWARE7BD1CB62(enableForceCrashOnError);
		
		/* ターミナルへのログ出力表示切り替え */
		CRIWARE8209F481(enableDebugPrintOnTerminal);

#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
		criWareUnity_SetErrorCallback(ErrorCallbackFromNative);
#endif

		/* シーンチェンジ後もオブジェクトを維持するかどうかの設定 */
		if (dontDestroyOnLoad) {
			DontDestroyOnLoad(transform.gameObject);
		}
	}
	
	/* Execution Order の設定を確実に有効にするために OnEnable をオーバーライド */
	void OnEnable() {
#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
		criWareUnity_SetErrorCallback(ErrorCallbackFromNative);
#endif
	}

	void OnDisable() {
#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
		criWareUnity_SetErrorCallback(null);
#endif
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	public override void CriInternalUpdate() {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS || UNITY_TVOS)
		if (enableDebugPrintOnTerminal == false){
			/* iOS/Androidの場合、エラーメッセージの出力先が重複してしまうため、*/
			/* ターミナル出力が無効になっている場合のみ、Unity出力を有効にする。*/
			OutputErrorMessage();
		}
#else
		OutputErrorMessage();
#endif
	}
	
	public override void CriInternalLateUpdate() { }

	protected override void OnDestroy() {
		/* 初期化カウンタの更新 */
		initializationCount--;
		if (initializationCount != 0) {
			return;
		}

		base.OnDestroy();
#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
		criWareUnity_SetErrorCallback(null);
#endif
		
		/* エラー処理の終了処理 */
		CRIWARE5A546EAC();
	}
	
	/* エラーメッセージのポーリングと出力 */
	private static void OutputErrorMessage() 
	{
		/* エラーメッセージのポーリング */
		System.IntPtr ptr = CRIWARE0B112F70();
		if (ptr == IntPtr.Zero) {
			return;
		}

		/* Unity上でログ出力 */
		string message = Marshal.PtrToStringAnsi(ptr);
		if (message != string.Empty) {
			OutputLog(message);
			CRIWARECD8FFB8C();
		}
		
		if (CriWareErrorHandler.errorMessage == null) {
			CriWareErrorHandler.errorMessage = message.Substring(0);
		}
	}

	/*JP ログの出力 */
	private static void OutputLog(string errmsg)
	{
		if (errmsg == null) {
			return;
		}
		
		if (errmsg.StartsWith("E")) {
			Debug.LogError("[CRIWARE] Error:" + errmsg);
		} else if (errmsg.StartsWith("W")) {
			Debug.LogWarning("[CRIWARE] Warning:" + errmsg);
		} else {
			Debug.Log("[CRIWARE]" + errmsg);
		}
	}

#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void ErrorCallbackFunc(string errmsg);

	[AOT.MonoPInvokeCallback(typeof(ErrorCallbackFunc))]
	private static void ErrorCallbackFromNative(string errmsg)
	{
		OutputLog(errmsg);
	}
#endif

	/*JP 初期化カウンタ */
	private static int initializationCount = 0;

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARED9F2C763();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE5A546EAC();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern System.IntPtr CRIWARE0B112F70();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARECD8FFB8C();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE8209F481(bool sw);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE7BD1CB62(bool sw);

#if CRIWAREERRORHANDLER_SUPPORT_NATIVE_CALLBACK
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void criWareUnity_SetErrorCallback(ErrorCallbackFunc callback);
#endif
	#else
	private static void CRIWARED9F2C763() { }
	private static void CRIWARE5A546EAC() { }
	private static System.IntPtr CRIWARE0B112F70() { return System.IntPtr.Zero; }
	private static void CRIWARECD8FFB8C() { }
	private static void CRIWARE8209F481(bool sw) { }
	private static void CRIWARE7BD1CB62(bool sw) { }
	private static void criWareUnity_SetErrorCallback(ErrorCallbackFunc callback) { }
	#endif
	#endregion
} // end of class

/// @}

/* --- end of file --- */
