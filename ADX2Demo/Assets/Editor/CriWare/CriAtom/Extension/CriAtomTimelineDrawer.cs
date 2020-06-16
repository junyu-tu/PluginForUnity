/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace CriTimeline.Atom {

	[CanEditMultipleObjects]
	[CustomPropertyDrawer(typeof(CriAtomBehaviour))]
	public class CriAtomTimelineDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			SerializedProperty volumeProp = property.FindPropertyRelative("volume");
			SerializedProperty pitchProp = property.FindPropertyRelative("pitch");
			SerializedProperty aisacProp = property.FindPropertyRelative("AISACValue");

			EditorGUILayout.PropertyField(volumeProp);
			EditorGUILayout.PropertyField(pitchProp);
			EditorGUILayout.PropertyField(aisacProp);
		}
	}
}

#endif