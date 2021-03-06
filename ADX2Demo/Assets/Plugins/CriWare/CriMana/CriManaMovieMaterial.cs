﻿/****************************************************************************
 *
 * Copyright (c) 2015 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using UnityEngine;


/**
 * \addtogroup CRIMANA_UNITY_COMPONENT
 * @{
 */


/**
 * <summary>ムービをマテリアルに流し込むためのコンポーネントです。</summary>
 * \par 説明:
 * ムービをマテリアルに流し込むためのコンポーネントです。<br/>
 * ムービ再生、描画コンポーネントの基本クラスです。<br/>
 * 本クラスを継承することで、任意の描画対象にムービを描画することが可能です。<br/>
 * 本コンポーネントはムービをマテリアルに流し込むだけなので、貼り付けて使用しても何も表示されません。<br/>
 * 通常はムービを表示したいオブジェクトにあわせて、 CriManaMovieController や、 CriManaMovieControllerForUI コンポーネントを使用してください。<br/>
 * \par 注意:
 * 本クラスでは、再生・停止・ポーズの基本操作しか行えません。<br/>
 * 複雑な再生制御を行う場合は、playerプロパティでコアプレーヤに対して操作を行って下さい。<br/>
 */
[AddComponentMenu("CRIWARE/CriManaMovieMaterial")]
public class CriManaMovieMaterial : CriMonoBehaviour
{
	#region Properties
	/**
	 * <summary>Start 時のストリーミング再生用のファイルパスです。</summary>
	 * <param name="filePath">ファイルパス</param>
	 * \par 説明:
	 * Start 時のストリーミング再生用のファイルパスです。<br/>
	 * - 相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。
	 * - 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。
	 * \par 注意:
	 * スクリプトからのファイルパス設定は、 player プロパティの CriMana::Player::SetFile メソッドなどで行なってください。<br/>
	 * 本プロパティはインスペクタ上で Start 時のストリーミング再生用のファイルパスを設定するために用意されています。<br/>
	 * Start より後に本プロパティを変更しても次回の再生には適用されません。
	 */
	public string moviePath {
		get { return _moviePath; }
		set {
			if (isMonoBehaviourStartCalled) {
				Debug.LogError("[CRIWARE] moviePath can not be changed. Use CriMana::Player::SetFile method.");
			} else {
				_moviePath = value;
			}
		}
	}

	/**
	 * <summary>Start 時に再生を行うかを設定します。</summary>
	 * \par 説明:
	 * Start 時に再生を行うかを設定します。デフォルトは false です。
	 */
	public bool		playOnStart		= false;

	/**
	 * <summary>Start 時のムービ再生のループ設定です。</summary>
	 * \par 説明:
	 * Start 時のムービ再生のループ設定です。デフォルトは false です。
	 * \par 注意:
	 * スクリプトからのループ設定は、 player プロパティの CriMana::Player::Loop メソッドで行なってください。<br/>
	 * 本プロパティはインスペクタ上で Start 時のループ設定を行なうために用意されています。<br/>
	 * Start より後に本プロパティを変更しても次回の再生には適用されません。
	 */
	public bool loop {
		get { return _loop; }
		set {
			if (isMonoBehaviourStartCalled) {
				Debug.LogError("[CRIWARE] loop property can not be changed. Use CriMana::Player::Loop method.");
			}
			else {
				_loop = value;
			}
		}
	}

	/**
	 * <summary>最大フレームドロップ数種別</summary>
	 */
	public enum MaxFrameDrop
	{
		Disabled = 0,
		One = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Infinite = -1
	};

	/**
	 * <summary>最大フレームドロップ数を設定します。</summary>
	 * \par 説明:
	 * 再生に対して描画フレームの更新が間に合っていない場合に一度の更新でドロップする
	 * フレームの最大数を設定します。<br/>
	 * アプリケーションのフレームレートが低下している場合や、フレームレート以上に
	 * 動画の再生速度を上げた場合に、意図通りに再生が行えるようになります。
	 */
	public MaxFrameDrop maxFrameDrop
	{
		get { return _maxFrameDrop; }
		set {
			_maxFrameDrop = value;
			if (player != null) {
				player.maxFrameDrop = (int)_maxFrameDrop;
			}
		}
	}

	/**
	 * <summary> Advanced Audio モードに切り替えます。</summary>
	 * \par 説明:
	 * Advanced Audio モードを有効化することで高度な音声再生機能が使用可能になります。デフォルト値は false です。
	 * 例えば Ambisonic 音声つきムービを再生する場合は、本モードを有効にしておく必要があります。
	 */
	public bool advancedAudio
	{
		get { return _advancedAudio; }
		set
		{
			if (isMonoBehaviourStartCalled)
			{
				Debug.LogError("[CRIWARE] advancedAudio property can not be changed in running.");
			}
			else
			{
				if (value == false) {
					ambisonics = false;
				}
				_advancedAudio = value;
			}
		}
	}


	/**
	 * <summary> Ambisonic 音声つきムービを再生可能な状態にします </summary>
	 * \par 説明:
	 * Ambisonic 音声つきムービを再生可能な状態にするためのプロパティです。Advanced Audio モードが有効な時にだけ使用可能です。
	 * \par 注意:
	 * 本モードの有効化時、Ambisonic Source という GameObject が、子オブジェクトとして作成されます。
	 * この Ambisonic Source オブジェクトには CriManaAmbisonicSource コンポーネントがアタッチされます。
	 */
	public bool ambisonics
	{
		get { return _ambisonics; }
		set
		{
			if (isMonoBehaviourStartCalled)
			{
				Debug.LogError("[CRIWARE] ambisonics property can not be changed in running.");
			}
			else if (_advancedAudio != true) {
				Debug.LogError("[CRIWARE] ambisonics property needs for advancedAudio property to be true.");
			}
			else
			{
				/*  Advanced Audio モードの ON/OFF 切替時に、Ambisonic Source オブジェクトを 生成/破棄 */
				if (value == false)
				{
					GameObject obj = null;
					if (gameObject.transform.childCount > 0)
					{
						/* 自分の子オブジェクトである "Ambisonic Source" を破棄する */
						obj = (ambisonicSource != null) ? ambisonicSource : gameObject.transform.Find("Ambisonic Source").gameObject;
						if (obj != null)
						{
							DestroyImmediate(obj);
							obj = null;
						}
					}
				}
				else
				{
					if (ambisonicSource == null)
					{
						ambisonicSource = new GameObject();
						ambisonicSource.name = "Ambisonic Source";
						ambisonicSource.transform.parent = gameObject.transform;
						ambisonicSource.transform.position = gameObject.transform.position;
						ambisonicSource.transform.rotation = gameObject.transform.rotation;
						ambisonicSource.transform.localScale = gameObject.transform.localScale;
						ambisonicSource.AddComponent<CriManaAmbisonicSource>();
					}
				}
				_ambisonics = value;
			}
		}
	}


	/**
	 * <summary>Start 時の加算合成モード設定です。</summary>
	 * \par 説明:
	 * Start 時の加算合成モード設定です。デフォルトは false です。
	 * \par 注意:
	 * スクリプトからの加算合成モード設定は、 player プロパティの CriMana::Player::additiveMode プロパティで行なってください。<br/>
	 * 本プロパティはインスペクタ上で Start 時の加算合成モード設定を行なうために用意されています。<br/>
	 * Start より後に本プロパティを変更しても次回の再生には適用されません。
	 */
	public bool additiveMode {
		get { return _additiveMode; }
		set {
			if (isMonoBehaviourStartCalled) {
				Debug.LogError("[CRIWARE] additiveMode can not be changed. Use CriMana::Player::additiveMode method.");
			}
			else {
				_additiveMode = value;
			}
		}
	}

	/**
	  * <summary>オブジェクトの透過度を適用するかを設定します。</summary>
	  * \par 説明:
	  * アタッチしたオブジェクトの透過度に応じて、ムービも透過させるかを設定します。デフォルト値は false です。
	  */
	public bool applyTargetAlpha {
		get { return _applyTargetAlpha; }
		set {
			if (isMonoBehaviourStartCalled) {
				Debug.LogError("[CRIWARE] applyTargetAlpha property can not be changed in running.");
			} else {
				_applyTargetAlpha = value;
			}
		}
	}

	/**
	 * <summary>CriManaMovieMaterial::material でムービフレームが描画できるかどうか</summary>
	 * \par 説明:
	 * CriManaMovieMaterial::material でムービフレームが描画できるかどうかです。
	 */
	public bool isMaterialAvailable { get; private set; }

	/**
	 * <summary>再生制御プレーヤ</summary>
	 * \par 説明:
	 * ムービの細やかな再生制御を行うためのプレーヤプロパティです。<br/>
	 * Start・Stop・Pause以外の操作を行いたい場合、本プロパティ経由で、 CriMana::Player APIを使用してください。
	 */
	public CriMana.Player player { get; private set; }

	/**
	 * <summary>ムービを流し込むマテリアルを設定します。</summary>
	 * \par 説明:
	 * マテリアルを設定すると、設定されたマテリアルにムービが流し込まれます。
	 * マテリアルを設定しない場合は、ムービを流し込むマテリアルを生成します。<br/>
	 * \par 注意:
	 * マテリアルを設定する場合、 Start メソッドが呼びだされる前に設定する必要があります。
	 */
	public Material material
	{
		get
		{
			return _material;
		}
		set
		{
			if (value != _material) {
				if (materialOwn) {
#if UNITY_EDITOR
					if (UnityEditor.EditorApplication.isPlaying == false) {
						DestroyImmediate(_material);
					}else{
						Destroy(_material);
					}
#else
					Destroy(_material);
#endif
					materialOwn = false;
				}
				_material = value;
				isMaterialAvailable = false;
			}
		}
	}

	/**
	* <summary>Material Render Mode</summary>
	* \par 説明:
	* The way we render the movie to the material<br/>
	* - Always: Render at each frame.<br/>
	* - OnVisibility: Render only when owner GameObject is visible (or, is active, for UI.Graphic). 
	*   This can provide an optimization if movie does not need to be updated to a material.<br/>
	* - Never: Never render movie to the material: you need to call "RenderMovie()" to control rendering.<br/>
	* \par 注意:
	* Atcually, on some platfrom the movie is always rendered (PC, iOS).
	*/
	public enum RenderMode {
		Always = 0,
		OnVisibility = 1,
		Never = 2
	};

	/**
	* <summary>マテリアルレンダーモード</summary>
	* \par 説明:
	* <br/>
	* \par 注意:
	* 
	*/
	public RenderMode renderMode = RenderMode.Always;

	#endregion


	#region Internal Variables
	[SerializeField]
	private Material _material;
	[SerializeField]
	private string _moviePath;
	[SerializeField]
	private bool _loop = false;
	[SerializeField]
	private MaxFrameDrop _maxFrameDrop = MaxFrameDrop.Disabled;
	[SerializeField]
	private bool _additiveMode = false;
	[SerializeField]
	private bool _advancedAudio = false;
	[SerializeField]
	private bool _ambisonics = false;
	[SerializeField]
	private bool _applyTargetAlpha = false;
	private bool materialOwn = false;
	private bool isMonoBehaviourStartCalled = false;
	private GameObject ambisonicSource = null;
#if UNITY_EDITOR
	private bool isApplicationPaused = false;
	private bool isEditorPaused = false;
#endif
	private bool unpauseOnApplicationUnpause = false;
#endregion
	protected bool HaveRendererOwner { get; private set; }


	/**
	 * <summary>再生を開始します。</summary>
	 * \par 説明:
	 * ムービ再生を開始します。<br/>
	 * 再生が開始されると、状態( ::CriMana::Player::Status) は Playing になります。
	 * \par 注意:
	 * 本関数を呼び出し後、実際にムービの描画が開始されるまで数フレームかかります。<br>
	 * ムービ再生の頭出しを行いたい場合は本関数をを使わず、 player プロパティにアクセスし、CriMana::Player::Prepare 関数で事前に再生準備を行ってください。
	 * \sa CriManaMovieMaterial::Stop, CriMana::Player::Status
	 */
    public void Play()
	{
		player.Start();
		this.CriInternalUpdate();
	}


	/**
	 * <summary>ムービ再生の停止要求を行います。</summary>
	 * \par 説明:
	 * ムービ再生の停止要求を出します。本関数は即時復帰関数です。<br/>
	 * 本関数を呼び出すと、描画は即座に終了しますが、再生はすぐには止まりません。<br/>
	 * \sa CriMana::Player::Status
	 */
	public void Stop()
	{
		player.Stop();
		if (isMaterialAvailable) {
			isMaterialAvailable = false;
			OnMaterialAvailableChanged();
		}
	}
	


	/**
	 * <summary>ムービ再生のポーズ切り替えを行います。</summary>
	 * <param name="sw">ポーズスイッチ(true: ポーズ, false: ポーズ解除)</param>
	 * \par 説明:
	 * ポーズのON/OFFを切り替えます。<br/>
	 * 引数にtrueを指定する一時停止、falseを指定すると再生再開です。<br/>
	 * CriManaMovieMaterial::Stop 関数を呼び出すと、ポーズ状態は解除されます <br/>
	 */
	public void Pause(bool sw)
	{
		player.Pause(sw);
	}


	/**
	 * <summary>isMaterialAvailable プロパティが変化した際に呼び出されるメソッドです。</summary>
	 * \par 説明:
	 * isMaterialAvailable プロパティが変化した際に呼び出されるメソッドです。<br>
	 * 継承先でオーバーライドすることを想定しています。<br>
	 */
	protected virtual void OnMaterialAvailableChanged()
	{
	}


	/**
	 * <summary>マテリアルに新しいフレームが流し込まれた際に呼び出されるメソッドです。</summary>
	 * \par 説明:
	 * マテリアルに新しいフレームが流し込まれた際に呼び出されるメソッドです。<br>
	 * 継承先でオーバーライドすることを想定しています。<br>
	 */
	protected virtual void OnMaterialUpdated()
	{
	}

	private CriManaMoviePlayerHolder playerHolder;

	/**
	 * <summary>プレーヤーの初期化（手動用）</summary>
	 * \par 説明:
	 * プレーヤーを手動で初期化します。<br/>
	 * \par 注意：
	 * 通常はAwake関数から呼び出されるため、本関数を個別に呼ぶ必要はありません。<br/>
	 * エディタ拡張などで、手動で初期化したい場合、本関数を使用してください。<br/>
	 */
	public void PlayerManualInitialize(){
		player = new CriMana.Player(_advancedAudio, _ambisonics);
		isMaterialAvailable = false;
		if (Application.isPlaying) {
			// object that can keep alive player for defering graphics cleaning
			GameObject go = new GameObject("CriManaMovieResources");
			playerHolder = go.AddComponent<CriManaMoviePlayerHolder>();
			playerHolder.player = player;
			player.playerHolder = playerHolder;
		}
	}

	/**
	 * <summary>プレーヤーの終了（手動用）</summary>
	 * \par 説明:
	 * プレーヤーを手動で終了します。<br/>
	 * \par 注意：
	 * 通常はOnDestroy関数から呼び出されるため、本関数を個別に呼ぶ必要はありません。<br/>
	 * エディタ拡張などで、手動で終了したい場合、本関数を使用してください。<br/>
	 */
	public void PlayerManualFinalize(){
		if (player != null) {
			player.Dispose();
			player = null;
			material = null;
		}
	}

	/**
	 * <summary>プレーヤーの準備（手動用）</summary>
	 * \par 説明:
	 * プレーヤーを手動で準備します。<br/>
	 * \par 注意：
	 * 通常はStart関数から呼び出されるため、本関数を個別に呼ぶ必要はありません。<br/>
	 * エディタ拡張などで、手動で準備したい場合、本関数を使用してください。<br/>
	 */
	public void PlayerManualSetup(){
		HaveRendererOwner = (this.GetComponent<Renderer>() != null);

		if (_material == null) {
			CreateMaterial();
		}
		if (!System.String.IsNullOrEmpty(moviePath)) {
			player.SetFile(null, moviePath);
		}
		player.Loop(loop);
		player.additiveMode = additiveMode;
		player.maxFrameDrop = (int)maxFrameDrop;
		player.applyTargetAlpha = applyTargetAlpha;
		if (playOnStart) {
			player.Start();
		}
	}

	/**
	 * <summary>プレーヤーのフレーム更新（手動用）</summary>
	 * \par 説明:
	 * プレーヤーのフレームを手動で更新します。<br/>
	 * \par 注意：
	 * 通常はUpdate関数から呼び出されるため、本関数を個別に呼ぶ必要はありません。<br/>
	 * エディタ拡張などで、手動でフレームを更新したい場合、本関数を使用してください。<br/>
	 */
	public void PlayerManualUpdate(){
		if (player != null) {
			player.Update();
			bool isMaterialAvailableCurrent;
			if (player.isFrameAvailable) {
				isMaterialAvailableCurrent = player.UpdateMaterial(material);
				if (isMaterialAvailableCurrent) {
					OnMaterialUpdated();
				}
			} else {
				isMaterialAvailableCurrent = false;
			}
			if (isMaterialAvailable != isMaterialAvailableCurrent) {
				isMaterialAvailable = isMaterialAvailableCurrent;
				OnMaterialAvailableChanged();
			}
		} 
	}

	#region MonoBehavior Inherited Methods
	protected override void Awake()
	{
		base.Awake();
		PlayerManualInitialize();
	}

	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
#if UNITY_2017_2_OR_NEWER
		UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		UnityEditor.EditorApplication.pauseStateChanged += OnPauseStateChanged;
#else
		UnityEditor.EditorApplication.playmodeStateChanged += OnPlaymodeStateChange;
#endif
#endif
	}

	protected virtual void OnDisable()
	{
#if UNITY_EDITOR
#if UNITY_2017_2_OR_NEWER
		UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		UnityEditor.EditorApplication.pauseStateChanged -= OnPauseStateChanged;
#else
		UnityEditor.EditorApplication.playmodeStateChanged -= OnPlaymodeStateChange;
#endif
#endif
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		PlayerManualFinalize();
	}

	protected virtual void Start()
	{
		PlayerManualSetup();

		/*
			if (CriManaPlugin.isMultithreadedRenderingEnabled) {
				StartCoroutine(CallPluginAtEndOfFrames());
			}
		*/
		isMonoBehaviourStartCalled = true;
	}

	public override void CriInternalUpdate()
	{
		PlayerManualUpdate();
	}

	// Render the movie picture to target material.
	public virtual void RenderMovie()
	{
		player.OnWillRenderObject(this);
	}

	// The movie picture is always rendered to target material whenever owner object is visible or not.
	public override void CriInternalLateUpdate()
	{
		if (renderMode == RenderMode.Always)
		{
			player.OnWillRenderObject(this);
		}
	}

	// If owner object is visible the movie picture is rendered to target material.
	protected virtual void OnWillRenderObject()
	{
		if (renderMode == RenderMode.OnVisibility)
		{
			player.OnWillRenderObject(this);
		}
	}

#if UNITY_EDITOR
	private void OnPlaymodeStateChange()
	{
		bool paused = UnityEditor.EditorApplication.isPaused;
		if (!isApplicationPaused && isEditorPaused != paused) {
			ProcessApplicationPause(paused);
			isEditorPaused = paused;
		}
	}

#if UNITY_2017_2_OR_NEWER
	private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
	{
		OnPlaymodeStateChange();
	}
	private void OnPauseStateChanged(UnityEditor.PauseState state)
	{
		OnPlaymodeStateChange();
	}
#endif

#endif

	void OnApplicationPause(bool appPause)
	{
#if UNITY_EDITOR
		if (!isEditorPaused && isApplicationPaused != appPause) {
			ProcessApplicationPause(appPause);
			isApplicationPaused = appPause;
		}
#else
		ProcessApplicationPause(appPause);
#endif
	}	

	void ProcessApplicationPause(bool appPause)
	{
		if (appPause) {
			unpauseOnApplicationUnpause = !player.IsPaused();
			if (unpauseOnApplicationUnpause) {
				player.Pause(true);
			}
		}
		else {
			if (unpauseOnApplicationUnpause) {
				player.Pause(false);
			}
			unpauseOnApplicationUnpause = false;
		}
	}
	
	
	protected virtual void OnDrawGizmos()
	{
		if ((player != null) && (player.status == CriMana.Player.Status.Playing)) {
			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.8f);
		}
		else {
			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		}
		
		Gizmos.DrawIcon(this.transform.position, "CriWare/film.png");
		Gizmos.DrawLine(this.transform.position, new Vector3(0, 0, 0));
	}
	#endregion


	#region Private Methods
	private void CreateMaterial()
	{
		_material = new Material(Shader.Find("VertexLit"));
		_material.name = "CriMana-MovieMaterial";
		materialOwn = true;
	}

	/*
		private System.Collections.IEnumerator CallPluginAtEndOfFrames()
		{
	 		while (true) {
				yield return new WaitForEndOfFrame();
				player.IssuePluginEvent();
			}
		}
	*/
	#endregion
}


/**
 * @}
 */

/* end of file */
