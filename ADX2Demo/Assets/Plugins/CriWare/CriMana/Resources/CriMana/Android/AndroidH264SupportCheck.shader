Shader "CriMana/AndroidH264DummySupportCheck"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		[HideInInspector] _TextureRGB("TextureRGB", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			GLSLPROGRAM
			#version 100 // this will be converted to 300 es when using OpenGLES3
			#ifdef VERTEX
			attribute vec4 _glesVertex;
			attribute vec4 _glesMultiTexCoord0;
			varying mediump vec2 xlv_TEXCOORD0;
			void main()
			{
				gl_Position = _glesVertex;
				xlv_TEXCOORD0 = _glesMultiTexCoord0.xy;
			}

			#endif
			#ifdef FRAGMENT
			#extension GL_OES_EGL_image_external : enable
			#extension GL_OES_EGL_image_external_essl3 : enable
			uniform samplerExternalOES _TextureRGB;
			varying mediump vec2 xlv_TEXCOORD0;
			void main()
			{
				gl_FragData[0] = texture2D(_TextureRGB, xlv_TEXCOORD0);
			}
			#endif
			ENDGLSL
		}
	}
}
