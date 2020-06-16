/****************************************************************************
 *
 * Copyright (c) 2011 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

/*---------------------------
 * Force Load Data with Async Method Defines
 *---------------------------*/
#if UNITY_WEBGL
	#define CRIWARE_FORCE_LOAD_ASYNC
#endif

#if !(!UNITY_EDITOR && UNITY_IOS && ENABLE_MONO)
	#define CRIWARE_SUPPORT_NATIVE_CALLBACK
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
#if !UNITY_EDITOR && (UNITY_WINRT && !ENABLE_IL2CPP)
using System.Reflection;
#endif

public static class CriAtomPlugin
{
	#region Editor/Runtime共通

#if UNITY_EDITOR
	public static bool showDebugLog = false;
	public delegate void PreviewCallback();
	public static PreviewCallback previewCallback = null;
#endif

	public static void Log(string log)
	{
	#if UNITY_EDITOR
		if (CriAtomPlugin.showDebugLog) {
			Debug.Log(log);
		}
	#endif
	}

	/* 初期化カウンタ */
	private static int initializationCount = 0;

	public static bool isInitialized { get { return initializationCount > 0; } }

	private static List<IntPtr> effectInterfaceList = null;
	public static bool GetAudioEffectInterfaceList(out List<IntPtr> effect_interface_list)
	{
		if (CriAtomPlugin.IsLibraryInitialized() == true) {
			effect_interface_list = null;
			return false;
		}
		if (effectInterfaceList == null) {
			effectInterfaceList = new List<IntPtr>();
		}
		effect_interface_list = effectInterfaceList;
		return true;
	}

	private static IntPtr GetSpatializerCoreInterfaceFromAtomOculusAudioBridge()
	{
		/* Ambisonic データを再生するために、プラットフォームによってはブリッジプラグインを必要とするかもしれない
		 * 例えば PC では Oculus Audio ブリッジプラグインを使う。
		 * 例えば PS4 では ブリッジプラグインを使わない */
		/* 以下、CRI Atom Oculus Audio ブリッジプラグインがインポートされている場合の処理 */
		Type type = Type.GetType("CriAtomOculusAudio");
		if (type == null) {
			/* BridgePluginが見つからなかった */
			Debug.LogError("[CRIWARE] ERROR: Cri Atom Oculus Audio Bridge Plugin is not imported.");
		} else {
			/* 現在のプラットフォームは Oculus Audio Bridge Plugin をサポートしているか確認 */
#if !UNITY_EDITOR && (UNITY_WINRT && !ENABLE_IL2CPP)
			System.Reflection.MethodInfo method_support_current_platform = type.GetTypeInfo().GetDeclaredMethod("SupportCurrentPlatform");
#else
			System.Reflection.MethodInfo method_support_current_platform = type.GetMethod("SupportCurrentPlatform");
#endif
			if (method_support_current_platform == null) {
				Debug.LogError("[CRIWARE] ERROR: CriAtomOculusAudio.SupportCurrentPlatform method is not found.");
				return IntPtr.Zero;
			}
			bool current_platform_supports_oculus_audio = (bool)method_support_current_platform.Invoke(null, null);
			/* カレントプラットフォームをサポートしているなら準備。
				* サポートしていないならスキップ。引き続き Atom 初期化処理を行う */
			if (current_platform_supports_oculus_audio) {
				/* 必要なメソッド情報を取得 */
#if !UNITY_EDITOR && (UNITY_WINRT && !ENABLE_IL2CPP)
				System.Reflection.MethodInfo method_get_spatializer_core_interface = type.GetTypeInfo().GetDeclaredMethod("GetSpatializerCoreInterface");
#else
				System.Reflection.MethodInfo method_get_spatializer_core_interface = type.GetMethod("GetSpatializerCoreInterface");
#endif
				if (method_get_spatializer_core_interface == null) {
					Debug.LogError("[CRIWARE] ERROR: CriAtomOculusAudio.GetSpatializerCoreInterface method is not found.");
					return IntPtr.Zero;
				}
				/* Spatilalizer の初期化に必要な情報を取得 */
				return (IntPtr)method_get_spatializer_core_interface.Invoke(null, null);
			}
		}
		return IntPtr.Zero;
	}

	public static void SetConfigParameters(int max_virtual_voices,
		int max_voice_limit_groups, int max_categories,
		int max_sequence_events_per_frame, int max_beatsync_callbacks_per_frame,
		int num_standard_memory_voices, int num_standard_streaming_voices,
		int num_hca_mx_memory_voices, int num_hca_mx_streaming_voices,
		int output_sampling_rate, int num_asr_output_channels,
		bool uses_in_game_preview, float server_frequency,
		int max_parameter_blocks,  int categories_per_playback,
		int num_buses, bool vr_mode)
	{
		IntPtr spatializer_core_interface = IntPtr.Zero;
		/* Ambisonic データの再生に必要な初期化パラメータを取得する */
		if (vr_mode) {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_ANDROID
			spatializer_core_interface = CriAtomPlugin.GetSpatializerCoreInterfaceFromAtomOculusAudioBridge();
#endif
		}
#if UNITY_WEBGL
		/* WebGLでは普通のボイスは作成することができない */
		num_standard_memory_voices = 0;
		num_standard_streaming_voices = 0;
		num_hca_mx_memory_voices = 0;
		num_hca_mx_streaming_voices = 0;
#endif
		CRIWAREDD6E6784(max_virtual_voices,
			max_voice_limit_groups, max_categories, 
			max_sequence_events_per_frame, max_beatsync_callbacks_per_frame,
			num_standard_memory_voices, num_standard_streaming_voices,
			num_hca_mx_memory_voices, num_hca_mx_streaming_voices,
			output_sampling_rate, num_asr_output_channels,
			uses_in_game_preview, server_frequency,
			max_parameter_blocks, categories_per_playback,
			num_buses, vr_mode,
			spatializer_core_interface);

		CriAtomPlugin.isConfigured = true;
	}

	public static void SetConfigAdditionalParameters_PC(long buffering_time_pc)
	{
		CRIWARE823A82C6(buffering_time_pc);
	}

	public static void SetConfigAdditionalParameters_LINUX(CriAtomConfig.LinuxOutput output)
	{
		CRIWARE62804F99((int)output);
	}

	public static void SetConfigAdditionalParameters_IOS(uint buffering_time_ios, bool override_ipod_music_ios)
	{
		CRIWAREA0B4ABAA(buffering_time_ios, override_ipod_music_ios);
	}

	public static void SetConfigAdditionalParameters_ANDROID(int num_low_delay_memory_voices, int num_low_delay_streaming_voices,
															 int sound_buffering_time,		  int sound_start_buffering_time,
															 bool use_fast_mixer)
	{
		CRIWARE8038736A(num_low_delay_memory_voices, num_low_delay_streaming_voices,
														   sound_buffering_time,		sound_start_buffering_time,
														   use_fast_mixer);
#if !UNITY_EDITOR && UNITY_ANDROID
		if (use_fast_mixer == true) {
			IntPtr android_context = IntPtr.Zero;
			using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity")) {
				android_context = activity.GetRawObject();
				criAtomUnity_ApplyHardwareProperty_ANDROID(android_context);
			}
		}
#endif
	}

	public static void SetConfigAdditionalParameters_VITA(int num_atrac9_memory_voices, int num_atrac9_streaming_voices, int num_mana_decoders)
	{
		#if !UNITY_EDITOR && UNITY_PSP2
		CRIWARED642FD19(num_atrac9_memory_voices, num_atrac9_streaming_voices, num_mana_decoders);
		#endif
	}

	public static void SetConfigAdditionalParameters_PS4(int num_atrac9_memory_voices, int num_atrac9_streaming_voices,
														 bool use_audio3d, int num_audio3d_memory_voices, int num_audio3d_streaming_voices)
	{
		#if !UNITY_EDITOR && UNITY_PS4
		CRIWAREB6DE9CCF(num_atrac9_memory_voices, num_atrac9_streaming_voices,
														use_audio3d, num_audio3d_memory_voices, num_audio3d_streaming_voices);
		#endif
	}

	public static void SetConfigAdditionalParameters_WEBGL(int num_webaudio_voices)
	{
		#if UNITY_WEBGL
		CRIWAREB3329845(num_webaudio_voices);
		#endif
	}

	public static void SetMaxSamplingRateForStandardVoicePool(int sampling_rate_for_memory, int sampling_rate_for_streaming)
	{
		CRIWAREF81EF758(sampling_rate_for_memory, sampling_rate_for_streaming);
	}

	public static int GetRequiredMaxVirtualVoices(CriAtomConfig atomConfig)
	{
		/* バーチャルボイスは、全ボイスプールのボイスの合計値よりも多くなくてはならない */
		int requiredVirtualVoices = 0;

		requiredVirtualVoices += atomConfig.standardVoicePoolConfig.memoryVoices;
		requiredVirtualVoices += atomConfig.standardVoicePoolConfig.streamingVoices;
		requiredVirtualVoices += atomConfig.hcaMxVoicePoolConfig.memoryVoices;
		requiredVirtualVoices += atomConfig.hcaMxVoicePoolConfig.streamingVoices;

		#if UNITY_ANDROID
		requiredVirtualVoices += atomConfig.androidLowLatencyStandardVoicePoolConfig.memoryVoices;
		requiredVirtualVoices += atomConfig.androidLowLatencyStandardVoicePoolConfig.streamingVoices;
		#elif UNITY_PSP2
		requiredVirtualVoices += atomConfig.vitaAtrac9VoicePoolConfig.memoryVoices;
		requiredVirtualVoices += atomConfig.vitaAtrac9VoicePoolConfig.streamingVoices;
		#endif

		return requiredVirtualVoices;
	}

	public static void InitializeLibrary()
	{
		/* 初期化カウンタの更新 */
		CriAtomPlugin.initializationCount++;
		if (CriAtomPlugin.initializationCount != 1) {
			return;
		}

		/* シーン実行前に初期化済みの場合は終了させる */
		if (CriAtomPlugin.IsLibraryInitialized() == true) {
			CriAtomPlugin.FinalizeLibrary();
			CriAtomPlugin.initializationCount = 1;
		}

		/* 初期化パラメータが設定済みかどうかを確認 */
		if (CriAtomPlugin.isConfigured == false) {
			Debug.Log("[CRIWARE] Atom initialization parameters are not configured. "
				+ "Initializes Atom by default parameters.");
		}

		/* 依存ライブラリの初期化 */
		CriFsPlugin.InitializeLibrary();

		/* ライブラリの初期化 */
		CriAtomPlugin.CRIWARE1E282908();

		/* ユーザカスタムエフェクトプラグインのインタフェースを登録 */
	#if !UNITY_EDITOR && UNITY_PSP2
		// unsupported
	#else
		if (effectInterfaceList != null)
		{
			for (int i = 0; i < effectInterfaceList.Count; i++)
			{
				CriAtomExAsr.RegisterEffectInterface(effectInterfaceList[i]);
			}
		}
	#endif

		/* CriAtomServerのインスタンスを生成 */
		#if UNITY_EDITOR
		/* ゲームプレビュー時のみ生成する */
		if (UnityEngine.Application.isPlaying) {
			CriAtomServer.CreateInstance();
		}
		#else
		CriAtomServer.CreateInstance();
		#endif

		/* CriAtomListener の共有ネイティブリスナーを生成 */
		CriAtomListener.CreateSharedNativeListener();
	}

	public static bool IsLibraryInitialized()
	{
		/* ライブラリが初期化済みかチェック */
		return CRIWAREC80540F4();
	}

	public static void FinalizeLibrary()
	{
		/* 初期化カウンタの更新 */
		CriAtomPlugin.initializationCount--;
		if (CriAtomPlugin.initializationCount < 0) {
			CriAtomPlugin.initializationCount = 0;
			if (CriAtomPlugin.IsLibraryInitialized() == false) {
				return;
			}
		}
		if (CriAtomPlugin.initializationCount != 0) {
			return;
		}

		/* CriAtomListener の共有ネイティブリスナーを破棄 */
		CriAtomListener.DestroySharedNativeListener();

		/* CriAtomServerのインスタンスを破棄 */
		CriAtomServer.DestroyInstance();

		/* 未破棄のDisposableを破棄 */
		CriDisposableObjectManager.CallOnModuleFinalization(CriDisposableObjectManager.ModuleType.Atom);

		/* ユーザエフェクトインタフェースのリストをクリア */
		if (effectInterfaceList != null) {
			effectInterfaceList.Clear();
			effectInterfaceList = null;
		}

		/* ライブラリの終了 */
		CriAtomPlugin.CRIWARE66F94C37();

		/* 依存ライブラリの終了 */
		CriFsPlugin.FinalizeLibrary();
	}

	public static void Pause(bool pause)
	{
		if (isInitialized) {
			CRIWARE6952D387(pause);
		}
	}

	#if !UNITY_EDITOR && UNITY_IOS
	public static void CallOnApplicationResume_IOS()
	{
		criAtomUnity_SleepToDelay_IOS(100);
	}
	#endif

	private static bool isConfigured = false;
	private static float timeSinceStartup = Time.realtimeSinceStartup;
	private static CriWare.CpuUsage cpuUsage;
	public static CriWare.CpuUsage GetCpuUsage()
	{
		float currentTimeSinceStartup = Time.realtimeSinceStartup;
		if (currentTimeSinceStartup - timeSinceStartup > 1.0f) {
			CriAtomEx.PerformanceInfo info;
			CriAtomEx.GetPerformanceInfo(out info);

			cpuUsage.last = info.lastServerTime * 100.0f / info.averageServerInterval;
			cpuUsage.average = info.averageServerTime * 100.0f / info.averageServerInterval;
			cpuUsage.peak = info.maxServerTime * 100.0f / info.averageServerInterval;

			CriAtomEx.ResetPerformanceMonitor();
			timeSinceStartup = currentTimeSinceStartup;
		}
		return cpuUsage;
	}

	private static int CRIATOMUNITY_PARAMETER_ID_LOOP_COUNT = 0;
	private static ushort CRIATOMPARAMETER2_ID_INVALID = ushort.MaxValue;

	public static ushort GetLoopCountParameterId()
	{
		ushort ret = CRIWARED08A6CB7(CRIATOMUNITY_PARAMETER_ID_LOOP_COUNT);
		if (ret == CRIATOMPARAMETER2_ID_INVALID) {
			throw new Exception("GetNativeParameterId failed.");
		}
		return ret;
	}

	public static void DecryptAcb(IntPtr acb_hn, ulong key, ulong nonce)
	{
		temporalStorage = key ^ 0x0017D207B5350050UL;
		CRIWARE62AB8ED8(acb_hn, CallbackFromNative, IntPtr.Zero);
		temporalStorage = 0;
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
	private static extern void CRIWAREDD6E6784(int max_virtual_voices,
		int max_voice_limit_groups, int max_categories,
		int max_sequence_events_per_frame, int max_beatsync_callbacks_per_frame,
		int num_standard_memory_voices, int num_standard_streaming_voices,
		int num_hca_mx_memory_voices, int num_hca_mx_streaming_voices,
		int output_sampling_rate, int num_asr_output_channels,
		bool uses_in_game_preview, float server_frequency,
		int max_parameter_blocks, int categories_per_playback,
		int num_buses, bool use_ambisonics,
		IntPtr spatializer_core_interface);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE823A82C6(long buffering_time_pc);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE62804F99(int output);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWAREA0B4ABAA(uint buffering_time_ios, bool override_ipod_music_ios);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE8038736A(int num_low_delay_memory_voices, int num_low_delay_streaming_voices,
																				  int sound_buffering_time,		   int sound_start_buffering_time,
																				  bool apply_hw_property);

	#if !UNITY_EDITOR && UNITY_ANDROID
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void criAtomUnity_ApplyHardwareProperty_ANDROID(IntPtr android_context);
	#endif

	#if !UNITY_EDITOR && UNITY_PSP2
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARED642FD19(int num_atrac9_memory_voices, int num_atrac9_streaming_voices, int num_mana_decoders);
	#endif

	#if !UNITY_EDITOR && UNITY_PS4
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWAREB6DE9CCF(int num_atrac9_memory_voices, int num_atrac9_streaming_voices,
																			  bool use_audio3d, int num_audio3d_memory_voices, int num_audio3d_streaming_voices);
	#endif

	#if UNITY_WEBGL
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWAREB3329845(int num_webaudio_voices);
	#endif

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE1E282908();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern bool CRIWAREC80540F4();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE66F94C37();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWARE6952D387(bool pause);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern uint CRIWARE366DD7CC();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE78B94547(int code);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE54141BCD(IntPtr cbfunc, string separator_string);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE34D5EE7B();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE31B98F5C(IntPtr cbfunc);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE73C35540();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	private static extern void CRIWAREF81EF758(int sampling_rate_for_memory, int sampling_rate_for_streaming);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE87886F3C();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWAREDF031BB6();

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void CRIWARE62AB8ED8(IntPtr acb_hn, CallbackFromNativeDelegate func, IntPtr obj);

	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern ushort CRIWARED08A6CB7(int id);

	#if !UNITY_EDITOR && UNITY_IOS
	[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
	public static extern void criAtomUnity_SleepToDelay_IOS(int milliseconds);
	#endif
	#else
	private static void CRIWAREDD6E6784(int max_virtual_voices,
		int max_voice_limit_groups, int max_categories,
		int max_sequence_events_per_frame, int max_beatsync_callbacks_per_frame,
		int num_standard_memory_voices, int num_standard_streaming_voices,
		int num_hca_mx_memory_voices, int num_hca_mx_streaming_voices,
		int output_sampling_rate, int num_asr_output_channels,
		bool uses_in_game_preview, float server_frequency,
		int max_parameter_blocks, int categories_per_playback,
		int num_buses, bool use_ambisonics,
		IntPtr spatializer_core_interface) { }
	private static void CRIWARE823A82C6(long buffering_time_pc) { }
	private static void CRIWARE62804F99(int output) { }
	private static void CRIWAREA0B4ABAA(uint buffering_time_ios, bool override_ipod_music_ios) { }
	private static void CRIWARE8038736A(int num_low_delay_memory_voices, int num_low_delay_streaming_voices,
																			int sound_buffering_time,		   int sound_start_buffering_time,
																			bool apply_hw_property) { }
	#if !UNITY_EDITOR && UNITY_ANDROID
	private static void criAtomUnity_ApplyHardwareProperty_ANDROID(IntPtr android_context) { }
	#endif

	#if !UNITY_EDITOR && UNITY_PSP2
	private static void CRIWARED642FD19(int num_atrac9_memory_voices, int num_atrac9_streaming_voices, 
																		int num_mana_decoders) { }
	#endif
	#if !UNITY_EDITOR && UNITY_PS4
	private static void CRIWAREB6DE9CCF(int num_atrac9_memory_voices, int num_atrac9_streaming_voices,
																		bool use_audio3d, int num_audio3d_memory_voices, int num_audio3d_streaming_voices) { }
	#endif
	#if UNITY_WEBGL
	private static void CRIWAREB3329845(int num_webaudio_voices) { }
	#endif
	private static bool _dummyInitialized = false;
	private static void CRIWARE1E282908() { _dummyInitialized = true; }
	public static bool CRIWAREC80540F4() { return _dummyInitialized; }
	private static void CRIWARE66F94C37() { _dummyInitialized = false; }
	private static void CRIWARE6952D387(bool pause) { }
	public static uint CRIWARE366DD7CC() { return 0; }
	public static void CRIWARE78B94547(int code) { }
	public static void CRIWARE54141BCD(IntPtr cbfunc, string separator_string) { }
	public static void CRIWARE34D5EE7B() { }
	public static void CRIWARE31B98F5C(IntPtr cbfunc) { }
	public static void CRIWARE73C35540() { }
	private static void CRIWAREF81EF758(int sampling_rate_for_memory, int sampling_rate_for_streaming) { }
	public static void CRIWARE87886F3C() { }
	public static void CRIWAREDF031BB6() { }
	public static void CRIWARE62AB8ED8(IntPtr acb_hn, CallbackFromNativeDelegate func, IntPtr obj) { }
	public static ushort CRIWARED08A6CB7(int id) { return 0; }
#if !UNITY_EDITOR && UNITY_IOS
	public static void criAtomUnity_SleepToDelay_IOS(int milliseconds) { }
#endif
	#endif
	#endregion

	#endregion
}

[Serializable]
public class CriAtomCueSheet
{
	public string name = "";
	public string acbFile = "";
	public string awbFile = "";
	public CriAtomExAcb acb;
	public CriAtomExAcbLoader.Status loaderStatus = CriAtomExAcbLoader.Status.Stop;
	public bool IsLoading { get { return loaderStatus == CriAtomExAcbLoader.Status.Loading; } }
	public bool IsError { get { return (loaderStatus == CriAtomExAcbLoader.Status.Error) || (!IsLoading && acb == null); } }
}

/// \addtogroup CRIATOM_UNITY_COMPONENT
/// @{
/**
 * <summary>サウンド再生全体を制御するためのコンポーネントです。</summary>
 * \par 説明:
 * 各シーンにひとつ用意しておく必要があります。<br/>
 * UnityEditor上でCRI Atomウィンドウを使用して CriAtomSource を作成した場合、
 * 「CRIWARE」という名前を持つオブジェクトとして自動的に作成されます。通常はユーザーが作成する必要はありません。
 */
[AddComponentMenu("CRIWARE/CRI Atom")]
public class CriAtom : CriMonoBehaviour
{

	/* @cond DOXYGEN_IGNORE */
	public string acfFile = "";
	private bool acfIsLoading = false;
#if CRIWARE_FORCE_LOAD_ASYNC
	private byte[] acfData = null;
#endif
	public CriAtomCueSheet[] cueSheets = {};
	public string dspBusSetting = "";
	public bool dontDestroyOnLoad = false;
#if CRIWARE_SUPPORT_NATIVE_CALLBACK
	private static CriAtomExSequencer.EventCbFunc eventUserCbFunc = null;
	private static CriAtomExBeatSync.CbFunc beatsyncUserCbFunc = null;
#endif
	private static CriAtom instance {
		get; set;
	}

	/* @endcond */

	#region Functions

	/**
	 * <summary>DSPバス設定のアタッチ</summary>
	 * <param name="settingName">DSPバス設定の名前</param>
	 * \par 説明:
	 * DSPバス設定からDSPバスを構築してサウンドレンダラにアタッチします。<br/>
	 * 現在設定中のDSPバス設定を切り替える場合は、一度デタッチしてから再アタッチしてください。
	 * <br/>
	 * \attention
	 * 本関数は完了復帰型の関数です。<br/>
	 * 本関数を実行すると、しばらくの間Atomライブラリのサーバ処理がブロックされます。<br/>
	 * 音声再生中に本関数を実行すると、音途切れ等の不具合が発生する可能性があるため、
	 * 本関数の呼び出しはシーンの切り替わり等、負荷変動を許容できるタイミングで行ってください。<br/>
	 * \sa CriAtom::DetachDspBusSetting
	 */
	public static void AttachDspBusSetting(string settingName)
	{
		CriAtom.instance.dspBusSetting = settingName;
		if (!String.IsNullOrEmpty(settingName)) {
			CriAtomEx.AttachDspBusSetting(settingName);
		} else {
			CriAtomEx.DetachDspBusSetting();
		}
	}

	/**
	 * <summary>DSPバス設定のデタッチ</summary>
	 * \par 説明:
	 * DSPバス設定をデタッチします。<br/>
	 * <br/>
	 * \attention
	 * 本関数は完了復帰型の関数です。<br/>
	 * 本関数を実行すると、しばらくの間Atomライブラリのサーバ処理がブロックされます。<br/>
	 * 音声再生中に本関数を実行すると、音途切れ等の不具合が発生する可能性があるため、
	 * 本関数の呼び出しはシーンの切り替わり等、負荷変動を許容できるタイミングで行ってください。<br/>
	 * \sa CriAtom::DetachDspBusSetting
	 */
	public static void DetachDspBusSetting()
	{
		CriAtom.instance.dspBusSetting = "";
		CriAtomEx.DetachDspBusSetting();
	}

	/**
	 * <summary>キューシートの取得</summary>
	 * <param name="name">キューシート名</param>
	 * <returns>キューシートオブジェクト</returns>
	 * \par 説明:
	 * 引数に指定したキューシート名を元に登録済みのキューシートオブジェクトを取得します。<br/>
	 */
	public static CriAtomCueSheet GetCueSheet(string name)
	{
		return CriAtom.instance.GetCueSheetInternal(name);
	}

	/**
	 * <summary>キューシートの追加</summary>
	 * <param name="name">キューシート名</param>
	 * <param name="acbFile">ACBファイルパス</param>
	 * <param name="awbFile">AWBファイルパス</param>
	 * <param name="binder">バインダオブジェクト(オプション)</param>
	 * <returns>キューシートオブジェクト</returns>
	 * \par 説明:
	 * 引数に指定したファイルパス情報を元にキューシートの追加を行います。<br/>
	 * 同時に複数のキューシートを登録することが可能です。<br/>
	 * <br/>
	 * 各ファイルパスに対して相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。<br/>
	 * 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。<br/>
	 * <br/>
	 * CPKファイルにパッキングされたACBファイルとAWBファイルからキューシートを追加する場合は、
	 * binder引数にCPKをバインドしたバインダを指定してください。<br/>
	 * なお、バインダ機能はADX2LEではご利用になれません。<br/>
	 */
	public static CriAtomCueSheet AddCueSheet(string name, string acbFile, string awbFile, CriFsBinder binder = null)
	{
	#if CRIWARE_FORCE_LOAD_ASYNC
		return CriAtom.AddCueSheetAsync(name, acbFile, awbFile, binder);
	#else
		CriAtomCueSheet cueSheet = CriAtom.instance.AddCueSheetInternal(name, acbFile, awbFile, binder);
		if (Application.isPlaying) {
			cueSheet.acb = CriAtom.instance.LoadAcbFile(binder, acbFile, awbFile);
		}
		return cueSheet;
	#endif
	}

	/**
	 * <summary>非同期でのキューシートの追加</summary>
	 * <param name="name">キューシート名</param>
	 * <param name="acbFile">ACBファイルパス</param>
	 * <param name="awbFile">AWBファイルパス</param>
	 * <param name="binder">バインダオブジェクト(オプション)</param>
	 * <param name="loadAwbOnMemory">AWBファイルをメモリ上にロードするか(オプション)</param>
	 * <returns>キューシートオブジェクト</returns>
	 * \par 説明:
	 * 引数に指定したファイルパス情報を元に、非同期でキューシートの追加を行います。<br/>
	 * 同時に複数のキューシートを登録することが可能です。<br/>
	 * <br/>
	 * 各ファイルパスに対して相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。<br/>
	 * 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。<br/>
	 * <br/>
	 * CPKファイルにパッキングされたACBファイルとAWBファイルからキューシートを追加する場合は、
	 * binder引数にCPKをバインドしたバインダを指定してください。<br/>
	 * なお、バインダ機能はADX2LEではご利用になれません。<br/>
	 * <br/>
	 * 戻り値のキューシートオブジェクトの CriAtomCueSheet::isLoading メンバが true を返す間はロード中です。<br/>
	 * 必ず false を返すのを確認してからキューの再生等を行うようにしてください。<br/>
	 * <br/>
	 * loadAwbOnMemory が false の場合、AWBファイルのヘッダ部分のみをメモリ上にロードしストリーム再生を行います。<br/>
	 * loadAwbOnMemory を true に設定すると、AWBファイル全体をメモリ上にロードするため実質メモリ再生になります。<br/>
	 * WebGL(Editor実行時)では内部都合上、 loadAwbOnMemory が強制的にtrueになります。<br/>
	 */
	public static CriAtomCueSheet AddCueSheetAsync(string name, string acbFile, string awbFile, CriFsBinder binder = null, bool loadAwbOnMemory = false)
	{
	#if UNITY_EDITOR && UNITY_WEBGL
		loadAwbOnMemory = true;
	#endif
		CriAtomCueSheet cueSheet = CriAtom.instance.AddCueSheetInternal(name, acbFile, awbFile, binder);
		if (Application.isPlaying) {
			CriAtom.instance.LoadAcbFileAsync(cueSheet, binder, acbFile, awbFile, loadAwbOnMemory);
		}
		return cueSheet;
	}

	/**
	 * <summary>キューシートの追加（メモリからの読み込み）</summary>
	 * <param name="name">キューシート名</param>
	 * <param name="acbData">ACBデータ</param>
	 * <param name="awbFile">AWBファイルパス</param>
	 * <param name="awbBinder">AWB用バインダオブジェクト(オプション)</param>
	 * <returns>キューシートオブジェクト</returns>
	 * \par 説明:
	 * 引数に指定したデータとファイルパス情報からキューシートの追加を行います。<br/>
	 * 同時に複数のキューシートを登録することが可能です。<br/>
	 * <br/>
	 * ファイルパスに対して相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。<br/>
	 * 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。<br/>
	 * <br/>
	 * CPKファイルにパッキングされたAWBファイルからキューシートを追加する場合は、
	 * awbBinder引数にCPKをバインドしたバインダを指定してください。<br/>
	 * なお、バインダ機能はADX2LEではご利用になれません。<br/>
	 */
	public static CriAtomCueSheet AddCueSheet(string name, byte[] acbData, string awbFile, CriFsBinder awbBinder = null)
	{
	#if CRIWARE_FORCE_LOAD_ASYNC
		return CriAtom.AddCueSheetAsync(name, acbData, awbFile, awbBinder);
	#else
		CriAtomCueSheet cueSheet = CriAtom.instance.AddCueSheetInternal(name, "", awbFile, awbBinder);
		if (Application.isPlaying) {
			cueSheet.acb = CriAtom.instance.LoadAcbData(acbData, awbBinder, awbFile);
		}
		return cueSheet;
	#endif
	}

	/**
	 * <summary>非同期でのキューシートの追加（メモリからの読み込み）</summary>
	 * <param name="name">キューシート名</param>
	 * <param name="acbData">ACBデータ</param>
	 * <param name="awbFile">AWBファイルパス</param>
	 * <param name="awbBinder">AWB用バインダオブジェクト(オプション)</param>
 	 * <param name="loadAwbOnMemory">AWBファイルをメモリ上にロードするか(オプション)</param>
	 * <returns>キューシートオブジェクト</returns>
	 * \par 説明:
	 * 引数に指定したデータとファイルパス情報からキューシートの追加を行います。<br/>
	 * 同時に複数のキューシートを登録することが可能です。<br/>
	 * <br/>
	 * ファイルパスに対して相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。<br/>
	 * 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。<br/>
	 * <br/>
	 * CPKファイルにパッキングされたAWBファイルからキューシートを追加する場合は、
	 * awbBinder引数にCPKをバインドしたバインダを指定してください。<br/>
	 * なお、バインダ機能はADX2LEではご利用になれません。<br/>
	 * <br/>
	 * 戻り値のキューシートオブジェクトの CriAtomCueSheet::isLoading メンバが true を返す間はロード中です。<br/>
	 * 必ず false を返すのを確認してからキューの再生等を行うようにしてください。<br/>
	 * <br/>
	 * loadAwbOnMemory が false の場合、AWBファイルのヘッダ部分のみをメモリ上にロードしストリーム再生を行います。<br/>
	 * loadAwbOnMemory を true に設定すると、AWBファイル全体をメモリ上にロードするため実質メモリ再生になります。<br/>
	 * WebGL(Editor実行時)では内部都合上、 loadAwbOnMemory が強制的にtrueになります。<br/>
	 */
	public static CriAtomCueSheet AddCueSheetAsync(string name, byte[] acbData, string awbFile, CriFsBinder awbBinder = null, bool loadAwbOnMemory = false)
	{
	#if UNITY_EDITOR && UNITY_WEBGL
		loadAwbOnMemory = true;
	#endif
		CriAtomCueSheet cueSheet = CriAtom.instance.AddCueSheetInternal(name, "", awbFile, awbBinder);
		if (Application.isPlaying) {
			CriAtom.instance.LoadAcbDataAsync(cueSheet, acbData, awbBinder, awbFile, loadAwbOnMemory);
		}
		return cueSheet;
	}

	/**
	 * <summary>キューシートの削除</summary>
	 * <param name="name">キューシート名</param>
	 * \par 説明:
	 * 追加済みのキューシートを削除します。<br/>
	 */
	public static void RemoveCueSheet(string name)
	{
		if (CriAtom.instance == null) {
			return;
		}
		CriAtom.instance.RemoveCueSheetInternal(name);
	}

	/**
	 * <summary>キューシートのロード完了チェック</summary>
	 * \par 説明:
	 * 全てのキューシートのロード完了をチェックします。<br/>
	 */
	public static bool CueSheetsAreLoading {
		get {
			if (CriAtom.instance == null) {
				return false;
			}
			foreach (var cueSheet in CriAtom.instance.cueSheets) {
				if (cueSheet.IsLoading) {
					return true;
				}
			}
			return false;
		}
	}

	/**
	 * <summary>ACBオブジェクトの取得</summary>
	 * <param name="cueSheetName">キューシート名</param>
	 * <returns>ACBオブジェクト</returns>
	 * \par 説明:
	 * 指定したキューシートに対応するACBオブジェクトを取得します。<br/>
	 */
	public static CriAtomExAcb GetAcb(string cueSheetName)
	{
		foreach (var cueSheet in CriAtom.instance.cueSheets) {
			if (cueSheetName == cueSheet.name) {
				return cueSheet.acb;
			}
		}
		Debug.LogWarning(cueSheetName + " is not loaded.");
		return null;
	}

	/**
	 * <summary>カテゴリ名指定でカテゴリボリュームを設定します。</summary>
	 * <param name="name">カテゴリ名</param>
	 * <param name="volume">ボリューム</param>
	 */
	public static void SetCategoryVolume(string name, float volume)
	{
		CriAtomExCategory.SetVolume(name, volume);
	}

	/**
	 * <summary>カテゴリID指定でカテゴリボリュームを設定します。</summary>
	 * <param name="id">カテゴリID</param>
	 * <param name="volume">ボリューム</param>
	 */
	public static void SetCategoryVolume(int id, float volume)
	{
		CriAtomExCategory.SetVolume(id, volume);
	}

	/**
	 * <summary>カテゴリ名指定でカテゴリボリュームを取得します。</summary>
	 * <param name="name">カテゴリ名</param>
	 * <returns>ボリューム</returns>
	 */
	public static float GetCategoryVolume(string name)
	{
		return CriAtomExCategory.GetVolume(name);
	}
	/**
	 * <summary>カテゴリID指定でカテゴリボリュームを取得します。</summary>
	 * <param name="id">カテゴリID</param>
	 * <returns>ボリューム</returns>
	 */
	public static float GetCategoryVolume(int id)
	{
		return CriAtomExCategory.GetVolume(id);
	}

	/**
	 * <summary>バス情報取得を有効にします。</summary>
	 * <param name="busName">DSPバス名</param>
	 * <param name="sw">true: 取得を有効にする。 false: 取得を無効にする</param>
	 */
	public static void SetBusAnalyzer(string busName, bool sw)
	{
	#if !UNITY_EDITOR && UNITY_PSP2
		// unsupported
	#else
		if (sw) {
			CriAtomExAsr.AttachBusAnalyzer(busName, 50, 1000);
		} else {
			CriAtomExAsr.DetachBusAnalyzer(busName);
		}
	#endif
	}

	/**
	 * <summary>全てのバス情報取得を有効にします。</summary>
	 * <param name="sw">true: 取得を有効にする。 false: 取得を無効にする</param>
	 */
	public static void SetBusAnalyzer(bool sw)
	{
	#if !UNITY_EDITOR && UNITY_PSP2
		// unsupported
	#else
		if (sw) {
			CriAtomExAsr.AttachBusAnalyzer(50, 1000);
		} else {
			CriAtomExAsr.DetachBusAnalyzer();
		}
	#endif
	}

	/**
	 * <summary>バス情報を取得します。</summary>
	 * <param name="busName">DSPバス名</param>
	 * <returns>DSPバス情報</returns>
	 */
	public static CriAtomExAsr.BusAnalyzerInfo GetBusAnalyzerInfo(string busName)
	{
		CriAtomExAsr.BusAnalyzerInfo info;
	#if !UNITY_EDITOR && UNITY_PSP2
		info = new CriAtomExAsr.BusAnalyzerInfo(null);
	#else
		CriAtomExAsr.GetBusAnalyzerInfo(busName, out info);
	#endif
		return info;
	}

	[System.Obsolete("Use CriAtom.GetBusAnalyzerInfo(string busName)")]
	public static CriAtomExAsr.BusAnalyzerInfo GetBusAnalyzerInfo(int busId)
	{
		CriAtomExAsr.BusAnalyzerInfo info;
	#if !UNITY_EDITOR && UNITY_PSP2
		info = new CriAtomExAsr.BusAnalyzerInfo(null);
	#else
		CriAtomExAsr.GetBusAnalyzerInfo(busId, out info);
	#endif
		return info;
	}

	#endregion // Functions

	/* @cond DOXYGEN_IGNORE */
	#region Functions for internal use

	public void Setup()
	{
		if (CriAtom.instance != null && CriAtom.instance != this) {
			var obj = CriAtom.instance.gameObject;
			CriAtom.instance.Shutdown();
			CriAtomEx.UnregisterAcf();
			GameObject.Destroy(obj);
		}

		CriAtom.instance = this;

		CriAtomPlugin.InitializeLibrary();

		if (!String.IsNullOrEmpty(this.acfFile)) {
			string acfPath = Path.Combine(CriWare.streamingAssetsPath, this.acfFile);
			CriAtomEx.RegisterAcf(null, acfPath);
		}

		if (!String.IsNullOrEmpty(dspBusSetting)) {
			AttachDspBusSetting(dspBusSetting);
		}

		foreach (var cueSheet in this.cueSheets) {
			cueSheet.acb = this.LoadAcbFile(null, cueSheet.acbFile, cueSheet.awbFile);
		}

		if (this.dontDestroyOnLoad) {
			GameObject.DontDestroyOnLoad(this.gameObject);
		}
	}

	public void Shutdown()
	{
		foreach (var cueSheet in this.cueSheets) {
			if (cueSheet.acb != null) {
				cueSheet.acb.Dispose();
				cueSheet.acb = null;
			}
		}
		CriAtomPlugin.FinalizeLibrary();
		if (CriAtom.instance == this) {
			CriAtom.instance = null;
		}
	}

	protected override void Awake()
	{
		base.Awake();		
		if (CriAtom.instance != null && CriAtom.instance != this) {
			if (CriAtom.instance.acfFile != this.acfFile) {
				var obj = CriAtom.instance.gameObject;
				CriAtom.instance.Shutdown();
				CriAtomEx.UnregisterAcf();
				GameObject.Destroy(obj);
				return;
			}
			if (CriAtom.instance.dspBusSetting != this.dspBusSetting) {
				CriAtom.AttachDspBusSetting(this.dspBusSetting);
			}
			CriAtom.instance.MargeCueSheet(this.cueSheets, this.dontRemoveExistsCueSheet);
			GameObject.Destroy(this.gameObject);
		}
	}

	private void OnEnable()
	{
	#if UNITY_EDITOR
		if (CriAtomPlugin.previewCallback != null) {
			CriAtomPlugin.previewCallback();
		}
	#endif
		if (CriAtom.instance != null) {
			return;
		}

	#if CRIWARE_FORCE_LOAD_ASYNC
		this.SetupAsync();
	#else
		this.Setup();
	#endif
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (this != CriAtom.instance) {
			return;
		}
		this.Shutdown();
	}

	public override void CriInternalUpdate()
	{
		CriAtomPlugin.CRIWARE34D5EE7B();
		CriAtomPlugin.CRIWARE73C35540();
	}

	public override void CriInternalLateUpdate() { }

	public CriAtomCueSheet GetCueSheetInternal(string name)
	{
		for (int i = 0; i < this.cueSheets.Length; i++) {
			CriAtomCueSheet cueSheet = this.cueSheets[i];
			if (cueSheet.name == name) {
				return cueSheet;
			}
		}
		return null;
	}

	public CriAtomCueSheet AddCueSheetInternal(string name, string acbFile, string awbFile, CriFsBinder binder)
	{
		var cueSheets = new CriAtomCueSheet[this.cueSheets.Length + 1];
		this.cueSheets.CopyTo(cueSheets, 0);
		this.cueSheets = cueSheets;

		var cueSheet = new CriAtomCueSheet();
		this.cueSheets[this.cueSheets.Length - 1] = cueSheet;
		if (String.IsNullOrEmpty(name)) {
			cueSheet.name = Path.GetFileNameWithoutExtension(acbFile);
		} else {
			cueSheet.name = name;
		}
		cueSheet.acbFile = acbFile;
		cueSheet.awbFile = awbFile;
		return cueSheet;
	}

	public void RemoveCueSheetInternal(string name)
	{
		int index = -1;
		for (int i = 0; i < this.cueSheets.Length; i++) {
			if (name == this.cueSheets[i].name) {
				index = i;
			}
		}
		if (index < 0) {
			return;
		}

		var cueSheet = this.cueSheets[index];
		if (cueSheet.acb != null) {
			cueSheet.acb.Dispose();
			cueSheet.acb = null;
		}

		var cueSheets = new CriAtomCueSheet[this.cueSheets.Length - 1];
		Array.Copy(this.cueSheets, 0, cueSheets, 0, index);
		Array.Copy(this.cueSheets, index + 1, cueSheets, index, this.cueSheets.Length - index - 1);
		this.cueSheets = cueSheets;
	}

	public bool dontRemoveExistsCueSheet = false;

	/*
	 * newDontRemoveExistsCueSheet == false の場合、古いキューシートを登録解除して、新しいキューシートの登録を行う。
	 * ただし同じキューシートの再登録は回避する
	 */
	private void MargeCueSheet(CriAtomCueSheet[] newCueSheets, bool newDontRemoveExistsCueSheet)
	{
		if (!newDontRemoveExistsCueSheet) {
			for (int i = 0; i < this.cueSheets.Length; ) {
				int index = Array.FindIndex(newCueSheets, sheet => sheet.name == this.cueSheets[i].name);
				if (index < 0) {
					CriAtom.RemoveCueSheet(this.cueSheets[i].name);
				} else {
					i++;
				}
			}
		}

		foreach (var sheet1 in newCueSheets) {
			var sheet2 = this.GetCueSheetInternal(sheet1.name);
			if (sheet2 == null) {
				CriAtom.AddCueSheet(null, sheet1.acbFile, sheet1.awbFile, null);
			}
		}
	}

	private CriAtomExAcb LoadAcbFile(CriFsBinder binder, string acbFile, string awbFile)
	{
		if (String.IsNullOrEmpty(acbFile)) {
			return null;
		}

		string acbPath = acbFile;
		if ((binder == null) && CriWare.IsStreamingAssetsPath(acbPath)) {
			acbPath = Path.Combine(CriWare.streamingAssetsPath, acbPath);
		}

		string awbPath = awbFile;
		if (!String.IsNullOrEmpty(awbPath)) {
			if ((binder == null) && CriWare.IsStreamingAssetsPath(awbPath)) {
				awbPath = Path.Combine(CriWare.streamingAssetsPath, awbPath);
			}
		}

		return CriAtomExAcb.LoadAcbFile(binder, acbPath, awbPath);
	}

	private CriAtomExAcb LoadAcbData(byte[] acbData, CriFsBinder binder, string awbFile)
	{
		if (acbData == null) {
			return null;
		}

		string awbPath = awbFile;
		if (!String.IsNullOrEmpty(awbPath)) {
			if ((binder == null) && CriWare.IsStreamingAssetsPath(awbPath)) {
				awbPath = Path.Combine(CriWare.streamingAssetsPath, awbPath);
			}
		}

		return CriAtomExAcb.LoadAcbData(acbData, binder, awbPath);
	}

#if CRIWARE_FORCE_LOAD_ASYNC
	private void SetupAsync()
	{
		CriAtom.instance = this;

		CriAtomPlugin.InitializeLibrary();

		if (this.dontDestroyOnLoad) {
			GameObject.DontDestroyOnLoad(this.gameObject);
		}

		if (!String.IsNullOrEmpty(this.acfFile)) {
			this.acfIsLoading = true;
			StartCoroutine(RegisterAcfAsync(this.acfFile, this.dspBusSetting));
		}

		foreach (var cueSheet in this.cueSheets) {
			#if UNITY_EDITOR
			bool loadAwbOnMemory = true;
			#else
			bool loadAwbOnMemory = false;
			#endif
			StartCoroutine(LoadAcbFileCoroutine(cueSheet, null, cueSheet.acbFile, cueSheet.awbFile, loadAwbOnMemory));
		}
	}

	private IEnumerator RegisterAcfAsync(string acfFile, string dspBusSetting)
	{
	#if UNITY_EDITOR
		string acfPath = "file://" + CriWare.streamingAssetsPath + "/" + acfFile;
	#else
		string acfPath = CriWare.streamingAssetsPath + "/" + acfFile;
	#endif
		using (var req = new WWW(acfPath)) {
			yield return req;
			this.acfData = req.bytes;
		}
		CriAtomEx.RegisterAcf(this.acfData);

		if (!String.IsNullOrEmpty(dspBusSetting)) {
			AttachDspBusSetting(dspBusSetting);
		}
		this.acfIsLoading = false;
	}
#endif


	private void LoadAcbFileAsync(CriAtomCueSheet cueSheet, CriFsBinder binder, string acbFile, string awbFile, bool loadAwbOnMemory)
	{
		if (String.IsNullOrEmpty(acbFile)) {
			return;
		}

		StartCoroutine(LoadAcbFileCoroutine(cueSheet, binder, acbFile, awbFile, loadAwbOnMemory));
	}

	private IEnumerator LoadAcbFileCoroutine(CriAtomCueSheet cueSheet, CriFsBinder binder, string acbPath, string awbPath, bool loadAwbOnMemory)
	{
		cueSheet.loaderStatus = CriAtomExAcbLoader.Status.Loading;

		if ((binder == null) && CriWare.IsStreamingAssetsPath(acbPath)) {
			acbPath = Path.Combine(CriWare.streamingAssetsPath, acbPath);
		}

		if (!String.IsNullOrEmpty(awbPath)) {
			if ((binder == null) && CriWare.IsStreamingAssetsPath(awbPath)) {
				awbPath = Path.Combine(CriWare.streamingAssetsPath, awbPath);
			}
		}

		while (this.acfIsLoading) {
			yield return null;
		}

		using (var asyncLoader = CriAtomExAcbLoader.LoadAcbFileAsync(binder, acbPath, awbPath, loadAwbOnMemory)) {
			if (asyncLoader == null) {
				cueSheet.loaderStatus = CriAtomExAcbLoader.Status.Error;
				yield break;
			}

			while (true) {
				var status = asyncLoader.GetStatus();
				cueSheet.loaderStatus = status;
				if (status == CriAtomExAcbLoader.Status.Complete) {
					cueSheet.acb = asyncLoader.MoveAcb();
					break;
				} else if (status == CriAtomExAcbLoader.Status.Error) {
					break;
				}
				yield return null;
			}
		}
	}

	private void LoadAcbDataAsync(CriAtomCueSheet cueSheet, byte[] acbData, CriFsBinder awbBinder, string awbFile, bool loadAwbOnMemory)
	{
		StartCoroutine(LoadAcbDataCoroutine(cueSheet, acbData, awbBinder, awbFile, loadAwbOnMemory));
	}

	private IEnumerator LoadAcbDataCoroutine(CriAtomCueSheet cueSheet, byte[] acbData, CriFsBinder awbBinder, string awbPath, bool loadAwbOnMemory)
	{
		cueSheet.loaderStatus = CriAtomExAcbLoader.Status.Loading;

		if (!String.IsNullOrEmpty(awbPath)) {
			if ((awbBinder == null) && CriWare.IsStreamingAssetsPath(awbPath)) {
				awbPath = Path.Combine(CriWare.streamingAssetsPath, awbPath);
			}
		}

		while (this.acfIsLoading) {
			yield return null;
		}

		using (var asyncLoader = CriAtomExAcbLoader.LoadAcbDataAsync(acbData, awbBinder, awbPath, loadAwbOnMemory)) {
			if (asyncLoader == null) {
				cueSheet.loaderStatus = CriAtomExAcbLoader.Status.Error;
				yield break;
			}

			while (true) {
				var status = asyncLoader.GetStatus();
				cueSheet.loaderStatus = status;
				if (status == CriAtomExAcbLoader.Status.Complete) {
					cueSheet.acb = asyncLoader.MoveAcb();
					break;
				} else if (status == CriAtomExAcbLoader.Status.Error) {
					break;
				}
				yield return null;
			}
		}
	}

#if CRIWARE_SUPPORT_NATIVE_CALLBACK
	[AOT.MonoPInvokeCallback(typeof(CriAtomExSequencer.EventCbFunc))]
	public static void SequenceEventCallbackFromNative(string eventString)
	{
		/* 本関数はアプリケーションメインスレッドの CriAtom.Update から呼び出される */
		if (eventUserCbFunc != null) {
			eventUserCbFunc(eventString);
		}
	}

	[AOT.MonoPInvokeCallback(typeof(CriAtomExBeatSync.CbFunc))]
	public static void BeatSyncCallbackFromNative(ref CriAtomExBeatSync.Info info)
	{
		/* 本関数はアプリケーションメインスレッドの CriAtom.Update から呼び出される */
		if (beatsyncUserCbFunc != null) {
			beatsyncUserCbFunc(ref info);
		}
	}
#endif

	/* プラグイン内部用API */
	public static void SetEventCallback(CriAtomExSequencer.EventCbFunc func, string separator)
	{
#if CRIWARE_SUPPORT_NATIVE_CALLBACK
		/* ネイティブプラグインに関数ポインタを登録 */
		IntPtr ptr = IntPtr.Zero;
		eventUserCbFunc = func;
		if (func != null) {
			CriAtomExSequencer.EventCbFunc delegateObject;
			delegateObject = new CriAtomExSequencer.EventCbFunc(CriAtom.SequenceEventCallbackFromNative);
			ptr = Marshal.GetFunctionPointerForDelegate(delegateObject);
		}
		CriAtomPlugin.CRIWARE54141BCD(ptr, separator);
#else
		Debug.LogError("[CRIWARE] Event callback is not supported for this scripting backend.");
#endif
	}

	public static void SetBeatSyncCallback(CriAtomExBeatSync.CbFunc func)
	{
#if CRIWARE_SUPPORT_NATIVE_CALLBACK
		/* ネイティブプラグインに関数ポインタを登録 */
		IntPtr ptr = IntPtr.Zero;
		beatsyncUserCbFunc = func;
		if (func != null) {
			CriAtomExBeatSync.CbFunc delegateObject;
			delegateObject = new CriAtomExBeatSync.CbFunc (CriAtom.BeatSyncCallbackFromNative);
			ptr = Marshal.GetFunctionPointerForDelegate(delegateObject);
		}
		CriAtomPlugin.CRIWARE31B98F5C(ptr);
#else
		Debug.LogError("[CRIWARE] Beat sync callback is not supported for this scripting backend.");
#endif
	}

	#endregion
	/* @endcond */

}

/// @}
/* end of file */
