/****************************************************************************
 *
 * Copyright (c) 2012 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;


/*JP
 * \brief CRI File System初期化パラメータ
 */
[System.Serializable]
public class CriFsConfig {
	/*JP デバイス性能読み込み速度のデフォルト値(bps) */
	public const int defaultAndroidDeviceReadBitrate = 50000000;

	/*JP ローダー数 */
	public int numberOfLoaders    = 16;
	/*JP バインダ数 */
	public int numberOfBinders    = 8;
	/*JP インストーラ数 */
	public int numberOfInstallers = 2;
	/*JP インストールバッファのサイズ */
	public int installBufferSize  = CriFsPlugin.defaultInstallBufferSize / 1024;
	/*JP パスの最大長 */
	public int maxPath            = 256;
	/*JP ユーザーエージェント文字列 */
	public string userAgentString = "";
	/*JP ファイルディスクリプタの節約モードフラグ */
	public bool minimizeFileDescriptorUsage = false;
	/*JP CPKファイルのCRCチェックを行うかどうか */
	public bool enableCrcCheck = false;
	/*JP [Android] デバイス性能読み込み速度(bps) */
	public int androidDeviceReadBitrate = defaultAndroidDeviceReadBitrate;

}

/*JP
 * \brief CRI Atom初期化パラメータ
 */
[System.Serializable]
public class CriAtomConfig {
	/*JP ACFファイル名
	 *   \attention ACFファイルをStreamingAssetsフォルダに配置しておく必要あり。 */
	public string acfFileName = "";
	
	/*JP 標準ボイスプール作成パラメータ */
	[System.Serializable]
	public class StandardVoicePoolConfig {
		public int memoryVoices    = 16;
		public int streamingVoices = 8;
	}
	
	/*JP HCA-MXボイスプール作成パラメータ */
	[System.Serializable]
	public class HcaMxVoicePoolConfig {
		public int memoryVoices    = 0;
		public int streamingVoices = 0;
	}
	
	/*JP 最大バーチャルボイス数 */
	public int maxVirtualVoices = 32;
	/*JP 最大ボイスリミットグループ数 */
	public int maxVoiceLimitGroups = 32;
	/*JP 最大カテゴリ数 */
	public int maxCategories = 32;
	/*JP 1フレーム当たりの最大シーケンスイベント数 */
	public int maxSequenceEventsPerFrame = 2;
	/*JP 1フレーム当たりの最大ビート同期コールバック数 */
	public int maxBeatSyncCallbacksPerFrame = 1;
	/*JP 標準ボイスプール作成パラメータ */
	public StandardVoicePoolConfig standardVoicePoolConfig = new StandardVoicePoolConfig();
	/*JP HCA-MXボイスプール作成パラメータ */
	public HcaMxVoicePoolConfig hcaMxVoicePoolConfig = new HcaMxVoicePoolConfig();
	/*JP 出力サンプリングレート */
	public int outputSamplingRate = 0;
	/*JP インゲームプレビューを使用するかどうか */
	public bool usesInGamePreview = false;
	/*JP サーバ周波数 */
	public float serverFrequency  = 60.0f;
	/*JP ASR出力チャンネル数 */
	public int asrOutputChannels  = 0;
	/*JP 乱数種に時間（System.DateTime.Now.Ticks）を使用するかどうか */
	public bool useRandomSeedWithTime = false;
	/*JP 再生単位でのカテゴリ参照数 */
	public int categoriesPerPlayback = 4;
	/*JP 最大バス数 */
	public int maxBuses = 8;
	/*JP 最大パラメータブロック数 */
	public int maxParameterBlocks = 1024;
	/*JP VR サウンド出力モードを使用するか否か */
	public bool vrMode = false;

	/*JP [PC] 出力バッファリング時間 */
	public int pcBufferingTime = 0;
	
	/*JP [Linux] 出力タイプ */
	public enum LinuxOutput : int {
		Default = 0, /*JP Outputs to default audio system (PulseAudio). */
		PulseAudio = 1, /*JP Outputs to PulseAudio system.*/
		ALSA = 2 /*JP Outputs to Advanced Linux Sound Achitecture system.*/
	};
	/*JP [Linux] 出力タイプ指定 */
	public LinuxOutput linuxOutput = LinuxOutput.Default;

	/*JP [iOS] 出力バッファリング時間(ミリ秒)*/
	public int  iosBufferingTime     = 50;
	/*JP [iOS] iPodの再生を上書きするか？ */
	public bool iosOverrideIPodMusic = false;

	/*JP [Android] 出力バッファリング時間 */
	public int androidBufferingTime      = 133;
	/*JP [Android] 再生開始バッファリング時間 */
	public int androidStartBufferingTime = 100;

	/*JP [Android] 低遅延再生用ボイスプール作成パラメータ */
	[System.Serializable]
	public class AndroidLowLatencyStandardVoicePoolConfig {
		public int memoryVoices    = 0;
		public int streamingVoices = 0;
	}
	/*JP [Android] 低遅延再生用ボイスプール作成パラメータ */
	public AndroidLowLatencyStandardVoicePoolConfig androidLowLatencyStandardVoicePoolConfig = new AndroidLowLatencyStandardVoicePoolConfig();
	/*JP [Android] Android OS の Fast Mixer を使用して、音声再生時の発音遅延を短縮するかどうか。ASR/NSR の発音遅延や、遅延推測機能の結果に影響する */
	public bool androidUsesAndroidFastMixer = true;
	/*JP [Android] 非低遅延再生指定時のCriAtomSourceで、強制的にASRによる再生を行うか */
	public bool androidForceToUseAsrForDefaultPlayback = true;

	/*JP [PSVita] Mana用ボイスプール作成パラメータ */
	[System.Serializable]
	public class VitaManaVoicePoolConfig {
		public int numberOfManaDecoders = 8;
	}
	/*JP [PSVita] Mana用ボイスプール作成パラメータ */
	public VitaManaVoicePoolConfig vitaManaVoicePoolConfig = new VitaManaVoicePoolConfig();

	/*JP [PSVita] ATRAC9用ボイスプール作成パラメータ */
	[System.Serializable]
	public class VitaAtrac9VoicePoolConfig {
		public int memoryVoices    = 0;
		public int streamingVoices = 0;
	}
	/*JP [PSVita] ATRAC9用ボイスプール作成パラメータ */
	public VitaAtrac9VoicePoolConfig vitaAtrac9VoicePoolConfig = new VitaAtrac9VoicePoolConfig();

	/*JP [PS4] ATRAC9用ボイスプール作成パラメータ */
	[System.Serializable]
	public class Ps4Atrac9VoicePoolConfig {
		public int memoryVoices    = 0;
		public int streamingVoices = 0;
	}
	/*JP [PS4] ATRAC9用ボイスプール作成パラメータ */
	public Ps4Atrac9VoicePoolConfig ps4Atrac9VoicePoolConfig = new Ps4Atrac9VoicePoolConfig();

	/*JP [PS4] Audio3D用ボイスプール作成パラメータ */
	[System.Serializable]
	public class Ps4Audio3dConfig {
		/*JP [PS4] Audio3D機能を使用するかどうか */
		public bool useAudio3D = false;

		/*JP [PS4] Audio3D用ボイスプール作成パラメータ */
		[System.Serializable]
		public class VoicePoolConfig {
			public int memoryVoices    = 0;
			public int streamingVoices = 0;
		}
		public VoicePoolConfig voicePoolConfig = new VoicePoolConfig();

	}
	public Ps4Audio3dConfig ps4Audio3dConfig = new Ps4Audio3dConfig();

	/*JP [WebGL] WebAudioボイスプール作成パラメータ */
	[System.Serializable]
	public class WebGLWebAudioVoicePoolConfig {
		public int voices    = 16;
	}
	public WebGLWebAudioVoicePoolConfig webglWebAudioVoicePoolConfig = new WebGLWebAudioVoicePoolConfig();
}

/*JP
 * \brief CRI Mana初期化パラメータ
 */
[System.Serializable]
public class CriManaConfig {
	/*JP デコーダー数 */
	public int  numberOfDecoders   = 8;
	/*JP 連結再生エントリー数 */
	public int  numberOfMaxEntries = 4;
	/*JP GL.IssuePluginEventを用いたマルチスレッドでのテクスチャ描画処理を有効にするかどうか */
	public readonly bool graphicsMultiThreaded = true; // always true.

	/*JP [PC] H.264 ムービ再生の設定 */
	[System.Serializable]
	public class PCH264PlaybackConfig {
		public bool useH264Playback = true;
	}
	public PCH264PlaybackConfig pcH264PlaybackConfig = new PCH264PlaybackConfig();

	/*JP [PSVita] H.264 ムービ再生の設定 */
	[System.Serializable]
	public class VitaH264PlaybackConfig {
		/*JP H.264 H.264 ムービ再生の機能を使用するか */
		public bool useH264Playback = false;
		/*JP 再生する H.264 ムービの幅 (最大) */
		public int maxWidth = 960;
		/*JP 再生する H.264 ムービの高さ (最大) */
		public int maxHeight = 544;
		public bool getMemoryFromTexture = false;
	}
	public VitaH264PlaybackConfig vitaH264PlaybackConfig = new VitaH264PlaybackConfig();

	/*JP [WebGL] WebGLの設定 */
	[System.Serializable]
	public class WebGLConfig
	{
		/*JP WebWorker用JavaScriptのパス */
		public string webworkerPath = "StreamingAssets";
		public int heapSize = 32;
	}
	public WebGLConfig webglConfig = new WebGLConfig();
}

/*JP
 * \brief CRI Ware Decrypter初期化パラメータ
 */
[System.Serializable]
public class CriWareDecrypterConfig {
	/*JP 暗号化キー */
	public string key = "";
	/*JP 復号化認証ファイルのパス */
	public string authenticationFile = "";
	/*JP CRI Atomの復号化を有効にするかどうか */
	public bool enableAtomDecryption = true;
	/*JP CRI Manaの復号化を有効にするかどうか */
	public bool enableManaDecryption = true;
}

/// \addtogroup CRIWARE_UNITY_COMPONENT
/// @{

/*JP
 * \brief CRIWARE初期化コンポーネント
 * \par 説明:
 * CRIWAREライブラリの初期化を行うためのコンポーネントです。<br>
 */
[AddComponentMenu("CRIWARE/Library Initializer")]
public class CriWareInitializer : CriMonoBehaviour {
	
	/*JP CRI File Systemライブラリを初期化するかどうか */
	public bool initializesFileSystem = true;
	
	/*JP CRI File Systemライブラリ初期化設定 */
	public CriFsConfig fileSystemConfig = new CriFsConfig();
	
	/*JP CRI Atomライブラリを初期化するかどうか */
	public bool initializesAtom = true;
	
	/*JP CRI Atomライブラリ初期化設定 */
	public CriAtomConfig atomConfig = new CriAtomConfig();
	
	/*JP CRI Manaライブラリを初期化するかどうか */
	public bool initializesMana = true;
	
	/*JP CRI Manaライブラリ初期化設定 */
	public CriManaConfig manaConfig = new CriManaConfig();

	/*JP CRI Ware Decrypterを使用するかどうか */
	public bool useDecrypter =false;
	
	/*JP CRI Ware Decrypter設定 */
	public CriWareDecrypterConfig decrypterConfig = new CriWareDecrypterConfig();

	/*JP Awake時にライブラリを初期化するかどうか */
	public bool dontInitializeOnAwake = false;

	/*JP シーンチェンジ時にライブラリを終了するかどうか */
	public bool dontDestroyOnLoad = false;

	/* オブジェクト作成時の処理 */
	protected override void Awake() {
		/* 現在のランタイムのバージョンが正しいかチェック */
		CriWare.CheckBinaryVersionCompatibility();

		if (dontInitializeOnAwake) {
			/* フラグが立っていた場合はAwakeでは初期化を行わない */
			return;
		}

		base.Awake();
		/* プラグインの初期化 */
		this.Initialize();
	}
	
	/* Execution Order の設定を確実に有効にするために OnEnable をオーバーライド */
	void OnEnable() {
	}
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	public override void CriInternalUpdate() { }

	public override void CriInternalLateUpdate() { }

	/**
	 * <summary>プラグインの初期化（手動初期化用）</summary> 
	 * \par 説明:
	 * プラグインの初期化を行います。<br/>
	 * デフォルトでは本関数はAwake関数内で自動的に呼び出されるので、アプリケーションが直接呼び出す必要はありません。<br/>
	 * <br/>
	 * 初期化パラメタをスクリプトから動的に変更して初期化を行いたい場合、本関数を使用してください。<br/>
	 * \par 注意：
	 * 本関数を使用する場合は、 自動初期化を無効にするため、 ::CriWareInitializer::dontInitializeOnAwake プロパティをインスペクタ上でチェックしてください。<br/>
	 * また、本関数を呼び出すタイミングは全てのプラグインAPIよりも前に呼び出す必要があります。Script Execution Orderが高いスクリプト上で行ってください。
	 * 
	 */
	public void Initialize() {
		/* 初期化カウンタの更新 */
		initializationCount++;
		if (initializationCount != 1) {
			/* CriWareInitializer自身による多重初期化は許可しない */
			GameObject.Destroy(this);
			return;
		}

		/* 非実行時にライブラリ機能を使用していた場合は一度終了処理を行う */
		if ((CriFsPlugin.IsLibraryInitialized() == true && CriAtomPlugin.IsLibraryInitialized() == true && CriManaPlugin.IsLibraryInitialized() == true) ||
			(CriFsPlugin.IsLibraryInitialized() == true && CriAtomPlugin.IsLibraryInitialized() == true && CriManaPlugin.IsLibraryInitialized() == false) ||
			(CriFsPlugin.IsLibraryInitialized() == true && CriAtomPlugin.IsLibraryInitialized() == false && CriManaPlugin.IsLibraryInitialized() == false)) {
#if UNITY_EDITOR || (!UNITY_PS3)
			/* CRI Manaライブラリの終了 */
			if (initializesMana) {
				CriManaPlugin.FinalizeLibrary();
			}
#endif

			/* CRI Atomライブラリの終了 */
			if (initializesAtom) {
				/* EstimatorがStop状態になるまでFinalize */
				while (CriAtomExLatencyEstimator.GetCurrentInfo().status != CriAtomExLatencyEstimator.Status.Stop) {
					CriAtomExLatencyEstimator.FinalizeModule();
				}

				/* 終了処理の実行 */
				CriAtomPlugin.FinalizeLibrary();
			}

			/* CRI File Systemライブラリの終了 */
			if (initializesFileSystem) {
				CriFsPlugin.FinalizeLibrary();
			}
		}

		/* CRI File Systemライブラリの初期化 */
		if (initializesFileSystem) {
			InitializeFileSystem(fileSystemConfig);
		}

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
		if (initializesMana) {
			/* Atom の初期化前に設定する必要がある */
			CriManaPlugin.SetConfigAdditonalParameters_PC(manaConfig.pcH264PlaybackConfig.useH264Playback);
		}
#endif

		/* CRI Atomライブラリの初期化 */
		if (initializesAtom) {
#if !UNITY_EDITOR && UNITY_PSP2
			/* Mana と関連する初期化パラメータを設定 */
			atomConfig.vitaManaVoicePoolConfig.numberOfManaDecoders = initializesMana ? manaConfig.numberOfDecoders : 0;
#endif
			InitializeAtom(atomConfig);
		}

#if UNITY_EDITOR || (!UNITY_PS3)
		/* CRI Manaライブラリの初期化 */
		if (initializesMana) {
			InitializeMana(manaConfig);
		}
#endif

		/*JP< CRI Ware Decrypterの設定 */
		if (useDecrypter) {
			CriWareDecrypter.Initialize(decrypterConfig);
		} else {
			CriWareDecrypter.Initialize("0", "", false, false);
		}

		/* シーンチェンジ後もオブジェクトを維持するかどうかの設定 */
		if (dontDestroyOnLoad) {
			DontDestroyOnLoad(transform.gameObject);
			DontDestroyOnLoad(CriWare.managerObject);
		}
	}

	/**
	 * <summary>プラグインの終了（手動終了用）</summary> 
	 * \par 説明:
	 * プラグインを終了します。<br/>
	 * デフォルトでは本関数はOnDestroy関数内で自動的に呼び出されるので、アプリケーションが直接呼び出す必要はありません。
	 */
	public void Shutdown() {
		/* 初期化カウンタの更新 */
		initializationCount--;
		if (initializationCount != 0) {
			initializationCount = initializationCount < 0 ? 0 : initializationCount;
			return;
		}

#if UNITY_EDITOR || (!UNITY_PS3)
		/* CRI Manaライブラリの終了 */
		if (initializesMana) {
			CriManaPlugin.FinalizeLibrary();
		}
#endif

		/* CRI Atomライブラリの終了 */
		if (initializesAtom) {
			/* EstimatorがStop状態になるまでFinalize */
			while (CriAtomExLatencyEstimator.GetCurrentInfo().status != CriAtomExLatencyEstimator.Status.Stop) {
				CriAtomExLatencyEstimator.FinalizeModule();
			}

			/* 終了処理の実行 */
			CriAtomPlugin.FinalizeLibrary();
		}

		/* CRI File Systemライブラリの終了 */
		if (initializesFileSystem) {
			CriFsPlugin.FinalizeLibrary();
		}
	}

	protected override void OnDestroy() {
		base.OnDestroy();
		Shutdown();
	}
	
	/* 初期化カウンタ */
	private static int initializationCount = 0;

	/* 初期化実行チェック関数 */
	public static bool IsInitialized() {
		if (initializationCount > 0) {
			return true;
		} else {
			/* 現在のランタイムのバージョンが正しいかチェック */
			CriWare.CheckBinaryVersionCompatibility();
			return false;
		}
	}

	/**
	 * <summary> カスタムエフェクトのインタフェース登録 </summary> 
	 * \par 説明:
	 * ユーザが独自に実装したASRバスエフェクト(カスタムエフェクト)の
	 * インタフェースを登録するためのメソッドです。
	 * CRI ADX2 Audio Effect Plugin SDK を使用することで、
	 * ユーザ独自の ASR バスエフェクトを作成することができます。
	 * <br/>
	 * 通常は、予め用意されたエフェクト処理しか使うことができません。
	 * CRIWARE で定められたルールに従ってカスタムエフェクトライブラリを実装することで、
	 * ユーザは CRIWAER Unity Plug-in 用カスタムエフェクトインタフェースを用意することができます。 
	 * <br/>
	 * このインタフェースへのポインタを、本関数を用いて CRIWAER Unity Plug-in に登録することで、
	 * CRI ライブラリ初期化時にカスタムエフェクトが有効化されます。
	 * <br/>
	 * なお、登録したカスタムエフェクトは CRI ライブラリの終了時に強制的に登録解除されます。
	 * 再度 CRI ライブラリを初期化する際には、改めて本関数を呼び出してカスタムエフェクトの
	 * インタフェース登録を行ってください。
	 * \par 注意：
	 * 必ず CRI ライブラリの初期化前に本関数を呼んでください。
	 * 本関数で追加されたカスタムエフェクトのインタフェースは、CRI ライブラリの初期化処理内で
	 * 実際に有効化されます。
	 */
	static public void AddAudioEffectInterface(IntPtr effect_interface)
	{
		List<IntPtr> effect_interface_list = null;
		if (CriAtomPlugin.GetAudioEffectInterfaceList(out effect_interface_list))
		{
			effect_interface_list.Add(effect_interface);
		}
	}

	public static bool InitializeFileSystem(CriFsConfig config)
	{
		/* CRI File Systemライブラリの初期化 */
		if (!CriFsPlugin.IsLibraryInitialized()) {
			CriFsPlugin.SetConfigParameters(
				config.numberOfLoaders,
				config.numberOfBinders,
				config.numberOfInstallers,
				(config.installBufferSize * 1024),
				config.maxPath,
				config.minimizeFileDescriptorUsage,
				config.enableCrcCheck
				);
			{
				/* Ver.2.03.03 以前は 0 がデフォルト値だったことの互換性維持のための処理 */
				if (config.androidDeviceReadBitrate == 0) {
					config.androidDeviceReadBitrate = CriFsConfig.defaultAndroidDeviceReadBitrate;
				}
			}
			CriFsPlugin.SetConfigAdditionalParameters_ANDROID(config.androidDeviceReadBitrate);
			CriFsPlugin.InitializeLibrary();
			if (config.userAgentString.Length != 0) {
				CriFsUtility.SetUserAgentString(config.userAgentString);
			}
			return true;
		} else {
			return false;
		}
	}

	public static bool InitializeAtom(CriAtomConfig config)
	{
		/* CRI Atomライブラリの初期化 */
		if (CriAtomPlugin.IsLibraryInitialized() == false) {
			/* 初期化処理の実行 */
			CriAtomPlugin.SetConfigParameters(
				(int)Math.Max(config.maxVirtualVoices, CriAtomPlugin.GetRequiredMaxVirtualVoices(config)),
				config.maxVoiceLimitGroups,
				config.maxCategories,
				config.maxSequenceEventsPerFrame,
				config.maxBeatSyncCallbacksPerFrame,
				config.standardVoicePoolConfig.memoryVoices,
				config.standardVoicePoolConfig.streamingVoices,
				config.hcaMxVoicePoolConfig.memoryVoices,
				config.hcaMxVoicePoolConfig.streamingVoices,
				config.outputSamplingRate,
				config.asrOutputChannels,
				config.usesInGamePreview,
				config.serverFrequency,
				config.maxParameterBlocks,
				config.categoriesPerPlayback,
				config.maxBuses,
				config.vrMode);

			CriAtomPlugin.SetConfigAdditionalParameters_PC(
				config.pcBufferingTime
				);

			CriAtomPlugin.SetConfigAdditionalParameters_LINUX(
				config.linuxOutput
				);

			CriAtomPlugin.SetConfigAdditionalParameters_IOS(
				(uint)Math.Max(config.iosBufferingTime, 16),
				config.iosOverrideIPodMusic
				);
			/* Android 固有の初期化パラメータを登録 */
			{
				/* Ver.2.03.03 以前は 0 がデフォルト値だったことの互換性維持のための処理 */
				if (config.androidBufferingTime == 0) {
					config.androidBufferingTime = (int)(4 * 1000.0 / config.serverFrequency);
				}
				if (config.androidStartBufferingTime == 0) {
					config.androidStartBufferingTime = (int)(3 * 1000.0 / config.serverFrequency);
				}
#if !UNITY_EDITOR && UNITY_ANDROID
				CriAtomEx.androidDefaultSoundRendererType = config.androidForceToUseAsrForDefaultPlayback ?
					CriAtomEx.SoundRendererType.Asr : CriAtomEx.SoundRendererType.Default;
#endif
				CriAtomPlugin.SetConfigAdditionalParameters_ANDROID(
					config.androidLowLatencyStandardVoicePoolConfig.memoryVoices,
					config.androidLowLatencyStandardVoicePoolConfig.streamingVoices,
					config.androidBufferingTime,
					config.androidStartBufferingTime,
					config.androidUsesAndroidFastMixer);
			}
			/* 要修正：static関数化したためinitializesMana、manaConfigが参照できない。暫定的に第三引数は0にしておく。*/
			CriAtomPlugin.SetConfigAdditionalParameters_VITA(
				config.vitaAtrac9VoicePoolConfig.memoryVoices,
				config.vitaAtrac9VoicePoolConfig.streamingVoices,
				config.vitaManaVoicePoolConfig.numberOfManaDecoders);
			{
				/* VR Mode が有効なときも useAudio3D を True にする */
				config.ps4Audio3dConfig.useAudio3D |= config.vrMode;
				CriAtomPlugin.SetConfigAdditionalParameters_PS4(
					config.ps4Atrac9VoicePoolConfig.memoryVoices,
					config.ps4Atrac9VoicePoolConfig.streamingVoices,
					config.ps4Audio3dConfig.useAudio3D,
					config.ps4Audio3dConfig.voicePoolConfig.memoryVoices,
					config.ps4Audio3dConfig.voicePoolConfig.streamingVoices);
			}
			CriAtomPlugin.SetConfigAdditionalParameters_WEBGL(
				config.webglWebAudioVoicePoolConfig.voices);

			CriAtomPlugin.InitializeLibrary();

			if (config.useRandomSeedWithTime == true){
				/* 時刻を乱数種に設定 */
				CriAtomEx.SetRandomSeed((uint)System.DateTime.Now.Ticks);
			}

			/* ACFファイル指定時は登録 */
			if (config.acfFileName.Length != 0) {
			#if UNITY_WEBGL
				Debug.LogError("In WebGL, ACF File path should be set to CriAtom Component.");
			#else
				string acfPath = config.acfFileName;
				if (CriWare.IsStreamingAssetsPath(acfPath)) {
					acfPath = Path.Combine(CriWare.streamingAssetsPath, acfPath);
				}

				CriAtomEx.RegisterAcf(null, acfPath);
			#endif
			}
			return true;
		} else {
			return false;
		}

	}

	public static bool InitializeMana(CriManaConfig config) {
		/* CRI Manaライブラリの初期化 */
		if (CriManaPlugin.IsLibraryInitialized() == false) {
			CriManaPlugin.SetConfigParameters(config.graphicsMultiThreaded, config.numberOfDecoders, config.numberOfMaxEntries);
			CriManaPlugin.SetConfigAdditonalParameters_ANDROID(true);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			if (CriAtomPlugin.IsLibraryInitialized() == false) {
				CriManaPlugin.SetConfigAdditonalParameters_PC(config.pcH264PlaybackConfig.useH264Playback);
			}
#endif
#if !UNITY_EDITOR && UNITY_PSP2
			CriWareVITA.EnableManaH264Playback(config.vitaH264PlaybackConfig.useH264Playback);
			CriWareVITA.SetManaH264DecoderMaxSize(config.vitaH264PlaybackConfig.maxWidth,
													 config.vitaH264PlaybackConfig.maxHeight);
			CriWareVITA.EnableManaH264DecoderGetDisplayMemoryFromUnityTexture(config.vitaH264PlaybackConfig.getMemoryFromTexture);
#endif
#if !UNITY_EDITOR && UNITY_WEBGL
			CriManaPlugin.SetConfigAdditonalParameters_WEBGL(
				config.webglConfig.webworkerPath,
				(uint)config.webglConfig.heapSize);
#endif

			CriManaPlugin.InitializeLibrary();

			// set shader global keyword to inform cri mana shaders to output to correct colorspace
			if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
				Shader.EnableKeyword("CRI_LINEAR_COLORSPACE");
			}
			return true;
		} else {
			return false;
		}
	}

	[System.Obsolete("Use CriWareDecypter.Initialize")]
	public static bool InitializeDecrypter(CriWareDecrypterConfig config) {
		return CriWareDecrypter.Initialize(config);
	}


} // end of class

/// @}

/* --- end of file --- */
