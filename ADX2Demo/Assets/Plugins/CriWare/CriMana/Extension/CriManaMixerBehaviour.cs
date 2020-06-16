/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if UNITY_2018_1_OR_NEWER

//#define CRI_TIMELINE_MANA_VERBOSE_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CriTimeline.Mana
{
	[Serializable]
	public class CriManaMixerBehaviour : PlayableBehaviour {
		internal PlayableDirector m_PlayableDirector;
		internal IEnumerable<TimelineClip> m_clips;
		internal CriManaMovieMaterial m_boundMovieMaterial;

		static private double cPreloadTimeSec = 1.0;

		private Guid? m_lastClipId = null;

		static private bool IsEditMode {
			get {
#if UNITY_EDITOR
				if (EditorApplication.isPlaying == false) {
					return true;
				}
#endif
				return false;
			}
		}

		private enum MovieMixerState {
			Preloading,
			Ready,
			Playing,
			Stopping,
			Stopped,
			Seeking,
			SeekStopping,
			SeekStopped
		}
		private MovieMixerState m_movieMixerState = MovieMixerState.Stopped;

		private bool PlayMovie(CriManaClip clipAsset, int startFrame) {
			if (m_boundMovieMaterial == null ||
				m_boundMovieMaterial.player == null ||
				m_boundMovieMaterial.player.status == CriMana.Player.Status.StopProcessing ||
				m_boundMovieMaterial.player.status == CriMana.Player.Status.Error
				) {
				return false;
			}

			/**
			 * Set render target explicitly when in edit mode
			 * (since the Start method in the movie controller component will not run)
			 */
			if (IsEditMode) {
				if (m_boundMovieMaterial is CriManaMovieController) {
					var casted = m_boundMovieMaterial as CriManaMovieController;
					if (casted.target == null) {
						casted.target = casted.gameObject.GetComponent<Renderer>();
					}
					if (casted.target == null) {
						Debug.LogWarning("[CRIWARE] Missing render target on CriManaMovieController: Please add a renderer to the GameObject or specify the target manually.");
						return false;
					}
				} else if (m_boundMovieMaterial is CriManaMovieControllerForUI) {
					var casted = m_boundMovieMaterial as CriManaMovieControllerForUI;
					if (casted.target == null) {
						casted.target = casted.gameObject.GetComponent<UnityEngine.UI.Graphic>();
					}
					if (casted.target == null) {
						Debug.LogWarning("[CRIWARE] Missing render target on CriManaMovieControllerForUI: Please add a renderer to the GameObject or specify the target manually.");
						return false;
					}
				}
			}

			/**
			 * if (clipAsset == null) then play the prepared movie
			 * otherwise set new movie to play
			 */
			if (clipAsset != null) {
				if (clipAsset.guid != m_lastClipId) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
					Debug.Log("Set <color=white>new</color> movie");
#endif
					m_boundMovieMaterial.player.SetFile(null, clipAsset.m_moviePath);
					m_boundMovieMaterial.player.Loop(clipAsset.m_loopWithinClip);
					m_lastClipId = clipAsset.guid;
				}
				if (startFrame > 0) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
					Debug.Log("Set start Frame <color=white>at</color> " + startFrame);
#endif
					m_boundMovieMaterial.player.SetSeekPosition(startFrame);
				} else {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
					Debug.Log("<color=white>Reset</color> Frame");
#endif
					m_boundMovieMaterial.player.SetSeekPosition(0);
				}
			}

#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=yellow>PlayMovie</color>");
#endif
			m_boundMovieMaterial.player.Start();

			return true;
		}

		private bool PrepareMovie(CriManaClip clipAsset) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=green>PrepareMovie</color>");
#endif
			if (m_boundMovieMaterial == null ||
				m_boundMovieMaterial.player == null ||
				m_boundMovieMaterial.player.status != CriMana.Player.Status.Stop
				) {
				return false;
			}

			m_boundMovieMaterial.player.SetFile(null, clipAsset.m_moviePath);
			m_boundMovieMaterial.player.Loop(clipAsset.m_loopWithinClip);
			m_boundMovieMaterial.player.SetSeekPosition(0);
			m_lastClipId = clipAsset.guid;

			m_boundMovieMaterial.player.Prepare();
			return true;
		}

		private bool StopMovie() {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=red>StopMovie</color>");
#endif
			if (m_boundMovieMaterial == null || m_boundMovieMaterial.player == null) { return false; }

			m_boundMovieMaterial.player.Stop();
			m_lastClipId = null;
			return true;
		}

		private bool StopForSeekMovie() {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=#f26fe7>StopForSeekMovie</color>");
#endif
			if (m_boundMovieMaterial == null || m_boundMovieMaterial.player == null) { return false; }

			m_boundMovieMaterial.player.StopForSeek();
			return true;
		}

		private enum ClipState {
			Idle,
			Prepare,
			Play,
			Seek
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
			if (playable.GetInputCount() <= 0 || playerData == null) {
				return;
			}

			double frameTime = m_PlayableDirector.time;

			ClipState clipState = ClipState.Idle;
			TimelineClip activeClip = null;
			CriManaClip clipAsset = null;
			foreach (var clip in m_clips) {
				if (frameTime > clip.start && frameTime < clip.end) {
					if (m_PlayableDirector.state == UnityEngine.Playables.PlayState.Playing) {
						clipState = ClipState.Play;
					} else if (IsEditMode) {
						clipState = ClipState.Seek;
					}
					activeClip = clip;
					break;
				} else if (frameTime >= clip.start - cPreloadTimeSec && frameTime <= clip.start) {
					clipState = ClipState.Prepare;
					activeClip = clip;
				}
			}

			if (clipState != ClipState.Idle) {
				if (activeClip != null) {
					clipAsset = activeClip.asset as CriManaClip;
				}
				if (activeClip == null || clipAsset == null) {
					/* impossible if clip system works */
					Debug.LogWarning("[CRIWARE] Timeline / Mana: null clip");
					return;
				}
			}

			/**
			 * Cautions:
			 * 1. activeClip / clipAsset should never be used when clipState == ClipState.Idle
			 * 2. Each case should cover all ClipState variations.
			 */
			switch (m_movieMixerState) {
				case MovieMixerState.Stopped:
					if (clipState == ClipState.Prepare) {
						if (this.PrepareMovie(clipAsset)) {
							m_movieMixerState = MovieMixerState.Preloading;
						}
					} else if (clipState == ClipState.Play || clipState == ClipState.Seek) {
						if (this.PlayMovie(clipAsset, clipAsset.GetSeekFrame(frameTime - activeClip.start, clipAsset.m_loopWithinClip))) {
							m_movieMixerState = (clipState == ClipState.Play)
											  ? MovieMixerState.Playing
											  : MovieMixerState.Seeking;
						}
					}
					break;
				case MovieMixerState.Preloading:
					if (clipState != ClipState.Prepare) {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
					} else {
						if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Ready ||
							m_boundMovieMaterial.player.status == CriMana.Player.Status.ReadyForRendering) {
							if (m_boundMovieMaterial.player.movieInfo != null) {
								m_movieMixerState = MovieMixerState.Ready;
							} else {
								if (this.StopMovie()) {
									m_movieMixerState = MovieMixerState.Stopping;
								}
							}
						}
					}
					break;
				case MovieMixerState.Ready:
					if ((clipState == ClipState.Play || clipState == ClipState.Seek) && clipAsset.guid == m_lastClipId) {
						if (this.PlayMovie(null, clipAsset.GetSeekFrame(frameTime - activeClip.start, clipAsset.m_loopWithinClip))) {
							m_movieMixerState = (clipState == ClipState.Play) 
											  ? MovieMixerState.Playing 
											  : MovieMixerState.Seeking;
						}
					}
					/* Do nothing when clipState == ClipState.Prepare */
					if (clipState == ClipState.Idle || clipAsset.guid != m_lastClipId) {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
					}
					break;
				case MovieMixerState.Playing:
					if (clipState != ClipState.Play || clipAsset.guid != m_lastClipId) {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
						break;
					}
					if (clipState != ClipState.Idle && clipAsset.IsMovieInfoReady == false) {
						if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Ready ||
							m_boundMovieMaterial.player.status == CriMana.Player.Status.ReadyForRendering ||
							m_boundMovieMaterial.player.status == CriMana.Player.Status.Playing
						) {
							clipAsset.ReplaceMovieInfo(m_boundMovieMaterial.player.movieInfo);
						}
					}
					break;
				case MovieMixerState.Stopping:
					if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Stop) {
						m_movieMixerState = MovieMixerState.Stopped;
					}
					break;
				case MovieMixerState.Seeking:
					if (clipState == ClipState.Seek && clipAsset.guid == m_lastClipId) {
						if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Ready ||
							m_boundMovieMaterial.player.status == CriMana.Player.Status.ReadyForRendering ||
							m_boundMovieMaterial.player.status == CriMana.Player.Status.Playing) {
							clipAsset.ReplaceMovieInfo(m_boundMovieMaterial.player.movieInfo);
							if (this.StopForSeekMovie()) {
								m_movieMixerState = MovieMixerState.SeekStopping;
							}
						}
					} else if (clipState == ClipState.Play) {	/* impossible: switched from edit to play mode with clipState untouched */
						m_movieMixerState = MovieMixerState.Playing;
					} else {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
					}
					break;
				case MovieMixerState.SeekStopping:
					if (clipState != ClipState.Seek || clipAsset.guid != m_lastClipId) {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
					} else {
						if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Stop) {
							m_movieMixerState = MovieMixerState.SeekStopped;
						}
					}
					break;
				case MovieMixerState.SeekStopped:
					if (clipState == ClipState.Seek && clipAsset.guid == m_lastClipId) {
						if (this.PlayMovie(clipAsset, clipAsset.GetSeekFrame(frameTime - activeClip.start, clipAsset.m_loopWithinClip))) {
							m_movieMixerState = MovieMixerState.Seeking;
						}
					} else {
						if (this.StopMovie()) {
							m_movieMixerState = MovieMixerState.Stopping;
						}
					}
					break;
				default:
					break;
			}

			/* Update movie material in Editor Mode */
			if (IsEditMode && m_boundMovieMaterial != null && m_boundMovieMaterial.player != null) {
				var _seekPlaybackIEnumrator = seekPlaybackEnumerator();
				do {	/* Forcing update on intermediate states */
					m_boundMovieMaterial.PlayerManualUpdate();
				} while (_seekPlaybackIEnumrator.MoveNext());

				if (m_boundMovieMaterial.renderMode == CriManaMovieMaterial.RenderMode.Always) {
					m_boundMovieMaterial.player.OnWillRenderObject(m_boundMovieMaterial);
				}
			}
		}

		IEnumerator seekPlaybackEnumerator() {
			if (m_PlayableDirector.state != PlayState.Playing) {
				switch (m_boundMovieMaterial.player.status) {
					case CriMana.Player.Status.Dechead:
					case CriMana.Player.Status.WaitPrep:
					case CriMana.Player.Status.Prep:
					case CriMana.Player.Status.StopProcessing:
						yield return 0;
						break;
					default:
						break;
				}
			}
		}

		public override void OnBehaviourPlay(Playable playable, FrameData info) {
			base.OnBehaviourPlay(playable, info);

			if (m_boundMovieMaterial != null && m_boundMovieMaterial.player != null) {
				m_boundMovieMaterial.player.Pause(false);
			}
		}

		public override void OnBehaviourPause(Playable playable, FrameData info) {
			base.OnBehaviourPause(playable, info);

			if (m_boundMovieMaterial != null && m_boundMovieMaterial.player != null) {
				if (m_boundMovieMaterial.player.status == CriMana.Player.Status.Playing) {
					m_boundMovieMaterial.player.Pause(true);
				} else {
					if (this.StopMovie()) {
						m_movieMixerState = MovieMixerState.Stopping;
					}
				}
			}
		}

		public override void OnGraphStart(Playable playable) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=white>OnGraphStart" + (IsEditMode ? " (Editor)</color>" : "</color>"));
#endif
			base.OnGraphStart(playable);

			if (IsEditMode) {
				if (m_boundMovieMaterial != null) {
					m_boundMovieMaterial.PlayerManualInitialize();
					m_boundMovieMaterial.PlayerManualSetup();
				}
			}
		}

		public override void OnGraphStop(Playable playable) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=white>OnGraphStop" + (IsEditMode ? " (Editor)</color>" : "</color>"));
#endif
			base.OnGraphStop(playable);

			if (IsEditMode) {
				if (this.StopMovie()) {
					m_movieMixerState = MovieMixerState.Stopping;
				}
				if (m_boundMovieMaterial != null) {
					m_boundMovieMaterial.PlayerManualFinalize();
				}
			}
		}

		public override void OnPlayableCreate(Playable playable) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=white>OnPlayableCreate" + (IsEditMode ? " (Editor)</color>" : "</color>"));
#endif
			base.OnPlayableCreate(playable);
			if (IsEditMode) {
				if (CriManaPlugin.IsLibraryInitialized() == false) {
					CriWareInitializer cfg = GameObject.FindObjectOfType<CriWareInitializer>();
					if (cfg != null) {
						CriWareInitializer.InitializeMana(cfg.manaConfig);
					} else {
						CriWareInitializer.InitializeMana(new CriManaConfig());
						Debug.Log("[CRIWARE] Timeline / Mana: Can't find CriWareInitializer component; Using default parameters in edit mode.");
					}
				}
			}

			m_lastClipId = null;
			m_movieMixerState = MovieMixerState.Stopped;
		}

		public override void OnPlayableDestroy(Playable playable) {
#if CRI_TIMELINE_MANA_VERBOSE_DEBUG
			Debug.Log("<color=white>OnPlayableDestroy" + (IsEditMode ? " (Editor)</color>" : "</color>"));
#endif
			base.OnPlayableDestroy(playable);

			if (IsEditMode == false) {
				this.StopMovie();
			}
		}
	}
}

#endif