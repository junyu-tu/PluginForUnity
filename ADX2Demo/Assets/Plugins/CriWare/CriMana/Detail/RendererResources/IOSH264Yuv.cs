/****************************************************************************
 *
 * Copyright (c) 2015 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if !UNITY_EDITOR && UNITY_IOS

using UnityEngine;
using System.Runtime.InteropServices;

namespace CriMana.Detail
{
	public static partial class AutoResisterRendererResourceFactories
	{
		[RendererResourceFactoryPriority(7000)]
		public class RendererResourceFactoryIOSH264Yuv : RendererResourceFactory
		{
			public override RendererResource CreateRendererResource(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
			{
				bool isCodecSuitable = movieInfo.codecType == CodecType.H264;
				bool isSuitable      = isCodecSuitable;
				return isSuitable
					? new RendererResourceIOSH264Yuv(playerId, movieInfo, additive, userShader)
					: null;
			}

			protected override void OnDisposeManaged()
			{
			}

			protected override void OnDisposeUnmanaged()
			{
			}
		}
	}


	public class RendererResourceIOSH264Yuv : RendererResource
	{
		private int		width;
		private int		height;
		private int 	playerId;
		private bool	hasAlpha;
		private bool	additive;
		private bool	applyTargetAlpha;
		private bool	useUserShader;
		private bool	useOGLTempTextures;
		private bool	isPaused;

		private Shader			shader;

		private Vector4			movieTextureST = Vector4.zero;

		private Texture2D[]		textures;
		private RenderTexture[]	pauseTextures;
		private Texture[]		currentTextures;
		private System.IntPtr[]	nativePtrs;
		private Material		currentMaterial = null;
		private int				srcBlendMode;
		private int				dstBlendMode;
		private bool			isStoppingForSeek = false;
		private bool			isStartTriggered = true;

		public RendererResourceIOSH264Yuv(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
		{
			this.width		= (int)movieInfo.width;
			this.height		= (int)movieInfo.height;
			this.playerId	= playerId;
			hasAlpha		= movieInfo.hasAlpha;
			this.additive	= additive;
			useUserShader	= userShader != null;

			if (userShader != null) {
				shader = userShader;
			} else {
				string shaderName = "CriMana/IOSH264Yuv";
				shader = Shader.Find(shaderName);
			}

			if (hasAlpha) {
				srcBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				dstBlendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
			} else {
				srcBlendMode = (int)UnityEngine.Rendering.BlendMode.One;
				dstBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.Zero;
			}

			int numTextures = hasAlpha ? 3 : 2;
			textures = new Texture2D[numTextures];
			nativePtrs = new System.IntPtr[numTextures];

			UpdateMovieTextureST(movieInfo.dispWidth, movieInfo.dispHeight);

#if (UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
			useOGLTempTextures = SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL");
#else
			useOGLTempTextures = (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2);
#endif
			currentTextures = textures;
		}


		protected override void OnDisposeManaged()
		{
		}


		protected override void OnDisposeUnmanaged()
		{
			DisposeTextures(textures);
			DisposeTextures(pauseTextures);
			textures = null;
			pauseTextures = null;
			currentMaterial = null;
		}


		public override bool IsPrepared()
		{ return true; }


		public override bool ContinuePreparing()
		{ return true; }

		public override bool IsSuitable(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
		{
			bool isCodecSuitable    = movieInfo.codecType == CodecType.H264;
			bool isAlphaSuitable    = hasAlpha == movieInfo.hasAlpha;
			bool isAdditiveSuitable = this.additive == additive;
			bool isShaderSuitable   = this.useUserShader ? (userShader == shader) : true;
			return isCodecSuitable && isAlphaSuitable && isAdditiveSuitable && isShaderSuitable;
		}

		public override void SetApplyTargetAlpha(bool flag)
		{
			applyTargetAlpha = flag;

			if (hasAlpha || applyTargetAlpha) {
				srcBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				dstBlendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
			} else {
				srcBlendMode = (int)UnityEngine.Rendering.BlendMode.One;
				dstBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.Zero;
			}

			if (currentMaterial != null) {
				currentMaterial.SetInt("_SrcBlendMode", srcBlendMode);
				currentMaterial.SetInt("_DstBlendMode", dstBlendMode);
				if (applyTargetAlpha) {
					currentMaterial.EnableKeyword("CRI_APPLY_TARGET_ALPHA");
				} else {
					currentMaterial.DisableKeyword("CRI_APPLY_TARGET_ALPHA");
				}
			}
		}

		public override void AttachToPlayer(int playerId)
		{
 			DisposeTextures(textures);
 			if (!isStoppingForSeek) {
 				DisposeTextures(pauseTextures);
 				pauseTextures = null;
 				currentTextures = textures;
 			}
			isPaused = false;
		}

		// app did goes background / will goes foreground or player is paused
		public override void OnPlayerPause(bool pauseStatus)
		{
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1
			if (pauseStatus == true) {
				if (isPaused == false && !isStoppingForSeek) {
					isPaused = true;
					applyCurrentTexturesToPausedTextures();
				}
			} else {
				isPaused = false;
			}
#endif
		}

		public override bool OnPlayerStopForSeek() {
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1
			isStoppingForSeek = true;
			isStartTriggered = false;
			if (pauseTextures != null || textures[0] == null) {
				return true;
			}
			applyCurrentTexturesToPausedTextures();
 			DisposeTextures(textures);
			return true;
#else
			return false;
#endif
		}

		public override void OnPlayerStart() {
			isStartTriggered = true;
		}

		private void restorePausedTextures()
		{
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1
			forceUpdateMaterialTextures(textures);
			DisposeTextures(pauseTextures);
			pauseTextures = null;
#endif
		}

		private void applyCurrentTexturesToPausedTextures() {
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1
			pauseTextures = new RenderTexture[hasAlpha ? 3 : 2];
			for (int i = 0; i < pauseTextures.Length; i++) {
				Texture2D baseTexture = textures[i];
				RenderTexture texture = new RenderTexture(baseTexture.width, baseTexture.height, 0,
															i == 1 ? RenderTextureFormat.RG16 : RenderTextureFormat.R8);
				texture.Create();
				Graphics.Blit(baseTexture, texture);
				pauseTextures[i] = texture;
			}
			forceUpdateMaterialTextures(pauseTextures);
#endif
		}

		private void forceUpdateMaterialTextures(Texture[] newTextures)
		{
			currentTextures = newTextures;
			if (currentMaterial != null) {
				currentMaterial.SetTexture("_TextureY", currentTextures[0]);
				currentMaterial.SetTexture("_TextureUV", currentTextures[1]);
				if (hasAlpha) {
					currentMaterial.SetTexture("_TextureA", currentTextures[2]);
				}
			}
		}

		public override bool UpdateFrame(int playerId, FrameInfo frameInfo, ref bool frameDrop)
		{
			bool isFrameUpdated = CRIWAREE00232AA(playerId, 0, null, frameInfo, ref frameDrop);
			if (isFrameUpdated && !frameDrop) {
				UpdateMovieTextureST(frameInfo.dispWidth, frameInfo.dispHeight);
			}
			return isFrameUpdated;
		}

		public override bool UpdateMaterial(Material material)
		{
			if (currentTextures[0] != null) {
				if (currentMaterial != material) {
					if (material.shader != shader) {
						material.shader = shader;
					}
					material.SetInt("_SrcBlendMode", srcBlendMode);
					material.SetInt("_DstBlendMode", dstBlendMode);
					if (hasAlpha) {
						material.EnableKeyword("CRI_ALPHA_MOVIE");
					}
					if (applyTargetAlpha) {
						material.EnableKeyword("CRI_APPLY_TARGET_ALPHA");
					}
					if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
						material.EnableKeyword("CRI_LINEAR_COLORSPACE");
					}
					currentMaterial = material;
				}

				if (!(isPaused || isStoppingForSeek) && pauseTextures != null &&
					!CRIWARE48866453(playerId)) {
					restorePausedTextures();
					isStoppingForSeek = false;
				} else {
					material.SetTexture("_TextureY", currentTextures[0]);
					material.SetTexture("_TextureUV", currentTextures[1]);
					if (hasAlpha) {
						material.SetTexture("_TextureA", currentTextures[2]);
					}
 				}
				material.SetVector("_MovieTexture_ST", movieTextureST);

				return true;
			}
			return false;
		}


		private void UpdateMovieTextureST(System.UInt32 dispWidth, System.UInt32 dispHeight)
		{
			float uScale = (dispWidth != width) ? (float)(dispWidth - 0.5f) / width : 1.0f;
			float vScale = (dispHeight != height) ? (float)(dispHeight - 0.5f) / height : 1.0f;
			movieTextureST.x = uScale;
			movieTextureST.y = -vScale;
			movieTextureST.z = 0.0f;
			movieTextureST.w = vScale;
		}


		public override void UpdateTextures()
		{
			int numTextures = hasAlpha ? 3 : 2;
			for (int i = 0; i < numTextures; i++) {
				nativePtrs[i] = System.IntPtr.Zero;
			}
			bool isTextureUpdated = CRIWARE1DB87507(playerId, numTextures, nativePtrs); // out textures
			if (isTextureUpdated && nativePtrs[0] != System.IntPtr.Zero && isStartTriggered) {
				if (useOGLTempTextures) {
					for (int i = 0; i < numTextures; i++) {
						if (textures[i] == null) {
							textures[i] = Texture2D.CreateExternalTexture((i == 1) ? width / 2 : width,
																			(i == 1) ? height / 2 : height,
																			TextureFormat.Alpha8, false, false, nativePtrs[i]);
						}
						Texture2D tmptexture = Texture2D.CreateExternalTexture(textures[i].width, textures[i].height,
																				 textures[i].format, false, false, nativePtrs[i]);
						tmptexture.wrapMode = TextureWrapMode.Clamp;
						textures[i].UpdateExternalTexture(tmptexture.GetNativeTexturePtr());
						Texture2D.Destroy(tmptexture);
					}
				} else {
					if (textures[0] == null) {
						for (int i = 0; i < numTextures; i++) {
							textures[i] = Texture2D.CreateExternalTexture((i == 1) ? width / 2 : width,
																		  (i == 1) ? height / 2 : height,
																		  TextureFormat.Alpha8, false, false, nativePtrs[i]);
							textures[i].wrapMode = TextureWrapMode.Clamp;
						}
					} else {
						for (int i = 0; i < numTextures; i++) {
							textures[i].UpdateExternalTexture(nativePtrs[i]);
						}
					}
				}
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1
				if (CRIWARE48866453(playerId)) {
					OnPlayerPause(true);
				}
#endif
 				isStoppingForSeek = false;
			}
		}
#region DLL Import
#if !CRIWARE_ENABLE_HEADLESS_MODE
		[DllImport(CriWare.pluginName, CallingConvention = CriWare.pluginCallingConvention)]
		private static extern bool CRIWARE48866453(int player_id);
#else
		private static bool CRIWARE48866453(int player_id) { return false; }
#endif
#endregion
	}
}

#endif
