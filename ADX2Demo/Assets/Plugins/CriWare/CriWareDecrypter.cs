/****************************************************************************
 *
 * Copyright (c) 2018 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System;
using System.Runtime.InteropServices;

/// \addtogroup CRIWARE_COMMON_CLASS
/// @{

/**
 * <summary>暗号化データ再生のための設定機能を提供するクラスです。</summary>
 * \par 説明:
 * 暗号化データ再生時の復号設定を行う機能を提供します。<br/>
 * 本クラスが提供する関数を呼び出すことで、復号機能を初期化することができます。
 */
public static class CriWareDecrypter {

	/**
	 * <summary>初期化(コンフィグ指定)</summary> 
	 * <param name="config">初期化コンフィグ</param>
	 * <returns>初期化に成功したか</returns>
	 * \par 説明:
	 * Decrytper の初期化を行います。<br/>
	 * CriWareInitializerに設定を行った場合、本関数は自動で呼び出されます。<br/>
	 * \attention
	 * 本関数は、FileSystemライブラリ初期化を行ったあとに呼び出してください。
	 */
	public static bool Initialize(CriWareDecrypterConfig config) {
		return Initialize(config.key, config.authenticationFile, 
							config.enableAtomDecryption, config.enableManaDecryption);
	}

	/**
	 * <summary>初期化(パラメータ指定)</summary> 
	 * <param name="key">暗号キー</param>
	 * <param name="authenticationFile">認証ファイルパス(絶対パス、またはSreamingAssetsからの相対パス)</param>
	 * <param name="enableAtomDecryption">音声データを復号するか</param>
	 * <param name="enableManaDecryption">動画データを復号するか</param>
	 * <returns>初期化に成功したか</returns>
	 * \par 説明:
	 * Decrytper の初期化を行います。<br/>
	 * CriWareInitializerに設定を行った場合、本関数は自動で呼び出されます。<br/>
	 * \attention
	 * 本関数は、FileSystemライブラリ初期化を行ったあとに呼び出してください。
	 */
	public static bool Initialize(string key, string authenticationFile, bool enableAtomDecryption, bool enableManaDecryption) {
		if (!CriFsPlugin.IsLibraryInitialized()) {
			return false;
		}

		/* バージョン番号が不正なライブラリには暗号キーを伝えない */
		/* 備考）不正に差し替えられたsoファイルを使用している可能性あり。 */
		bool isCorrectVersion = CriWare.CheckBinaryVersionCompatibility();
		if (isCorrectVersion == false) {
			return false;
		}

		ulong decryptionKey = (key.Length == 0) ? 0 : System.Convert.ToUInt64(key);
		string authenticationPath = authenticationFile;
		if (CriWare.IsStreamingAssetsPath(authenticationPath)) {
			authenticationPath = System.IO.Path.Combine(CriWare.streamingAssetsPath, authenticationPath);
		}

		temporalStorage = decryptionKey ^ 0x00D47EB533AEF7E5UL;
		CRIWAREF93901C7(enableAtomDecryption, enableManaDecryption, CallbackFromNative, IntPtr.Zero);
		temporalStorage = 0;

		return true;
	}

	/* 変数の一時的な格納場所 */
	private static ulong temporalStorage = 0;

	#region Private Methods
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate ulong CallbackFromNativeDelegate(System.IntPtr ptr1);

	[AOT.MonoPInvokeCallback(typeof(CallbackFromNativeDelegate))]
	private static ulong CallbackFromNative(System.IntPtr ptr1)
	{
		return temporalStorage;
	}
	#endregion

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern int CRIWAREF93901C7(
		bool enable_atom_decryption, bool enable_mana_decryption, CallbackFromNativeDelegate func, IntPtr obj
	);
	#else
	public static int CRIWAREF93901C7(
		bool enable_atom_decryption, bool enable_mana_decryption, CallbackFromNativeDelegate func, IntPtr obj
		) { return 0; }
	#endif
	#endregion
} // end of class

/// @}

/* --- end of file --- */
