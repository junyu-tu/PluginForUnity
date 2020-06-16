/****************************************************************************
 *
 * Copyright (c) 2015-2018 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_PSP2 || UNITY_IOS || UNITY_TVOS || UNITY_WEBGL || UNITY_STANDALONE_LINUX || UNITY_SWITCH || UNITY_STADIA

using UnityEngine;
using System;

namespace CriMana.Detail
{
	public static partial class AutoResisterRendererResourceFactories
	{
		[RendererResourceFactoryPriority(5000)]
		public class RendererResourceFactorySofdecPrimeYuv : RendererResourceFactory
		{
			public override RendererResource CreateRendererResource(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
			{
				bool isCodecSuitable = movieInfo.codecType == CodecType.SofdecPrime;
				bool isSuitable      = isCodecSuitable;
				return isSuitable
					? new RendererResourceSofdecPrimeYuv(playerId, movieInfo, additive, userShader)
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




	public class RendererResourceSofdecPrimeYuv : RendererResource
	{
		private int		width;
		private int		height;
		private bool	hasAlpha;
		private bool	additive;
		private bool	applyTargetAlpha;
		private bool	useUserShader;
		private CodecType	codecType;

		private Shader		shader;
		private Material	currentMaterial;
		private Vector4		movieTextureST = Vector4.zero;
		private Vector4		movieChromaTextureST = Vector4.zero;

		private static Int32 NumTextureSets { get { return 1; } }

		private Texture2D[]	textureY = new Texture2D[NumTextureSets];
		private Texture2D[]	textureU = new Texture2D[NumTextureSets];
		private Texture2D[]	textureV = new Texture2D[NumTextureSets];
		private Texture2D[]	textureA = new Texture2D[NumTextureSets];
		private IntPtr[][] 	nativeTextures = new IntPtr[NumTextureSets][];
		private Int32 currentTextureSet = 0;
		private Int32 drawTextureSet = 0;

		private int		 	srcBlendMode;
		private int		 	dstBlendMode;

		private Int32 		playerID;
		private bool hasTextureUpdated = false;
		private bool isTextureReady = false;


		public RendererResourceSofdecPrimeYuv(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
		{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_PSP2 || UNITY_PS4 || UNITY_WINRT || UNITY_WEBGL || UNITY_STANDALONE_LINUX || UNITY_SWITCH || UNITY_STADIA
			width = Ceiling256((int)movieInfo.width);
			height = Ceiling16((int)movieInfo.height);
#elif UNITY_IOS || UNITY_TVOS
			width  = NextPowerOfTwo(Ceiling64((int)movieInfo.width));
			height = NextPowerOfTwo(Ceiling16((int)movieInfo.height));
#else
	#error unsupported platform
#endif
			this.additive	= additive;
			hasAlpha		= movieInfo.hasAlpha;
			useUserShader	= userShader != null;
			codecType		= movieInfo.codecType;

			if (useUserShader) {
				shader = userShader;
			} else {
				string shaderName = "CriMana/SofdecPrimeYuv";
				shader = Shader.Find(shaderName);
			}

			if (hasAlpha) {
				srcBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One :  (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				dstBlendMode = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
			} else {
				srcBlendMode = (int)UnityEngine.Rendering.BlendMode.One;
				dstBlendMode = additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.Zero;
			}

			UpdateMovieTextureST(movieInfo.dispWidth, movieInfo.dispHeight);

			for (int i = 0; i < NumTextureSets; i++) {
				textureY[i] = new Texture2D(width, height, TextureFormat.Alpha8, false);
				textureY[i].wrapMode = TextureWrapMode.Clamp;
				textureU[i] = new Texture2D(width / 2, height / 2, TextureFormat.Alpha8, false);
				textureU[i].wrapMode = TextureWrapMode.Clamp;
				textureV[i] = new Texture2D(width / 2, height / 2, TextureFormat.Alpha8, false);
				textureV[i].wrapMode = TextureWrapMode.Clamp;

				nativeTextures[i] = new IntPtr[4];
				nativeTextures[i][0] = textureY[i].GetNativeTexturePtr();
				nativeTextures[i][1] = textureU[i].GetNativeTexturePtr();
				nativeTextures[i][2] = textureV[i].GetNativeTexturePtr();
				if (hasAlpha) {
					textureA[i] = new Texture2D(width, height, TextureFormat.Alpha8, false);
					textureA[i].wrapMode = TextureWrapMode.Clamp;
					nativeTextures[i][3] = textureA[i].GetNativeTexturePtr();
				}
			}

			playerID = playerId;
		}


		protected override void OnDisposeManaged()
		{
		}

		static private bool IsEditor {
			get {
#if UNITY_EDITOR
				if (UnityEditor.EditorApplication.isPlaying == false) {
					return true;
				}
#endif
				return false;
			}
		}

		protected override void OnDisposeUnmanaged()
		{
			for (int i = 0; i < NumTextureSets; i++) {
				if (textureY[i] != null) {
					if(IsEditor){
						Texture2D.DestroyImmediate(textureY[i]);
					} else{
						Texture2D.Destroy(textureY[i]);
					}
					textureY[i] = null;
				}
				if (textureU[i] != null) {
					if (IsEditor) {
						Texture2D.DestroyImmediate(textureU[i]);
					} else {
						Texture2D.Destroy(textureU[i]);
					}
					textureU[i] = null;
				}
				if (textureV[i] != null) {
					if (IsEditor) {
						Texture2D.DestroyImmediate(textureV[i]);
					} else {
						Texture2D.Destroy(textureV[i]);
					}
					textureV[i] = null;
				}
				if (textureA[i] != null) {
					if (IsEditor) {
						Texture2D.DestroyImmediate(textureA[i]);
					} else {
						Texture2D.Destroy(textureA[i]);
					}
					textureA[i] = null;
				}
			}
		}


		public override bool IsPrepared()
		{ return isTextureReady; }


		public override bool ContinuePreparing()
		{ return true; }


		public override bool IsSuitable(int playerId, MovieInfo movieInfo, bool additive, Shader userShader)
		{
			bool isCodecSuitable    = movieInfo.codecType == codecType;
			bool isSizeSuitable     = (width >= (int)movieInfo.width) && (height >= (int)movieInfo.height);
			bool isAlphaSuitable    = hasAlpha == movieInfo.hasAlpha;
			bool isAdditiveSuitable = this.additive == additive;
			bool isShaderSuitable   = this.useUserShader ? (userShader == shader) : true;
			return isCodecSuitable && isSizeSuitable && isAlphaSuitable && isAdditiveSuitable && isShaderSuitable;
		}


		public override void SetApplyTargetAlpha(bool flag) {
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


		public override bool OnPlayerStopForSeek() {
			return true;
		}


		public override void AttachToPlayer(int playerId)
		{
			hasTextureUpdated = false;
			isTextureReady = false;
		}


		public override bool UpdateFrame(int playerId, FrameInfo frameInfo, ref bool frameDrop)
		{
			bool isFrameUpdated = CRIWAREE00232AA(playerId, 0, null, frameInfo, ref frameDrop);
			if (isFrameUpdated && !frameDrop) {
				UpdateMovieTextureST(frameInfo.dispWidth, frameInfo.dispHeight);
				drawTextureSet = currentTextureSet;
				currentTextureSet = (currentTextureSet + 1) % NumTextureSets;
			}
			if (hasTextureUpdated) {
				isTextureReady = true;
			}
			return isFrameUpdated;
		}


		public override bool UpdateMaterial(Material material)
		{
			if (!isTextureReady && NumTextureSets > 1)
				return false;

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
			material.SetTexture("_TextureY", textureY[drawTextureSet]);
			material.SetTexture("_TextureU", textureU[drawTextureSet]);
			material.SetTexture("_TextureV", textureV[drawTextureSet]);
			material.SetVector("_MovieTexture_ST", movieTextureST);
			material.SetVector("_MovieChromaTexture_ST", movieChromaTextureST);
			if (hasAlpha) {
				material.SetTexture("_TextureA", textureA[drawTextureSet]);
				material.SetVector("_MovieAlphaTexture_ST", movieTextureST);
			}

			return true;
		}


		private void UpdateMovieTextureST(System.UInt32 dispWidth, System.UInt32 dispHeight)
		{
			float uScale = (dispWidth != width) ? (float)(dispWidth - 1) / width : 1.0f;
			float vScale = (dispHeight != height) ? (float)(dispHeight - 1) / height : 1.0f;
			movieTextureST.x = uScale;
			movieTextureST.y = -vScale;
			movieTextureST.z = 0.0f;
			movieTextureST.w = vScale;

			uScale = (dispWidth != width) ? (float)(dispWidth / 2 - 1) / width * 2 : 1.0f;
			vScale = (dispHeight != height) ? (float)(dispHeight / 2 - 1) / height * 2 : 1.0f;
			movieChromaTextureST.x = uScale;
			movieChromaTextureST.y = -vScale;
			movieChromaTextureST.z = 0.0f;
			movieChromaTextureST.w = vScale;
		}


		public override void UpdateTextures()
		{
			int numTextures = 3;
			if (hasAlpha) {
				numTextures = 4;
			}

			hasTextureUpdated |= CRIWARE1DB87507(playerID, numTextures, nativeTextures[currentTextureSet]);
		}
	}
}


#endif
