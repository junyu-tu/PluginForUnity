/****************************************************************************
 *
 * Copyright (c) 2015 - 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

/*---------------------------
 * Cuepoint Callback Defines
 *---------------------------*/
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_WINRT || UNITY_WEBGL || UNITY_STADIA || UNITY_STANDALONE_LINUX
	#define CRIMANA_PLAYER_SUPPORT_CUEPOINT_CALLBACK
#elif UNITY_PSP2 || UNITY_PS3 || UNITY_PS4 || UNITY_XBOXONE || UNITY_SWITCH
	#define CRIMANA_PLAYER_UNSUPPORT_CUEPOINT_CALLBACK
#else
	#error undefined platform if supporting cuepoint callback
#endif

// Use old or new Low-level Native Plugin Interface (from unity 5.2)
#if (UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
	#define CRIPLUGIN_USE_OLD_LOWLEVEL_INTERFACE
#endif

// Uncomment this for debug information.
//#define CRIMANA_STAT

using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if CRIMANA_STAT
using System.Collections;
using UnityEngine.UI;
#endif

/**
 * \addtogroup CRIMANA_NATIVE_WRAPPER
 * @{
 */
namespace CriMana
{

/**
 * <summary>ムービ再生制御を行うネイティブプレーヤのラッパークラスです。</summary>
 * \par 説明:
 * <br/>
 * 通常、 CriManaMovieController や CriManaMovieControllerForUI コンポーネントから取り出して使用します。
 * 再生停止・ポーズ以上の複雑な再生制御を行う場合本クラスを使用してください。
 * <br/>
 * コンポーネントを使用せずに直接ムービ再生を制御したい場合、本オブジェクトを直接生成して使用することも可能です。
 * <br/>
 * \par 注意:
 * 本クラスのオブジェクトを直接アプリケーションから生成した場合、再生終了後に Player::Dispose 関数で必ず破棄してください。
 */
	public class Player : CriDisposable
	{
		#region Data Types
		/**
		 * <summary>プレーヤの状態を示す値です。</summary>
		 * \sa Player::status
		 */
		public enum Status
		{
			Stop,			/**< 停止中		*/
			Dechead,		/**< ヘッダ解析中	*/
			WaitPrep,		/**< バッファリング開始停止中 */
			Prep,			/**< 再生準備中	*/
			Ready,			/**< 再生準備完了	*/
			Playing,		/**< 再生中		*/
			PlayEnd,		/**< 再生終了		*/
			Error,			/**< エラー		*/
			StopProcessing, /**< 停止処理中	*/

			ReadyForRendering,	/**< 描画準備完了	*/
		}


		/**
		 * <summary>ファイルをセットする際のモードです。</summary>
		 * \sa Player::SetFile, Player::SetContentId, Player::SetFileRange
		 */
		public enum SetMode
		{
			New,				/**< 次回の再生で利用するムービを指定	*/
			Append,				/**< 連結再生キューに追加する	*/
			AppendRepeatedly	/**< 連結再生キューに次のムービが登録されるまで繰り返し追加する	*/
		}

		/**
		 * <summary>ムービイベント（キューポイント、字幕）の同期モードです。</summary>
		 * \sa Player::SetMovieEventSyncMode
		 */
		public enum MovieEventSyncMode
		{
			FrameTime,			/**< フレーム時刻に同期します */
			PlayBackTime		/**< タイマ種別に基づいた再生時刻に同期します */
		}

		/**
		 * <summary>オーディオトラックを設定する際のモードです。</summary>
		 * \sa Player::SetAudioTrack, Player::SetSubAudioTrack
		 */
		public enum AudioTrack
		{
			Off,	/**< オーディオ再生OFFの指定値	*/
			Auto,	/**< オーディオトラックのデフォルト値	*/
		}

		/**
		 * <summary>タイマー種別です。</summary>
		 * \sa Player::SetMasterTimerType
		 */
		public enum TimerType : int {
			None	= 0,	/**< 時刻同期無し */
			System	= 1,	/**< システム時刻 */
			Audio	= 2,	/**< メインオーディオトラックのオーディオ時刻 */
			User	= 3,	/**< ユーザー独自のタイマ */
			Manual	= 4,	/**< マニュアルタイマ */
		}

		/**
		 * <summary>キューポイントコールバックデリゲート型です。</summary>
		 * <param name="eventPoint">イベントポイント情報</param>
		 * \par 説明:
		 * キューポイントコールバックデリゲート型です。<br/>
		 * \par 注意:
		 * キューポイントコールバック内では、ムービ再生をコントロールする関数を呼び出してはいけません。
		 * \sa Player::cuePointCallback
		 */
		public delegate void CuePointCallback(ref EventPoint eventPoint);


		/**
		 * <summary>シェーダ選択デリゲート型です。</summary>
		 * <param name="movieInfo">ムービ情報</param>
		 * <param name="additiveMode">加算合成するかどうか</param>	 
		 * \par 説明:
		 * シェーダ選択デリゲート型です。<br/>
		 * \sa Player::SetShaderDispatchCallback
		 */
		public delegate UnityEngine.Shader ShaderDispatchCallback(MovieInfo movieInfo, bool additiveMode);
		#endregion


		#region Constant Variables
		private const int  InvalidPlayerId = -1;
		#endregion


		#region Internal Variables
		static private Player		updatingPlayer			= null;

		private int					playerId				= InvalidPlayerId;
		private bool				isDisposed				= false;
		private Status				requiredStatus			= Status.Stop;
		private Status				nativeStatus			= Status.Stop;
		private bool				isNativeStartInvoked	= false;
		private bool				isNativeInitialized		= false;
		private Detail.RendererResource	rendererResource;
		private MovieInfo			_movieInfo				= new MovieInfo();
		private FrameInfo			_frameInfo				= new FrameInfo();
		private bool				isMovieInfoAvailable	= false;
		private bool				isFrameInfoAvailable	= false;
		private ShaderDispatchCallback	_shaderDispatchCallback	= null;
		private bool				enableSubtitle			= false;
		private int					subtitleBufferSize		= 0;
		private uint				droppedFrameCount		= 0;
		private CriAtomExPlayer		_atomExPlayer			= null;
		private CriAtomEx3dSource   _atomEx3Dsource			= null;
		private TimerType			timerType				= TimerType.Audio;
		private bool				isStoppingForSeek		= false;
		#endregion


		#region Properties
		/**
		 * <summary>キューポイントコールバックデリゲートです。</summary>
		 * \par 説明:
		 * キューポイントコールバックデリゲートです。<br/>
		 * \par 注意:
		 * Player::Prepare 関数および Player::Start 関数より前に設定する必要があります。
		 * \sa Player::CuePointCallback
		 */
		public CuePointCallback	cuePointCallback;


		/**
		 * <summary>ブレンドモードを加算合成モードにします。</summary>
		 * \par 説明:
		 * 再生時のシェーダ選択コールバックに渡される引数になります。<br/>
		 * \par 注意:
		 * Player::Prepare 関数および Player::Start 関数より前に設定する必要があります。
		 * \sa Player::ShaderDispatchCallback
		 */
		public bool additiveMode { get; set; }


		/**
		 * <summary>最大フレームドロップ数を設定します。</summary>
		 * \par 説明:
		 * 再生に対して描画フレームの更新が間に合っていない場合に一度の更新でドロップする
		 * フレームの最大数を設定します。<br/>
		 * アプリケーションのフレームレートが低下している場合や、フレームレート以上に
		 * 動画の再生速度を上げた場合に、意図通りに再生が行えるようになります。
		 */
		public int maxFrameDrop { get; set; }


		/**
		 * <summary>オブジェクトの透過度を適用するかを設定します。</summary>
		 * \par 説明:
		 * アタッチしたオブジェクトの透過度に応じて、ムービも透過させるかを設定します。<br/>
		 * \par 注意:
		 * Player::Prepare 関数および Player::Start 関数より前に設定する必要があります。
		 */
		public bool applyTargetAlpha { get; set; }


		/**
		 * <summary>有効なフレームを所持しているかどうか</summary>
		 * \par 説明:
		 * 有効なフレームを所持しているかどうかを返します。<br/>
		 * \par 備考:
		 * 独自で描画を行う場合、本フラグで描画の切り替え制御を行ってください。<br/>
		 */
		public bool isFrameAvailable
		{
			get { return isFrameInfoAvailable; }
		}


		/**
		 * <summary>再生中のムービ情報を取得します。</summary>
		 * <param name="movieInfo">ムービ情報</param>
		 * \par 説明:
		 * 再生を開始したムービのヘッダ解析結果の情報です。<br/>
		 * ムービの解像度、フレームレートなどの情報が参照できます。<br/>
		 * \par 注意:
		 * プレーヤのステータスが \link Player::Status WaitPrep〜Playing \endlink 状態の間のみ情報が取得できます。<br/>
		 * それ以外の状態は null が返ります。
		 */
		public MovieInfo movieInfo
		{
			get { return isMovieInfoAvailable ? _movieInfo : null; }
		}


		/**
		 * <summary>再生中のムービのフレーム情報を取得します。</summary>
		 * <param name="frameInfo">フレーム情報</param>
		 * \par 説明:
		 * 現在描画中のフレーム情報を取得できます。<br/>
		 * \par 備考:
		 * 再生状態のみ情報が取得できます。それ以外の状態では null が返ります。<br/>
		 */
		public FrameInfo frameInfo
		{
			get { return isFrameAvailable ? _frameInfo : null; }
		}


		/**
		 * <summary>プレーヤのステータスを取得します。</summary>
		 * <returns>ステータス</returns>
		 * \par 説明：
		 * プレーヤのステータスを取得します。<br/>
		 */
		public Status status
		{
			get {
				if ((requiredStatus == Status.Stop) && (nativeStatus != Status.Stop)) {
					return Status.StopProcessing;
				}
				if (requiredStatus == Status.ReadyForRendering) {
					if (nativeStatus == Status.Playing) {
						return ((rendererResource != null) && (rendererResource.IsPrepared()))
							? Status.ReadyForRendering : Status.Prep;
					} else {
						return Status.Prep;
					}
				}
				if (nativeStatus == Status.Ready) {
					return (rendererResource != null) ? Status.Ready : Status.Prep;
				} else {
					return nativeStatus;
				}
			}
		}


		/**
		 * <summary>連結再生エントリー数を取得します。</summary>
		 * \par 説明：
		 * 連結再生エントリー数を取得します。<br/>
		 * このプロパティで得られる値は、読み込みが開始されていないエントリー数です。<br/>
		 * 連結する数フレーム前に、エントリーに登録されているムービの読み込みが開始されます。<br/>
		 */
		public System.Int32 numberOfEntries
		{
			get {
				return CRIWARE54A7B6FE(playerId);
			}
		}


		/**
		 * <summary>字幕データバッファへのポインタを取得します。</summary>
		 * \par 説明：
		 * 字幕データバッファへのポインタを取得します。<br/>
		 * 表示時刻になっている字幕データが格納されています。<br/>
		 * System.Runtime.InteropServices.Marshal.PtrToStringAuto などで変換して使用してください。<br/>
		 * \sa Player::SetSubtitleChannel, Player::subtitleSize
		 */
		public System.IntPtr subtitleBuffer { get; private set; }


		/**
		 * <summary>字幕データバッファのサイズを取得します。</summary>
		 * \par 説明：
		 * 字幕データバッファのサイズを取得します<br/>
		 * System.Runtime.InteropServices.Marshal.PtrToStringAuto などで変換して使用してください。<br/>
		 * \sa Player::SetSubtitleChannel, Player::subtitleBuffer
		 */
		public int subtitleSize { get; private set; }


		public CriAtomExPlayer atomExPlayer
		{
			get
			{
				return _atomExPlayer;
			}
		}


		public CriAtomEx3dSource atomEx3DsourceForAmbisonics
		{
			get
			{
				return _atomEx3Dsource;
			}
		}

		/**
		 * <summary>Player Holder object.</summary>
		 * \par 説明：
		 *  MonoBehaviour derived object that keep alive Player before destruction. 
		 *  This permits to have coroutines used to control the cleaning of 
		 *  all unmanaged resourses that can be held by GPU and render thread.
		 */
		public CriManaMoviePlayerHolder playerHolder { get; set; }
		#endregion


		public Player()
		{
			if (!CriManaPlugin.IsLibraryInitialized()) {
				throw new Exception("CriManaPlugin is not initialized.");
			}
			playerId = CRIWARE83C6547D();
			additiveMode = false;
			maxFrameDrop = 0;
			CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Mana);
		}


		public Player(bool advanced_audio_mode, bool ambisonics_mode) {
			if (!CriManaPlugin.IsLibraryInitialized()) {
				throw new Exception("CriManaPlugin is not initialized.");
			}
			if (advanced_audio_mode) {
				/* AtomExPlayer 付きで作成 */
				playerId = CRIWARE96C9B3EF();
				_atomExPlayer = new CriAtomExPlayer(CRIWARE8FE20FE9(playerId));
				if (ambisonics_mode) {
					_atomEx3Dsource = new CriAtomEx3dSource();
					_atomExPlayer.Set3dSource(_atomEx3Dsource);
					_atomExPlayer.Set3dListener(CriAtomListener.sharedNativeListener);
					_atomExPlayer.SetPanType(CriAtomEx.PanType.Pos3d);
					_atomExPlayer.UpdateAll();
				}
			} else {
				playerId = CRIWARE83C6547D();
			}
			additiveMode = false;
			maxFrameDrop = 0;
			CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Mana);
		}

		~Player()
		{
			Dispose(false);
		}


		/**
		 * <summary>プレーヤオブジェクトの破棄</summary>
		 * \par 説明:
		 * プレーヤオブジェクトを破棄します。<br/>
		 * 独自に CriMana::Player オブジェクトを生成した場合、必ず Dispose 関数を呼び出してください。
		 * 本関数を実行した時点で、プレーヤ作成時に確保されたリソースが全て解放されます。<br/>
		 */
		public override void Dispose()
		{
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}


		/**
		 * <summary>ムービ描画に必要なリソースを生成します。</summary>
		 * <param name="width">ムービの幅</param>
		 * <param name="height">ムービの高さ</param>
		 * <param name="alpha">アルファムービか否か</param>
		 * \par 説明:
		 * ムービ描画に必要なリソースを生成します。<br/>
		 * 通常はムービ描画に必要なリソースの生成はプレーヤ内部で自動的に行いますが、 Player::Prepare および
		 * Player::Start を呼び出す前に本関数を呼び出すことでリソースの生成を事前に行うことができます。<br/>
		 * ただし、この関数で事前に生成したリソースの幅、高さが不十分などの理由で<br/>
		 * 実際に再生するムービの描画に利用できない場合はリソースは破棄されて再生成されます。
		 * \sa Player::DisposeRendererResource
		 */
		public void CreateRendererResource(int width, int height, bool alpha)
		{
			MovieInfo dummyMovieInfo = new MovieInfo();
			dummyMovieInfo.hasAlpha			= alpha;
			dummyMovieInfo.width			= (System.UInt32)width;
			dummyMovieInfo.height			= (System.UInt32)height;
			dummyMovieInfo.dispWidth		= (System.UInt32)width;
			dummyMovieInfo.dispHeight		= (System.UInt32)height;
			dummyMovieInfo.codecType		= CodecType.SofdecPrime;
			dummyMovieInfo.alphaCodecType	= CodecType.SofdecPrime;

			UnityEngine.Shader userShader		= (_shaderDispatchCallback == null) ? null : _shaderDispatchCallback(movieInfo, additiveMode);
			if (rendererResource != null) {
				if (!rendererResource.IsSuitable(playerId, dummyMovieInfo, additiveMode, userShader)) {
					rendererResource.Dispose();
					rendererResource = null;
				}
			}
			if (rendererResource == null) {
				rendererResource = Detail.RendererResourceFactory.DispatchAndCreate(playerId, dummyMovieInfo, additiveMode, userShader);
			}
		}


		/**
		 * <summary>ムービ描画に必要なリソースを破棄します。</summary>
		 * \par 説明:
		 * ムービ描画に必要なリソースを破棄します。<br/>
		 * リソースの破棄は Player::Dispose 時に行われますが、
		 * 本関数で明示的に行うことができます。<br/>
		 * \sa Player::CreateRendererResource
		 */
		public void DisposeRendererResource()
		{
			if (rendererResource != null) {
				rendererResource.Dispose();
				rendererResource = null;
			}
		}


		/**
		 * <summary>再生準備処理を行います。</summary>
		 * \par 説明:
		 * ムービ再生は開始せず、ヘッダ解析と再生準備(バッファリング)のみを行って待機する関数です。<br/>
		 * <br/>
		 * 再生対象のムービを指定した後、本関数を呼び出してください。
		 * 再生準備が完了すると、プレーヤの状態は \link CriMana::Player::Status Ready \endlink 状態になります。<br/>
		 * <br/>
		 * 本関数は \link CriMana::Player::Status Stop \endlink 状態、また \link CriMana::Player::Status PlayEnd \endlink 状態でのみ呼び出してください。
		 */
		public void Prepare()
		{
			if ((nativeStatus == Status.Stop) || (nativeStatus == Status.PlayEnd)) {
				PrepareNativePlayer();
				UpdateNativePlayer();
				requiredStatus = Status.Ready;
			}
		}

		/**
		 * <summary>再生準備処理を行います。</summary>
		 * \par 説明:
		 * ムービ再生は開始せず、ヘッダ解析と再生準備(バッファリングと一フレーム目の用意)のみを行って待機する関数です。<br/>
		 * ムービ再生開始の頭出しをそろえたい場合に本関数を使用します。<br/>
		 * <br/>
		 * 再生対象のムービを指定した後、本関数を呼び出してください。
		 * 再生準備が完了すると、プレーヤの状態は \link CriMana::Player::Status ReadyForRendering \endlink 状態になり、 
		 * CriMana::Player::Play 関数ですぐに描画開始されます。<br/>
		 * <br/>
		 * 本関数は \link CriMana::Player::Status Stop \endlink 状態、 \link CriMana::Player::Status PlayEnd \endlink 状態、
		 * または \link CriMana::Player::Status Prepare \endlink 状態でのみ呼び出してください。
		 */
		public void PrepareForRendering()
		{
			if (requiredStatus == Status.Ready) {
				Pause(true);
				requiredStatus = Status.ReadyForRendering;
			} else if ((nativeStatus == Status.Stop) || (nativeStatus == Status.PlayEnd)) {
				Start();
				Pause(true);
				requiredStatus = Status.ReadyForRendering;
			}
		}

		/**
		 * <summary>再生を開始します。</summary>
		 * \par 説明:
		 * ムービ再生を開始します。<br/>
		 * 再生が開始されると、状態は \link Player::Status Playing \endlink になります。
		 * \par 注意:
		 * 本関数を呼び出し後、実際にムービの描画が開始されるまで数フレームかかります。<br>
		 * ムービ再生の頭出しを行いたい場合は、 Player::Prepare 関数で事前に再生準備を行ってください。
		 * \sa Player::Stop, Player::Status
		 */
		public void Start()
		{
			if ((nativeStatus == Status.Stop) || (nativeStatus == Status.PlayEnd)) {
				PrepareNativePlayer();
				UpdateNativePlayer();
			} else if (requiredStatus == Status.ReadyForRendering) {
				Pause(false);
				UpdateNativePlayer();
			}
			if (rendererResource != null) {
				rendererResource.OnPlayerStart();
			}
			requiredStatus = Status.Playing;
			isStoppingForSeek = false;
		}


		/**
		 * <summary>ムービ再生の停止要求を行います。</summary>
		 * \par 説明:
		 * ムービ再生の停止要求を出します。本関数は即時復帰関数です。<br/>
		 * 本関数を呼び出しても、再生はすぐには止まりません。<br/>
		 * \sa Player::Status
		 */
		public void Stop()
		{
			isStoppingForSeek = false;
			if (playerId != InvalidPlayerId) {
				CRIWAREE8255B60(playerId);
				UpdateNativePlayer();
			}
			DisableInfos();
			requiredStatus = Status.Stop;
		}


		/**
		 * <summary>ムービ再生の停止要求を行います。</summary>
		 * \par 説明:
		 * ムービ再生の停止要求を出します。本関数は即時復帰関数です。<br/>
		 * 本関数を呼び出しても、再生はすぐには止まりません。<br/>
		 * 内部的には再生を停止しますが、描画状態はキープされます。<br/>
		 * 再生中にシークを行う場合など、描画したままで停止する必要がある場合にご利用ください。
		 * \sa Player::Start, Player::Status
		 */
		public void StopForSeek() {
			if (rendererResource != null) {
				if (!rendererResource.OnPlayerStopForSeek()) {
					UnityEngine.Debug.LogWarning("[CRIWARE] StopForSeek is not supported on this platform/codec.");
					Stop();
					return;
				}
			}
			if (playerId != InvalidPlayerId) {
				isStoppingForSeek = true;
				CRIWAREE8255B60(playerId);
				UpdateNativePlayer();
			}
			requiredStatus = Status.Stop;
		}


		/**
		 * <summary>ムービ再生のポーズ切り替えを行います。</summary>
		 * <param name="sw">ポーズスイッチ(true: ポーズ, false: ポーズ解除)</param>
		 * \par 説明:
		 * ポーズのON/OFFを切り替えます。<br/>
		 * 引数にtrueを指定する一時停止、falseを指定すると再生再開です。<br/>
		 * Player::Stop 関数を呼び出すと、ポーズ状態は解除されます <br/>
		 * \sa Player::IsPaused
		 */
		public void Pause(bool sw)
		{
			CRIWARE8ABAD03F(playerId, (sw) ? 1 : 0);

			if (rendererResource != null) {
				rendererResource.OnPlayerPause(sw);
			}
		}


		/**
		 * <summary>ムービ再生のポーズ状態の取得を行います。</summary>
		 * <returns>ポーズ状態</returns>
		 * \par 説明:
		 * ポーズのON/OFFを取得します。<br/>
		 * \sa Player::Pause
		 */
		public bool IsPaused()
		{
			return CRIWARE48866453(playerId);
		}


		/**
		 * <summary>ストリーミング再生用のファイルパスの設定を行います。</summary>
		 * <param name="binder">CPKファイルをバインド済みのバインダ</param>
		 * <param name="moviePath">CPKファイル内のコンテンツパス</param>
		 * <param name="setMode">ムービの追加モード</param>
		 * <returns>セットに成功したか</returns>
		 * \par 説明:
		 * ストリーミング再生用のファイルパスを設定します。<br/>
		 * 第一引数の binder にCPKファイルをバインドしたバインダハンドルを指定することにより、CPKファイルからのムービ再生が行えます。<br/>
		 * CPKからではなく直接ファイルからストリーミングする場合は、binder に null を指定ください。<br/>
		 * 第三引数の setMode に Player::SetMode::Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
		 * 連結再生できるムービファイルには以下の条件があります。<br>
		 * - ビデオの解像度、フレームレート、コーデックが全て同じ
		 * - アルファ情報の有無が一致
		 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
		 * 
		 * \par バインダを指定しない場合(nullを指定した場合):
		 * - moviePath プロパティを設定した場合と等価です。<br/>
		 * - 相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。
		 * - 絶対パスを指定した場合には指定したパスでファイルをロードします。<br/>
		 * 
		 * \par バインダを指定した場合:
		 * バインダ設定後に moviePath プロパティを設定した場合は、バインダにバインドされているCPKファイルからムービ再生が行われます。<br/>
		 * \attention
		 * moviePath として URL を指定することもできます。例えば "http://hoge-server/fuga-movie.usm" のような文字列を指定することで、
		 * HTTP サーバ上のムービファイルをストリーミング再生することが可能です。ただしこの機能は非推奨です。
		 * また、通信プロトコルとして HTTP にしか対応していません。HTTPS には非対応です。
		 * \sa Player::SetContentId, Player::SetFileRange, Player::moviePath
		 */
		public bool SetFile(CriFsBinder binder, string moviePath, SetMode setMode = SetMode.New)
		{
			System.IntPtr binderPtr = (binder == null) ? System.IntPtr.Zero : binder.nativeHandle;
			if ((binder == null) && CriWare.IsStreamingAssetsPath(moviePath) ) {
				moviePath = System.IO.Path.Combine(CriWare.streamingAssetsPath, moviePath);
			}
			if (setMode == SetMode.New) {
				CRIWARE697736B9(playerId, binderPtr, moviePath);
				return true;
			} else {
				return CRIWAREA2D9C574(
					playerId, binderPtr, moviePath,
					(setMode == SetMode.AppendRepeatedly)
					);
			}
		}

		/**
		 * <summary>メモリ再生用のデータ設定を行います。</summary>
		 * <param name="data">メモリ上にあるムービーデータ</param>
		 * <param name="datasize">データのサイズ</param>
		 * <param name="setMode">ムービの追加モード</param>
		 * <returns>セットに成功したか</returns>
		 * \par 説明:
		 * メモリ再生用のデータ設定を行います。<br/>
		 * 第一引数と第二引数でデータのアドレスとサイズを指定します。
		 * 第三引数の setMode に Player::SetMode::Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
		 * 連結再生できるムービファイルの条件は、 Player::SetFile 関数と同じです。<br>
	 	 * \attention
		 * SetDataに渡すバッファアドレスは、ガベージコレクターに移動されないようにアプリケーション
		 * 側で事前に固定したものを渡してください。<br/>
		 * また、メモリの固定解除は再生終了状態、再生停止状態、またはプレーヤオブジェクトの破棄後に行ってください。<br/>
		 *
 		 * \sa Player::SetFile, Player::SetContentId, Player::SetFileRange, Player::moviePath
		 */
		public bool SetData(byte[] data, System.Int64 datasize, SetMode setMode = SetMode.New)
		{
			if (setMode == SetMode.New) {
				CRIWARE72CBF76A(playerId, data, datasize);
				return true;
			} else {
				return CRIWARE383926E8(
					playerId, data, datasize,
					(setMode == SetMode.AppendRepeatedly)
					);
			}
		}

		/**
		 * <summary>CPKファイル内のムービファイルの指定を行います。 (コンテンツID指定)</summary>
		 * <param name="binder">CPKファイルをバインド済みのバインダ</param>
		 * <param name="contentId">CPKファイル内のコンテンツID</param>
		 * <param name="setMode">ムービの追加モード</param>
		 * <returns>セットに成功したか</returns>
		 * \par 説明:
		 * ストリーミング再生用のコンテンツIDを設定します。<br/>
		 * 引数にバインダとコンテンツIDを指定することで、CPKファイル内の任意のムービデータを読み込み元にすることが出来ます。
		 * 第三引数の setMode に Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
		 * 連結再生できるムービファイルには以下の条件があります。<br>
		 * - ビデオの解像度、フレームレート、コーデックが全て同じ
		 * - アルファ情報の有無が一致
		 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
		 * \sa Player::SetFile, Player::SetFileRange, Player::SetData
		 */
		public bool SetContentId(CriFsBinder binder, int contentId, SetMode setMode = SetMode.New)
		{
			if (binder == null) {
				UnityEngine.Debug.LogError("[CRIWARE] CriFsBinder is null");
				return false;
			}
			if (setMode == SetMode.New) {
				CRIWARE6E81E5C7(playerId, binder.nativeHandle, contentId);
				return true;
			}
			else {
				return CRIWAREC317A335(
					playerId, binder.nativeHandle, contentId,
					(setMode == SetMode.AppendRepeatedly)
					);
			}
		}


		/**
		 * <summary>パックファイル内のムービファイルの指定を行います。 (ファイル範囲指定)</summary>
		 * <param name="filePath">パックファイルへのパス</param>
		 * <param name="offset">パックファイル内のムービファイルのデータ開始位置</param>
		 * <param name="range">パックファイル内のムービファイルのデータ範囲</param>
		 * <param name="setMode">ムービの追加モード</param>
		 * <returns>セットに成功したか</returns>
		 * \par 説明:
		 * ストリーミング再生を行いたいムービファイルをパッキングしたファイルを指定します。<br/>
		 * 引数にオフセット位置とデータ範囲を指定することで、パックファイル内の任意のムービデータを読み込み元にすることが出来ます。
		 * 第二引数の range に -1 を指定した場合はパックファイルの終端までをデータ範囲とします。
		 * 第四引数の setMode に Player::SetMode.Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
		 * 連結再生できるムービファイルには以下の条件があります。<br>
		 * - ビデオの解像度、フレームレート、コーデックが全て同じ
		 * - アルファ情報の有無が一致
		 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
		 * \sa Player::SetFile, Player::SetContentId, Player::SetData, Player::moviePath
		 */
		public bool SetFileRange(string filePath, System.UInt64 offset, System.Int64 range, SetMode setMode = SetMode.New)
		{
			if (setMode == SetMode.New) {
				CRIWAREB50C7533(playerId, filePath, offset, range);
				return true;
			}
			else {
				return CRIWARE1564F389(
					playerId, filePath, offset, range,
					(setMode == SetMode.AppendRepeatedly)
					);
			}
		}


		/**
		 * <summary>ムービ再生のループ切り替えを行います。</summary>
		 * \par 値の意味:
		 * <param name="sw">ループスイッチ(true: ループモード, false: ループモード解除)</param>
		 * \par 説明:
		 * ループ再生のON/OFFを切り替えます。デフォルトはループOFFです。<br/>
		 * ループ再生をONにした場合は、ムービ終端まで再生しても再生は終了せず先頭に戻って再生を
		 * 繰り返します。<br/>
		 */
		public void Loop(bool sw)
		{
			CRIWAREA8119B22(playerId, sw ? 1 : 0);
		}


		/**
		 * <summary>マスタータイマー種別を設定します。</summary>
		 * <param name="timerType">マスタータイマー種別</param>
		 * \par 説明:
		 * 動画再生のタイマーモードを設定します。<br/>
		 * デフォルトはハンドル作成時に指定するシステムタイマーです。<br/>
		 * ビデオフレームの表示タイミングを任意のタイマと同期させたい場合は、
		 * ユーザータイマを指定してください。<br/>
		 * その場合、定期的に ::CriMana::Player::UpdateWithUserTime を呼び出してください。
		 * \sa Player::UpdateWithUserTime, Player::SetManualTimerUnit, Player::UpdateWithManualTimeAdvanced
		 */
		public void SetMasterTimerType(TimerType timerType)
		{
			this.timerType = timerType;
			criManaUnityPlayer_SetMasterTimerType(playerId, timerType);
		}


		/**
		 * <summary>シーク位置の設定を行います。</summary>
		 * <param name="frameNumber">シーク先のフレーム番号</param>
		 * \par 説明:
		 * シーク再生を開始するフレーム番号を指定します。<br>
		 * 本関数を実行しなかった場合、またはフレーム番号0を指定した場合はムービの先頭から再生を開始します。
		 * 指定したフレーム番号が、ムービデータの総フレーム数より大きかったり負の値だった場合もムービの先頭から再生します。
		 */
		public void SetSeekPosition(int frameNumber)
		{
			CRIWARE247881E0(playerId, frameNumber);
		}

		/**
		 * <summary>ムービイベント（キューポイント、字幕）の同期モードの設定をします。</summary>
		 * <param name="mode">ムービイベントの同期モード</param>
		 * \par 説明:
		 * ムービイベント（キューポイント、字幕）の同期モードの設定をします。<br>
		 * デフォルトではフレーム時刻に同期してイベントがトリガされます。<br>
		 * ムービ再生時刻と同期させたいときはMovieEventSyncMode::PlayBackTimeを指定してください。<br>
		 * \attention
		 * 本関数は再生開始前に呼び出してください。
		 */
		public void SetMovieEventSyncMode(MovieEventSyncMode mode)
		{
			CRIWAREF3E8400F(playerId, mode);
		}

		/**
		 * <summary>ムービ再生の速度調整を行います。</summary>
		 * <param name="speed">再生速度</param>
		 * \par 説明:
		 * ムービの再生速度を指定します。<br/>
		 * 再生速度は、0.0f〜3.0fの範囲で実数値を指定します。<br/>
		 * 1.0fが指定された場合はデータ本来の速度で再生を行います。<br/>
		 * 例えば、2.0fを指定した場合、ムービはムービデータのフレームレートの２倍の速度で再生されます。<br/>
		 * 再生速度のデフォルト値は1.0fです。<br/>
		 * \par 注意:
		 * 再生中のムービに対する再生速度の変更は、音声がないムービを再生した場合のみ対応しています。<br/>
		 * 音声つきムービに対して再生中の速度変更を行うと正常動作しません。音声つきのムービの場合は、一度再生停止
		 * してから、再生速度を変更し、目的のフレーム番号からシーク再生をしてください。<br>
		 * <br/>
		 */
		public void SetSpeed(float speed)
		{
			CRIWAREB1595578(playerId, speed);
		}


		/**
		 * <summary>最大ピクチャデータサイズの指定</summary>
		 * <param name="maxDataSize">最大ピクチャデータサイズ</param>
		 * \par 説明:
		 * 最大ピクチャデータサイズの設定をします。<br/>
		 * 通常再生では、本関数を使用する必要はありません。<br/>
		 * <br/>
		 * H.264ムービを連結再生する場合、本関数で最大ピクチャデータサイズを設定してから再生を開始してください。<br/>
		 * 設定すべきデータサイズは、USMファイルをWizzに入力した際、「Maximum chunk size」と表示される数値の、全USMの最大値です。
		 * <br/>
		 */
		public void SetMaxPictureDataSize(System.UInt32 maxDataSize)
		{
			CRIWARE19485E17(playerId, maxDataSize);
		}


		/*JP
		 * <summary>入力データのバッファリング時間を指定します。</summary>
		 * <param name="sec">バッファリング時間 [秒]</param>
		 * \par 説明:
		 * ストリーミング再生でバッファリングする入力データの量を秒単位の時間で指定します。<br>
		 * Manaプレーヤは、バッファリング時間とムービのビットレート等から読み込みバッファのサイズを決定します。<br>
		 * デフォルトのバッファリング時間は、1秒分です。<br>
		 * バッファリング時間に 0.0f を指定した場合、バッファリング時間はライブラリのデフォルト値となります。<br>
		 */
		public void SetBufferingTime(float sec)
		{
			CRIWAREE723C4CF(playerId, sec);
		}


		/*JP
		 * <summary>最小バッファサイズを指定します。</summary>
		 * <param name="min_buffer_size">最小バッファサイズ [byte]</param>
		 * \par 説明:
		 * ムービデータの最小バッファサイズを指定します。<br>
		 * 最小バッファサイズを指定した場合、指定サイズ分以上の入力バッファがManaプレーヤ内部で確保することが保証されます。<br>
		 * 単純再生においては本関数を使用する必要はありません。極端にビットレートが異なるようなムービを連結再生する場合に使用します。<br>
		 * 最小バッファサイズに 0を指定した場合、最小バッファサイズはムービデータの持つ値となります。(デフォルト)<br>
		 */
		public void SetMinBufferSize(int min_buffer_size)
		{
			CRIWARE686BF411(playerId, min_buffer_size);
		}


		/**
		 * <summary>メインオーディオトラック番号の設定を行います。</summary>
		 * <param name="track">トラック番号</param>
		 * \par 説明:
		 * ムービが複数のオーディオトラックを持っている場合に、再生するオーディオを指定します。<br>
		 * 本関数を実行しなかった場合は、もっとも若い番号のオーディオトラックを再生します。<br>
		 * データが存在しないトラック番号を指定した場合は、オーディオは再生されません。<br>
		 * <br>
		 * トラック番号としてAudioTrack.Offを指定すると、例えムービにオーディオが
		 * 含まれていたとしてもオーディオは再生しません。<br>
		 * <br>
		 * また、デフォルト設定（もっとも若いチャネルのオーディオを再生する）にしたい場合は、
		 * チャネルとしてAudioTrack.Autoを指定してください。<br>
		 *
		 * \par 備考:
		 * 再生中のトラック変更は未対応です。変更前のフレーム番号を記録しておいてシーク再生
		 * してください。
		 */
		public void SetAudioTrack(int track)
		{
			CRIWARE452F4E29(playerId, track);
		}


		public void SetAudioTrack(AudioTrack track)
		{
			if (track == AudioTrack.Off) {
				CRIWARE452F4E29(playerId, -1);
			} else if (track == AudioTrack.Auto) {
				CRIWARE452F4E29(playerId, 100);
			}
		}


		/**
		 * <summary>サブオーディオトラック番号の設定を行います。</summary>
		 * <param name="track">トラック番号</param>
		 * \par 説明:
		 * ムービが複数のオーディオトラックを持っている場合に、再生するオーディオを指定します。<br>
		 * 本関数を実行しなかった場合（デフォルト設定）はサブオーディオからは何も再生されません。
		 * また、データが存在しないトラック番号を指定した場合、サブオーディオトラックとして
		 * メインオーディオと同じトラックを指定した場合もサブオーディオからは何も再生されません。<br>
		 *
		 * \par 備考:
		 * 再生中のトラック変更は未対応です。変更前のフレーム番号を記録しておいてシーク再生
		 * してください。
		 */
		public void SetSubAudioTrack(int track)
		{
			CRIWARE1A1C5A54(playerId, track);
		}


		public void SetSubAudioTrack(AudioTrack track)
		{
			if (track == AudioTrack.Off) {
				CRIWARE1A1C5A54(playerId, -1);
			} else if (track == AudioTrack.Auto) {
				CRIWARE1A1C5A54(playerId, 100);
			}
		}


		/**
		 * <summary>エクストラオーディオトラック番号の設定を行います。</summary>
		 * <param name="track">トラック番号</param>
		 * \par 説明:
		 * ムービが複数のオーディオトラックを持っている場合に、再生するオーディオを指定します。<br>
		 * 本関数を実行しなかった場合（デフォルト設定）はエクストラオーディオからは何も再生されません。
		 * また、データが存在しないトラック番号を指定した場合、エクストラオーディオトラックとして
		 * メインオーディオと同じトラックを指定した場合もエクストラオーディオからは何も再生されません。<br>
		 *
		 * \par 備考:
		 * 再生中のトラック変更は未対応です。変更前のフレーム番号を記録しておいてシーク再生
		 * してください。
		 */
		public void SetExtraAudioTrack(int track)
		{
			CRIWARE7E9E57F6(playerId, track);
		}


		public void SetExtraAudioTrack(AudioTrack track)
		{
			if (track == AudioTrack.Off) {
				CRIWARE7E9E57F6(playerId, -1);
			} else if (track == AudioTrack.Auto) {
				CRIWARE7E9E57F6(playerId, 100);
			}
		}


		/**
		 * <summary>ムービ再生の音声ボリューム調整を行います。(メインオーディオトラック) </summary>
		 * <param name="volume">ボリューム</param>
		 * \par 説明:
		 * ムービのメインオーディオトラックの出力音声ボリュームを指定します。<br/>
		 * ボリューム値には、0.0f〜1.0fの範囲で実数値を指定します。<br/>
		 * ボリューム値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br/>
		 * 例えば、1.0fを指定した場合、原音はそのままのボリュームで出力されます。<br/>
		 * 0.0fを指定した場合、音声はミュートされます（無音になります）。<br/>
		 * ボリュームのデフォルト値は1.0fです。<br>
		 * \par 備考:
		 * 0.0f〜1.0fの範囲外を指定した場合は、それぞれの最小・最大値にクリップされます。<br/>
		 */
		public void SetVolume(float volume)
		{
			CRIWARE3EF01E23(playerId, volume);
		}


		/**
		 * <summary>ムービ再生の音声ボリューム調整を行います。（サブオーディオ）</summary>
		 * <param name="volume">ボリューム</param>
		 * \par 説明:
		 * ムービのサブオーディオトラックの出力音量ボリュームを指定します。<br/>
		 * ボリューム値には、0.0f〜1.0fの範囲で実数値を指定します。<br/>
		 * ボリューム値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br/>
		 * 例えば、1.0fを指定した場合、原音はそのままのボリュームで出力されます。<br/>
		 * 0.0fを指定した場合、音声はミュートされます（無音になります）。<br/>
		 * ボリュームのデフォルト値は1.0fです。<br>
		 * \par 備考:
		 * 0.0f〜1.0fの範囲外を指定した場合は、それぞれの最小・最大値にクリップされます。<br/>
		 */
		public void SetSubAudioVolume(float volume)
		{
			CRIWARE41FF07E6(playerId, volume);
		}


		/**
		 * <summary>ムービ再生の音声ボリューム調整を行います。（エクストラオーディオ）</summary>
		 * <param name="volume">ボリューム</param>
		 * \par 説明:
		 * ムービのエクストラオーディオトラックの出力音量ボリュームを指定します。<br/>
		 * ボリューム値には、0.0f〜1.0fの範囲で実数値を指定します。<br/>
		 * ボリューム値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br/>
		 * 例えば、1.0fを指定した場合、原音はそのままのボリュームで出力されます。<br/>
		 * 0.0fを指定した場合、音声はミュートされます（無音になります）。<br/>
		 * ボリュームのデフォルト値は1.0fです。<br>
		 * \par 備考:
		 * 0.0f〜1.0fの範囲外を指定した場合は、それぞれの最小・最大値にクリップされます。<br/>
		 */
		public void SetExtraAudioVolume(float volume)
		{
			CRIWAREC0215F00(playerId, volume);
		}


		/**
		 * <summary>メインオーディオトラックのバスセンドレベル調整を行います。</summary>
		 * <param name="bus_name">バス名</param>
		 * <param name="level">バスセンドレベル</param>
		 * \par 説明:
		 * ムービのメインオーディオトラックのバスセンドレベル（振幅レベル）と、センド先のバス名を指定します。<br>
		 * バスセンドレベル値には、0.0f〜1.0fの範囲で実数値を指定します。<br>
		 * バスセンドレベル値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br>
		 * 例えば、1.0fを指定した場合、原音はそのままのボリュームでバスに出力されます。<br>
		 * 0.0fを指定した場合、バスには無音が出力されます。<br>
		 */
		public void SetBusSendLevel(string bus_name, float level)
		{
			CRIWARE381773AA(playerId, bus_name, level);
		}


		/**
		 * <summary>サブオーディオトラックのバスセンドレベル調整を行います。</summary>
		 * <param name="bus_name">バス名</param>
		 * <param name="volume">バスセンドレベル</param>
		 * \par 説明:
		 * ムービのサブオーディオトラックのバスセンドレベル（振幅レベル）と、センド先のバス名を指定します。<br>
		 * 本関数の動作仕様は SetBusSendLevel と同様です。
		 */
		public void SetSubAudioBusSendLevel(string bus_name, float volume)
		{
			CRIWARE816E8379(playerId, bus_name, volume);
		}


		/**
		 * <summary>エクストラオーディオトラックのバスセンドレベル調整を行います。</summary>
		 * <param name="bus_name">バス名</param>
		 * <param name="volume">バスセンドレベル</param>
		 * \par 説明:
		 * ムービのエクストラオーディオトラックのバスセンドレベル（振幅レベル）と、センド先のバス名を指定します。<br>
		 * 本関数の動作仕様は SetBusSendLevel と同様です。
		 */
		public void SetExtraAudioBusSendLevel(string bus_name, float volume)
		{
			CRIWARE0DC35545(playerId, bus_name, volume);
		}


		/**
		 * <summary>字幕チャネルの設定を行ないます。</summary>
		 * <param name="channel">字幕チャネル番号</param>
		 * \par 説明:
		 * 取得する字幕チャネルを設定します。デフォルトは字幕取得無しです。<br/>
		 * デフォルト設定（字幕取得無し）にしたい場合は、チャネルとして -1 を指定してください。<br/>
		 * ムービ再生中に字幕チャンネルを切り替えることも可能です。ただし、実際のチャネルが切り替わるのは
		 * 設定した直後の次の字幕からとなります。<br/>
		 * \sa Player::subtitleBuffer, Player::subtitleBuffer
		 */
		public void SetSubtitleChannel(int channel)
		{
			enableSubtitle = (channel != -1);
			if (enableSubtitle) {
				if (isMovieInfoAvailable) {
					AllocateSubtitleBuffer((int)movieInfo.maxSubtitleSize);
				}
			} else {
				DeallocateSubtitleBuffer();
			}
			CRIWAREC7D0D2A2(playerId, channel);
		}


		/**
		 * <summary>シェーダ選択デリゲートの設定を行ないます。</summary>
		 * <param name="shaderDispatchCallback">シェーダ選択デリゲート</param>
		 * \par 説明:
		 * ユーザ定義シェーダを決定するデリゲートの設定を行ないます<br>
		 * \par 注意:
		 * Player::Prepare 関数および Player::Start 関数より前に設定する必要があります。
		 * \sa Player::ShaderDispatchCallback
		 */
		public void SetShaderDispatchCallback(ShaderDispatchCallback shaderDispatchCallback)
		{
			_shaderDispatchCallback = shaderDispatchCallback;
		}


		/**
		 * <summary>再生時刻の取得を行います。</summary>
		 * <returns>再生時刻 (単位:マイクロ秒)</returns>
		 * \par 説明:
		 * 現在再生中のムービの再生時刻を取得します。<br/>
		 * 再生開始前および再生停止後は、0 を返します。<br/>
		 * 単位はマイクロ秒です。
		 */
		public long GetTime()
		{
			return CRIWARE13E29EF1(this.playerId);
		}


		/**
		 * <summary>表示中のフレーム番号の取得を行います。</summary>
		 * <returns>フレーム番号</returns>
		 * \par 説明:
		 * 現在表示中のフレーム番号を取得します。<br/>
		 * 表示開始前および再生停止後は、-1 を返します。<br/>
		 * プラットフォームやコーデックにより、取得可能なフレーム情報よりも
		 * 前の番号が返される場合があります。<br/>
		 */
		public int GetDisplayedFrameNo()
		{
			return CRIWARE41E21EC8(this.playerId);
		}


		/**
		 * <summary>ASRラックの設定を行います。</summary>
		 * <param name="asrRackId">ASRラックID</param>
		 * \par 説明:
		 * 再生する音声の出力ASRラックIDを指定します。<br/>
		 * 出力ASRラックIDの設定はメインオーディオトラックとサブオーディオトラックの両方に反映されます。<br/>
		 * \par 注意:
		 * 再生中の変更はできません。再生中に本APIを呼び出した場合、次のムービ再生から設定が反映されます。<br/>
		 * \sa CriAtomExAsrRack::rackId
		 */
		public void SetAsrRackId(int asrRackId)
		{
			CRIWARE761EA766(this.playerId, asrRackId);
		}


#if CRIMANA_STAT
		float stat_DeltaPlayerUpdateTime = 0.0f;
		float stat_PrevPlayerUpdateTime = 0.0f;
		float stat_DeltaUpdateFrameTime = 0.0f;
		float stat_PrevUpdateFrameTime = 0.0f;
		float stat_DeltaRenderFrameTime = 0.0f;
		float stat_PrevRenderFrameTime = 0.0f;
		float stat_StartTime = 0.0f;
		float stat_ElapsedTimeToFirstFrame = 0.0f;
		uint stat_SkippedFrames = 0;
		uint stat_DroppedFrames = 0;

		UnityEngine.UI.Text stat_UI_PlayerUpdate;
		UnityEngine.UI.Text stat_UI_FrameUpdate;
		UnityEngine.UI.Text stat_UI_RenderUpdate;
		UnityEngine.UI.Text stat_UI_Status;
		UnityEngine.UI.Text stat_UI_Elapased;
		UnityEngine.UI.Text stat_UI_Skipped;
		UnityEngine.UI.Text stat_UI_Dropped;

		bool stat_Init = false;
		GameObject UIStats;

		public void UIInit()
		{
			stat_Init = true;
			int layer = 30;

			var g = new GameObject("CriStat");
			g.layer = layer;
			var c = g.AddComponent<Canvas>();

			var UICam = new GameObject("StatCamera");
			UICam.layer = layer;
			UICam.transform.position = new Vector3(0, 0, -615);
			UICam.transform.parent = g.transform;
			var cam = UICam.AddComponent<Camera>();
			cam.orthographic = true;
			cam.cullingMask = 1 << layer;
			cam.clearFlags = CameraClearFlags.Depth;
		   

			c.worldCamera = cam;
			c.renderMode = RenderMode.ScreenSpaceCamera;
			var cs = g.AddComponent<CanvasScaler>();
			cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

			g.AddComponent<GraphicRaycaster>();

			int w = 600, h = 400;
			int d = 15;
			var font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
			var color = Color.green;
			var size = new Vector2(w, d*2);
			var pivot = new Vector2(0, 0);
			var scale = new Vector3(1, 1, 1);

			UIStats = new GameObject("stat_UI_PlayerUpdate");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_PlayerUpdate = UIStats.AddComponent<Text>();
			stat_UI_PlayerUpdate.fontSize = d;
			stat_UI_PlayerUpdate.font = font;
			stat_UI_PlayerUpdate.alignment = TextAnchor.UpperLeft;
			stat_UI_PlayerUpdate.color = color;
			stat_UI_PlayerUpdate.rectTransform.sizeDelta = size;
			stat_UI_PlayerUpdate.rectTransform.pivot = pivot;
			stat_UI_PlayerUpdate.rectTransform.localScale = scale;
			stat_UI_PlayerUpdate.rectTransform.localPosition = new Vector3(-w/2, h/2, 0);

			UIStats = new GameObject("stat_UI_FrameUpdate");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_FrameUpdate = UIStats.AddComponent<Text>();
			stat_UI_FrameUpdate.fontSize = d;
			stat_UI_FrameUpdate.font = font;
			stat_UI_FrameUpdate.alignment = TextAnchor.UpperLeft;
			stat_UI_FrameUpdate.color = color;
			stat_UI_FrameUpdate.rectTransform.sizeDelta = size;
			stat_UI_FrameUpdate.rectTransform.pivot = pivot;
			stat_UI_FrameUpdate.rectTransform.localScale = scale;
			stat_UI_FrameUpdate.rectTransform.localPosition = new Vector3(-w/2, h/2 - d, 0);

			UIStats = new GameObject("stat_UI_RenderUpdate");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_RenderUpdate = UIStats.AddComponent<Text>();
			stat_UI_RenderUpdate.fontSize = d;
			stat_UI_RenderUpdate.font = font;
			stat_UI_RenderUpdate.alignment = TextAnchor.UpperLeft;
			stat_UI_RenderUpdate.color = color;
			stat_UI_RenderUpdate.rectTransform.sizeDelta = size;
			stat_UI_RenderUpdate.rectTransform.pivot = pivot;
			stat_UI_RenderUpdate.rectTransform.localScale = scale;
			stat_UI_RenderUpdate.rectTransform.localPosition = new Vector3(-w/2, h/2 - d*2, 0);

			UIStats = new GameObject("stat_UI_Status");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_Status = UIStats.AddComponent<Text>();
			stat_UI_Status.fontSize = d;
			stat_UI_Status.font = font;
			stat_UI_Status.alignment = TextAnchor.UpperLeft;
			stat_UI_Status.color = color;
			stat_UI_Status.rectTransform.sizeDelta = size;
			stat_UI_Status.rectTransform.pivot = pivot;
			stat_UI_Status.rectTransform.localScale = scale;
			stat_UI_Status.rectTransform.localPosition = new Vector3(-w/2, h/2 - d*3, 0);

			UIStats = new GameObject("stat_UI_Elapased");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_Elapased = UIStats.AddComponent<Text>();
			stat_UI_Elapased.fontSize = d;
			stat_UI_Elapased.font = font;
			stat_UI_Elapased.alignment = TextAnchor.UpperLeft;
			stat_UI_Elapased.color = color;
			stat_UI_Elapased.rectTransform.sizeDelta = size;
			stat_UI_Elapased.rectTransform.pivot = pivot;
			stat_UI_Elapased.rectTransform.localScale = scale;
			stat_UI_Elapased.rectTransform.localPosition = new Vector3(-w/2, h/2 - d*4, 0);

			UIStats = new GameObject("stat_UI_Skipped");
			UIStats.transform.parent = g.transform;
			UIStats.layer = layer;
			stat_UI_Skipped = UIStats.AddComponent<Text>();
			stat_UI_Skipped.fontSize = d;
			stat_UI_Skipped.font = font;
			stat_UI_Skipped.alignment = TextAnchor.UpperLeft;
			stat_UI_Skipped.color = color;
			stat_UI_Skipped.rectTransform.sizeDelta = size;
			stat_UI_Skipped.rectTransform.pivot = pivot;
			stat_UI_Skipped.rectTransform.localScale = scale;
			stat_UI_Skipped.rectTransform.localPosition = new Vector3(-w/2, h/2 - d*5, 0);
		}

		public void UIUpdate()
		{
			float msec = stat_DeltaPlayerUpdateTime * 1000.0f;
			float fps = 1.0f / stat_DeltaPlayerUpdateTime;
			string text = string.Format("Mana Player Update: {0:0.0} ms ({1:0.} fps)", msec, fps);
			stat_UI_PlayerUpdate.text = text;

			msec = stat_DeltaUpdateFrameTime * 1000.0f;
			fps = 1.0f / stat_DeltaUpdateFrameTime;
			text = string.Format("Mana Update Frame: {0:0.0} ms ({1:0.} fps)", msec, fps);
			stat_UI_FrameUpdate.text = text;

			msec = stat_DeltaRenderFrameTime * 1000.0f;
			fps = 1.0f / stat_DeltaRenderFrameTime;
			text = string.Format("Mana Render Frame: {0:0.0} ms ({1:0.} fps)", msec, fps);
			stat_UI_RenderUpdate.text = text;

			text = string.Format("Mana Player Status: {0}", nativeStatus.ToString());
			stat_UI_Status.text = text;

			float elapsed = 0.0f;
			if (stat_StartTime > float.Epsilon) {
				elapsed = Time.time - stat_StartTime;
			}
			float frames = 0.0f;
			if (movieInfo != null) {
				frames = elapsed * (movieInfo.framerateN / movieInfo.framerateD);
			}
			msec = stat_ElapsedTimeToFirstFrame * 1000.0f;
			text = string.Format("Mana Elapsed Time: {0:0.0} sec ({1:0.} frames) [Start Gap {2:0.0}ms]", elapsed, frames, msec);
			stat_UI_Elapased.text = text;

			uint skipped = stat_SkippedFrames;
			uint dropped = stat_DroppedFrames;
			text = string.Format("Mana Skipped Frames: {0} frames, Dropped Frames: {1} frames", skipped, dropped);
			stat_UI_Skipped.text = text;

			stat_DeltaPlayerUpdateTime = Time.time - stat_PrevPlayerUpdateTime;
			stat_PrevPlayerUpdateTime = Time.time;
			stat_DeltaUpdateFrameTime = 0.0f;
		}
#endif


		/**
		 * <summary>ユーザー時刻を指定して状態を更新します。</summary>
		 * \par 説明:
		 * プレーヤの状態を更新します。<br>
		 * ::CriMana::Player::SetMasterTimerType でユーザータイマーを有効にした場合、本関数を
		 * 定期的に呼び出してください。
		 * \par 注意:
		 * 本関数の呼び出しが滞るとムービ再生ががたつく場合があります。<br/>
		 * ユーザタイマを早送り又はスローで進めるとマスタタイマの精度が悪くなり、
		 * 正しい時刻でフレームを取得しにくくなるため、通常は等倍速再生で
		 * 本関数をご使用ください。
		 * \sa Player::SetMasterTimerType
		 */
		public void UpdateWithUserTime(ulong timeCount, ulong timeUnit)
		{
			if (timerType != TimerType.User) {
				Debug.LogError("[CRIWARE] Timer type is invalid.");
			}

			criManaUnityPlayer_SetUserTime(this.playerId, timeCount, timeUnit);
			InternalUpdate();
		}


		/**
		 * <summary>マニュアルタイマーの時刻が進む単位の設定します。</summary>
		 * <param name="timeUnitN">時刻が進む単位の分子</param>
		 * <param name="timeUnitD">時刻が進む単位の分母</param>
		 * \par 説明:
		 * マニュアルタイマーの時刻が進む単位を有理数形式で設定します。<br>
		 * ::CriMana::Player::SetMasterTimerType でマニュアルタイマ―ーを有効にした場合、本関数で
		 * 時刻が進む単位の設定を行ってください。<br>
		 * 「分子(timer_manual_unit_n) / 分母(timer_manual_unit_d) = 時刻が進む単位（秒）」になります。<br>
		 * 例えば 29.97fps を設定したい場合、1001 と 30000 を指定してください。
		 * \sa
		 * Player::SetMasterTimerType, Player::UpdateWithManualTimeAdvanced
		 */
		public void SetManualTimerUnit(ulong timeUnitN, ulong timeUnitD)
		{
			if (timerType != TimerType.Manual) {
				Debug.LogError("[CRIWARE] Timer type is invalid.");
			}

			criManaUnityPlayer_SetManualTimerUnit(this.playerId, timeUnitN, timeUnitD);
		}


		/**
		 * <summary>マニュアルタイマーの時刻を進めつつ更新を行います。</summary>
		 * \par 説明:
		 * マニュアルタイマ―でハンドルの時刻を進め、描画の更新を行います。<br>
		 * ::CriMana::Player::SetMasterTimerType でマニュアルタイマ―ーを有効にした場合、本関数を定期的に
		 * 呼び出す必要があります。<br>
		 * プレーヤステータスがPLAYING（再生中）の時に時刻が進みます。<br>
		 * ポーズ中は本関数を呼び出しても時刻は進まず、再生の開始や停止時に時刻は0にリセットされます。
		 * \sa
		 * Player::SetMasterTimerType, Player::SetManualTimerUnit
		 */
		public void UpdateWithManualTimeAdvanced()
		{
			if (timerType != TimerType.Manual) {
				Debug.LogError("[CRIWARE] Timer type is invalid.");
			}

			criManaUnityPlayer_AdvanceManualTimer(this.playerId);
			InternalUpdate();
		}


		/**
		 * <summary>状態を更新します。</summary>
		 * \par 説明:
		 * プレーヤの状態を更新します。<br>
		 * アプリケーションは、この関数を定期的に実行する必要があります。<br>
		 * \par 注意:
		 * 本関数の呼び出しが滞るとムービ再生ががたつく場合があります。
		 */
		public void Update()
		{
			if (timerType == TimerType.User || timerType == TimerType.Manual) {
				return;
			}

			InternalUpdate();
		}


		public void OnWillRenderObject(CriManaMovieMaterial sender)
		{
			if (rendererResource != null &&
				nativeStatus == Status.Playing) {
				// on main thread
				// unfortunatly this is called on main thread before native render event occur on render thread!
				rendererResource.UpdateTextures();
				// Native update - on render thread
				IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.RENDER);
#if CRIMANA_STAT
				stat_DeltaRenderFrameTime = Time.time - stat_PrevRenderFrameTime;
				stat_PrevRenderFrameTime = Time.time;
				if (stat_ElapsedTimeToFirstFrame == 0.0f && status == Status.Playing) {
					stat_ElapsedTimeToFirstFrame = Time.time - stat_StartTime;
				}
#endif
			}
		}

		/**
		 * <summary>マテリアルに現在のフレームを表示するための設定を行います。</summary>
		 * <param name="material">設定対象のマテリアル</param>
		 * <returns>設定を行なえたか</returns>
		 * \par 説明:
		 * マテリアルに現在のフレームを表示するためにシェーダや各種パラメータを設定します。<br>
		 */
		public bool UpdateMaterial(UnityEngine.Material material)
		{
			if ((rendererResource != null) && isFrameInfoAvailable) {
				bool result = rendererResource.UpdateMaterial(material);
				return result && (requiredStatus != Status.ReadyForRendering);
			}
			return false;
		}

		public void IssuePluginEvent(CriManaUnityPlayer_RenderEventAction renderEventAction)
		{
#if !CRIWARE_ENABLE_HEADLESS_MODE
			int eventID = CriManaPlugin.renderingEventOffset | (int)renderEventAction | this.playerId;
	#if CRIPLUGIN_USE_OLD_LOWLEVEL_INTERFACE
			UnityEngine.GL.IssuePluginEvent(eventID);
	#else
			UnityEngine.GL.IssuePluginEvent(criWareUnity_GetRenderEventFunc(), eventID);
	#endif
#endif
		}

		#region Private Methods
		private void Dispose(bool disposing)
		{
			if (isDisposed) { return; }
			isDisposed = true;

			CriDisposableObjectManager.Unregister(this);

			int numFrames = 0;

			if (rendererResource != null && playerId != InvalidPlayerId) {
				// signal to native plugin that it will be destroyed and begin to clean unmanaged resources.
				IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.DESTROY);
				// get the number of frame to continue to send update events before unmamaged resoures destruction. 
				numFrames = rendererResource.GetNumberOfFrameBeforeDestroy(playerId);
			}

			DisposeRendererResource();

			if (playerId != InvalidPlayerId) {
				if (atomExPlayer != null) {
					_atomExPlayer.Dispose();
					_atomExPlayer = null;
				}
				if (atomEx3DsourceForAmbisonics != null) {
					_atomEx3Dsource.Dispose();
					_atomEx3Dsource = null;
				}
				CRIWARED8A3F46C(playerId);
			}

			// if unmamaged resource need some frames before be destroyed,
			// we signal here Unity frame update events to native plugin.
			if (playerHolder != null) {
				if (numFrames > 0) {
					playerHolder.StartCoroutine(IssuePluginUpdatesForFrames(numFrames, playerHolder, true));
				} else {
					// destroy holder
					UnityEngine.Object.Destroy(playerHolder.gameObject);
				}
			}

			DeallocateSubtitleBuffer();
			cuePointCallback = null;
		}

		private void InternalUpdate()
		{
#if CRIMANA_STAT
			if (!stat_Init) {
				UIInit();
			}
			UIUpdate();
#endif
			// signal unity frame update to plugin - always called once by unity update
			CRIWAREF5FF04D4(playerId);

			if (requiredStatus == Status.Stop) {
				if (nativeStatus != Status.Stop) {
					UpdateNativePlayer();
				}
				return;
			}

			switch (nativeStatus) {
			case Status.Stop:
				break;
			case Status.Dechead:
#if CRIMANA_STAT
				stat_StartTime = Time.time;
				stat_ElapsedTimeToFirstFrame = 0.0f;
#endif
				UpdateNativePlayer();
				if (nativeStatus == Status.WaitPrep) {
					goto case Status.WaitPrep;
				}
				break;
			case Status.WaitPrep:
			{
				bool needRendererResourceDispatch = !isMovieInfoAvailable;
				if (needRendererResourceDispatch) {
					CRIWARE181DFF3C(playerId, _movieInfo);
					isMovieInfoAvailable = true;
					if (enableSubtitle) {
						AllocateSubtitleBuffer((int)movieInfo.maxSubtitleSize);
					}
					UnityEngine.Shader userShader	= (_shaderDispatchCallback == null) ? null : _shaderDispatchCallback(movieInfo, additiveMode);
					if (rendererResource != null) {
						if (!rendererResource.IsSuitable(playerId, _movieInfo, additiveMode, userShader)) {
							rendererResource.Dispose();
							rendererResource = null;
						}
					}
					if (rendererResource == null) {
						rendererResource = Detail.RendererResourceFactory.DispatchAndCreate(playerId, _movieInfo, additiveMode, userShader);
						if (rendererResource == null) {
							Stop();
							return;
						}
					}
					rendererResource.SetApplyTargetAlpha(applyTargetAlpha);
				}
				rendererResource.AttachToPlayer(playerId);

				if (requiredStatus == Status.Ready) {
					goto case Status.Prep;
				}

				if (requiredStatus == Status.Playing ||
					requiredStatus == Status.ReadyForRendering) {
					CRIWARE1711CB6B(playerId);
					isNativeStartInvoked = true;
					if (isNativeInitialized) {
						// Native destroy - on render thread
						IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.DESTROY);
					}
					// Native initialize - on render thread - must be called after start (iOS)
					IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.INITIALIZE);
					isNativeInitialized = true;
					goto case Status.Prep;
				}
			}
				break;
			case Status.Prep:
				UpdateNativePlayer();
				if (nativeStatus == Status.Ready) {
					goto case Status.Ready;
				} else if (nativeStatus == Status.Playing) {
					goto case Status.Playing;
				}
				break;
			case Status.Ready:
				if (requiredStatus == Status.Playing ||
					requiredStatus == Status.ReadyForRendering) {
					if (!isNativeStartInvoked) {
#if CRIMANA_STAT
						stat_StartTime = Time.time;
						stat_ElapsedTimeToFirstFrame = 0.0f;
#endif
						CRIWARE1711CB6B(playerId);
						isNativeStartInvoked = true;
						if (isNativeInitialized) {
							// Native destroy - on render thread
							IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.DESTROY);
						}
						// Native initialize - on render thread - must be called after start (iOS)
						IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.INITIALIZE);
						isNativeInitialized = true;
					}
					goto case Status.Playing;
				}
				break;
			case Status.Playing:
				UpdateNativePlayer();
				if (nativeStatus == Status.Playing) {
					bool frameDrop = (maxFrameDrop < 0 || droppedFrameCount < maxFrameDrop); // enable frame dropping if requested 
					bool updateNextFrame = true;
					bool isFrameInfoUpdated = false;

					while (updateNextFrame) {
						// FrameDrop refrence -> in (can drop frame?), out (is frame has been droped?)
						isFrameInfoUpdated = rendererResource.UpdateFrame(playerId, _frameInfo, ref frameDrop);

						if (isFrameInfoUpdated && frameDrop && (maxFrameDrop < 0 || droppedFrameCount < maxFrameDrop)) {
							// Frame was updated but not in-sync and dropped, try to get next decoded frame: continue
							droppedFrameCount++;

							if (maxFrameDrop > 0 && droppedFrameCount == maxFrameDrop) {
								frameDrop = false; // do not drop next frame.
							}
						} else {
							// Frame has not been updated or is in-sync: break.
							updateNextFrame = false;

							// Frame is drawable: reset the dropped frame counter.
							if (isFrameInfoUpdated) {
#if CRIMANA_STAT
								stat_DroppedFrames += droppedFrameCount;
#endif
								droppedFrameCount = 0;
							}
						}
					}

					// FrameInfo was updated?
					isFrameInfoAvailable |= isFrameInfoUpdated;

					// Native update - on render thread
					IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.UPDATE);
#if CRIMANA_STAT
					stat_DeltaUpdateFrameTime = Time.time - stat_PrevUpdateFrameTime;
					stat_PrevUpdateFrameTime = Time.time;
					stat_SkippedFrames = frameInfo.cntSkippedFrames;
#endif
				} else if (nativeStatus == Status.PlayEnd) {
					goto case Status.PlayEnd;
				}
				break;
			case Status.PlayEnd:
				break;
			case Status.Error:
				UpdateNativePlayer();
				break;
			}

			if (nativeStatus == Status.Error) {
				DisableInfos();
			}
		}

		private System.Collections.IEnumerator IssuePluginUpdatesForFrames(int frameCount, MonoBehaviour playerHolder, bool destroy)
		{
			while (frameCount > 0) {
				// issue plugin event
				IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.UPDATE);
				// continue update synchronizatons
				CRIWAREF5FF04D4(playerId);
				frameCount--;
				yield return null;
			}

			if (destroy) {
				// destroy holder
				UnityEngine.Object.Destroy(playerHolder.gameObject);
			}
		}

		private void DisableInfos()
		{
			isMovieInfoAvailable = false;
			isFrameInfoAvailable = false;
			isNativeStartInvoked = false;
			subtitleSize = 0;
		}

		private void PrepareNativePlayer()
		{
			if (cuePointCallback != null) {
				#if CRIMANA_PLAYER_SUPPORT_CUEPOINT_CALLBACK
				CRIWAREA627576B(playerId, CuePointCallbackFromNative);
				#else
				UnityEngine.Debug.LogError("This platform does not support event callback feature.");
				#endif
			}
			CRIWARE809BDF35(playerId);
		}


		private void UpdateNativePlayer()
		{
#if !CRIWARE_ENABLE_HEADLESS_MODE
			updatingPlayer = this;
			uint subtitleSizeTmp = (uint)subtitleBufferSize;
			nativeStatus = (Status)CRIWAREE9E294B1(playerId, subtitleBuffer, ref subtitleSizeTmp);
			subtitleSize = (int)subtitleSizeTmp;
			updatingPlayer = null;

			// check when status goes from any to stop
			if (isNativeInitialized && (
					nativeStatus == Status.StopProcessing ||
					nativeStatus == Status.Stop))
			{
				isNativeInitialized = false;
				// Native destroy - on render thread
				if (!isStoppingForSeek ||
					!(rendererResource != null && rendererResource.ShouldSkipDestroyOnStopForSeek())) {
					IssuePluginEvent(CriManaUnityPlayer_RenderEventAction.DESTROY);
				}
			}
#else
			if ((requiredStatus != Status.Stop) ||
				(nativeStatus != requiredStatus)) {
				DummyProceedNativeState();
			}
#endif
		}


#if CRIWARE_ENABLE_HEADLESS_MODE
		private void DummyProceedNativeState()
		{
			if (nativeStatus == requiredStatus) {
				if (nativeStatus == Status.Playing &&
	 				nativeStatus == requiredStatus &&
					!_dummyLoop && !_dummyPaused) {
					nativeStatus = Status.PlayEnd;
				}
			} else if (requiredStatus == Status.Playing ||
				requiredStatus == Status.Ready) {
				if (_dummyNativeStatus == nativeStatus + 1 &&
					!(_dummyPaused && _dummyNativeStatus == Status.Playing)) {
					nativeStatus = _dummyNativeStatus;
				} else {
					_dummyNativeStatus = nativeStatus + 1;
				}
			} else if (requiredStatus == _dummyNativeStatus) {
				nativeStatus = requiredStatus;
			} else {
				_dummyNativeStatus = requiredStatus;
			}
		}

#endif


		private void AllocateSubtitleBuffer(int size)
		{
			if (subtitleBufferSize < size) {
				DeallocateSubtitleBuffer();
				subtitleBuffer = Marshal.AllocHGlobal(size);
				subtitleBufferSize = size;
				subtitleSize = 0;
			}
			for (int i = 0; i < subtitleBufferSize; i++) {
				Marshal.WriteByte(subtitleBuffer, i, 0x00);
			}
		}

		private void DeallocateSubtitleBuffer()
		{
			if (subtitleBuffer != System.IntPtr.Zero) {
				Marshal.FreeHGlobal(subtitleBuffer);
				subtitleBuffer = System.IntPtr.Zero;
				subtitleBufferSize = 0;
				subtitleSize = 0;
			}
		}


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void CuePointCallbackFromNativeDelegate(System.IntPtr ptr1, System.IntPtr ptr2, [In] ref EventPoint eventPoint);


		[AOT.MonoPInvokeCallback(typeof(CuePointCallbackFromNativeDelegate))]
		private static void CuePointCallbackFromNative(System.IntPtr ptr1, System.IntPtr ptr2, [In] ref EventPoint eventPoint)
		{
			if (updatingPlayer.cuePointCallback != null) {
				updatingPlayer.cuePointCallback(ref eventPoint);
			}
		}
		#endregion

		#region DLL Import
		#if !CRIWARE_ENABLE_HEADLESS_MODE
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern int CRIWARE83C6547D();
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern int CRIWARE96C9B3EF();
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARED8A3F46C(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE697736B9(int player_id, System.IntPtr binder, string path);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE6E81E5C7(int player_id, System.IntPtr binder, int content_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREB50C7533(int player_id, string path, System.UInt64 offset, System.Int64 range);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE72CBF76A(int player_id, byte[] data, System.Int64 datasize);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWAREA2D9C574(int player_id, System.IntPtr binder, string path, bool repeat);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWAREC317A335(int player_id, System.IntPtr binder, int content_id, bool repeat);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWARE1564F389(int player_id, string path, System.UInt64 offset, System.Int64 range, bool repeat);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWARE383926E8(int player_id, byte[] data, System.Int64 datasize, bool repeat);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE2AD7B954(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern System.Int32 CRIWARE54A7B6FE(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREA627576B(
			int player_id,
			CuePointCallbackFromNativeDelegate cbfunc
			);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE181DFF3C(int player_id, [Out] MovieInfo movie_info);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern int CRIWAREE9E294B1(
			int player_id,
			System.IntPtr subtitle_buffer,
			ref uint subtitle_size
			);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE809BDF35(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE1711CB6B(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREE8255B60(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE247881E0(int player_id, int seek_frame_no);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREF3E8400F(int player_id, MovieEventSyncMode mode);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE8ABAD03F(int player_id, int sw);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWARE48866453(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREA8119B22(int player_id, int sw);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern long CRIWARE13E29EF1(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern int CRIWARE5E04E5E8(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern IntPtr CRIWARE8FE20FE9(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern int CRIWARE41E21EC8(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE452F4E29(int player_id, int track);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE3EF01E23(int player_id, float vol);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE1A1C5A54(int player_id, int track);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE41FF07E6(int player_id, float vol);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE7E9E57F6(int player_id, int track);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREC0215F00(int player_id, float vol);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE381773AA(int player_id, string bus_name, float level);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE816E8379(int player_id, string bus_name, float level);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE0DC35545(int player_id, string bus_name, float level);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREC7D0D2A2(int player_id, int channel);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREB1595578(int player_id, float speed);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWARE19485E17(int player_id, System.UInt32 max_data_size);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		public static extern void CRIWAREE723C4CF(int player_id, float sec);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		public static extern void CRIWARE686BF411(int player_id, int min_buffer_size);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		public static extern void CRIWARE761EA766(int player_id, int asr_rack_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void CRIWAREF5FF04D4(int player_id);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void criManaUnityPlayer_SetMasterTimerType(int player_id, TimerType timer_type);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void criManaUnityPlayer_SetUserTime(int player_id, ulong user_count, ulong user_unit);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void criManaUnityPlayer_SetManualTimerUnit(int player_id, ulong timer_unit_n, ulong timer_unit_d);
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern void criManaUnityPlayer_AdvanceManualTimer(int player_id);
#if !CRIPLUGIN_USE_OLD_LOWLEVEL_INTERFACE
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern IntPtr criWareUnity_GetRenderEventFunc();
#endif
		#else
		private Status _dummyNativeStatus = Status.Error;
		private bool _dummyPaused = false;
		private bool _dummyLoop = false;
		private static int CRIWARE83C6547D() { return 0; }
		private static int CRIWARE96C9B3EF() { return 0; }
		private static void CRIWARED8A3F46C(int player_id) { }
		private static void CRIWARE697736B9(int player_id, System.IntPtr binder, string path) { }
		private static void CRIWARE6E81E5C7(int player_id, System.IntPtr binder, int content_id) { }
		private static void CRIWAREB50C7533(int player_id, string path, System.UInt64 offset, System.Int64 range) { }
		private static void CRIWARE72CBF76A(int player_id, byte[] data, System.Int64 datasize) { }
		private static bool CRIWAREA2D9C574(int player_id, System.IntPtr binder, string path, bool repeat) { return true; }
		private static bool CRIWAREC317A335(int player_id, System.IntPtr binder, int content_id, bool repeat) { return true; }
		private static bool CRIWARE1564F389(int player_id, string path, System.UInt64 offset, System.Int64 range, bool repeat) { return true; }
		private static bool CRIWARE383926E8(int player_id, byte[] data, System.Int64 datasize, bool repeat) { return true; }
		private static void CRIWARE2AD7B954(int player_id) { }
		private static System.Int32 CRIWARE54A7B6FE(int player_id) { return 0; }
		private static void CRIWAREA627576B(int player_id, CuePointCallbackFromNativeDelegate cbfunc) { }
		private static void CRIWARE181DFF3C(int player_id, [Out] MovieInfo movie_info) { movie_info = new MovieInfo(); }
		private static int CRIWAREE9E294B1(int player_id, System.IntPtr subtitle_buffer, ref uint subtitle_size ) { return 0; }
		private void CRIWARE809BDF35(int player_id) { _dummyPaused = false; nativeStatus = Status.WaitPrep; }
		private void CRIWARE1711CB6B(int player_id) { _dummyPaused = false; nativeStatus = Status.WaitPrep; }
		private void CRIWAREE8255B60(int player_id) { nativeStatus = Status.Stop;  }
		private static void CRIWARE247881E0(int player_id, int seek_frame_no) { }
		private static void CRIWAREF3E8400F(int player_id, MovieEventSyncMode mode){}
		private void CRIWARE8ABAD03F(int player_id, int sw) { _dummyPaused = (sw == 1); }
		private static bool CRIWARE48866453(int player_id) { return false; }
		private void CRIWAREA8119B22(int player_id, int sw) { _dummyLoop = (sw == 1); }
		private static long CRIWARE13E29EF1(int player_id) { return 0; }
		private static int CRIWARE5E04E5E8(int player_id) { return 0; }
		private static IntPtr CRIWARE8FE20FE9(int player_id) { return new IntPtr(1); }
		private static int CRIWARE41E21EC8(int player_id) { return -1; }
		private static void CRIWARE452F4E29(int player_id, int track) { }
		private static void CRIWARE3EF01E23(int player_id, float vol) { }
		private static void CRIWARE1A1C5A54(int player_id, int track) { }
		private static void CRIWARE41FF07E6(int player_id, float vol) { }
		private static void CRIWARE7E9E57F6(int player_id, int track) { }
		private static void CRIWAREC0215F00(int player_id, float vol) { }
		private static void CRIWARE381773AA(int player_id, string bus_name, float level) { }
		private static void CRIWARE816E8379(int player_id, string bus_name, float level) { }
		private static void CRIWARE0DC35545(int player_id, string bus_name, float level) { }
		private static void CRIWAREC7D0D2A2(int player_id, int channel) { }
		private static void CRIWAREB1595578(int player_id, float speed) { }
		private static void CRIWARE19485E17(int player_id, System.UInt32 max_data_size) { }
		public static void CRIWAREE723C4CF(int player_id, float sec) { }
		public static void CRIWARE686BF411(int player_id, int min_buffer_size) { }
		public static void CRIWARE761EA766(int player_id, int asr_rack_id) { }
		private static void CRIWAREF5FF04D4(int player_id) { }
		private static void criManaUnityPlayer_SetMasterTimerType(int player_id, TimerType timer_type) { }
		private static void criManaUnityPlayer_SetUserTime(int player_id, ulong user_count, ulong user_unit) { }
		private static void criManaUnityPlayer_SetManualTimerUnit(int player_id, ulong timer_unit_n, ulong timer_unit_d) { }
		private static void criManaUnityPlayer_AdvanceManualTimer(int player_id) { }
		#endif

		public enum CriManaUnityPlayer_RenderEventAction {
			UPDATE = 0, // default action - always called at each loop
			INITIALIZE  = 1 << 8, // called only once at first
			RENDER = 2 << 8, // called each loop each camera before render if visible
			DESTROY = 3 << 8, // called only once at end
		};

		#endregion
	}
}

/**
 * @}
 */


/* end of file */
