using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public partial class SimpleAnimation : IAnimationClipSource
{
    private const string DefaultStateName = "Default";

    private class StateEnumerable : IEnumerable<IState>
    {
        private readonly SimpleAnimation _owner;

        public StateEnumerable(SimpleAnimation owner)
        {
            _owner = owner;
        }

        public IEnumerator<IState> GetEnumerator()
        {
            return new StateEnumerator(_owner);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new StateEnumerator(_owner);
        }

        private class StateEnumerator : IEnumerator<IState>
        {
            private readonly SimpleAnimation _owner;
            private readonly IEnumerator<SimpleAnimationPlayable.IState> _impl;

            public StateEnumerator(SimpleAnimation owner)
            {
                _owner = owner;
                _impl = _owner._playable.GetStates().GetEnumerator();
                Reset();
            }

            private IState GetCurrent()
            {
                return new StateImpl(_impl.Current, _owner);
            }

            object IEnumerator.Current => GetCurrent();

            IState IEnumerator<IState>.Current => GetCurrent();

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return _impl.MoveNext();
            }

            public void Reset()
            {
                _impl.Reset();
            }
        }
    }

    private sealed class StateImpl : IState
    {
        private readonly SimpleAnimationPlayable.IState _stateHandle;
        private readonly SimpleAnimation _component;

        public StateImpl(SimpleAnimationPlayable.IState handle, SimpleAnimation component)
        {
            _stateHandle = handle;
            _component = component;
        }

        bool IState.enabled
        {
            get => _stateHandle.enabled;
            set
            {
                _stateHandle.enabled = value;
                if (value)
                {
                    _component.Kick();
                }
            }
        }

        bool IState.isValid => _stateHandle.IsValid();

        float IState.time
        {
            get => _stateHandle.time;
            set
            {
                _stateHandle.time = value;
                _component.Kick();
            }
        }

        float IState.normalizedTime
        {
            get => _stateHandle.normalizedTime;
            set
            {
                _stateHandle.normalizedTime = value;
                _component.Kick();
            }
        }

        float IState.speed
        {
            get => _stateHandle.speed;
            set
            {
                _stateHandle.speed = value;
                _component.Kick();
            }
        }

        string IState.name
        {
            get => _stateHandle.name;
            set { _stateHandle.name = value; }
        }

        float IState.weight
        {
            get => _stateHandle.weight;
            set
            {
                _stateHandle.weight = value;
                _component.Kick();
            }
        }

        float IState.length => _stateHandle.length;

        AnimationClip IState.clip => _stateHandle.clip;

        WrapMode IState.wrapMode
        {
            get => _stateHandle.wrapMode;
            set => Debug.LogError("Not Implemented");
        }
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class EditorState
    {
        public AnimationClip? clip;
        public string name = null!;
        public bool defaultState;
    }

    private void Kick()
    {
        if (_isPlaying) return;
        _graph.Play();
        _isPlaying = true;
    }

    private PlayableGraph _graph;
    private PlayableHandle _layerMixer;
    private PlayableHandle _transitionMixer;
    private Animator? _animator;
    private bool _initialized;
    private bool _isPlaying;

    private SimpleAnimationPlayable? _playable;

    // ReSharper disable InconsistentNaming

    [SerializeField] private bool m_PlayAutomatically = true;

    [SerializeField, Obsolete(nameof(_updateMode))]
    private bool m_AnimatePhysics;

    [SerializeField] private AnimatorUpdateMode _updateMode;

    [SerializeField] private AnimatorCullingMode m_CullingMode = AnimatorCullingMode.CullUpdateTransforms;

    [SerializeField] private WrapMode m_WrapMode;

    [SerializeField] private AnimationClip? m_Clip;

    [SerializeField] private EditorState[]? m_States;

    // ReSharper restore InconsistentNaming

    private AnimatorUpdateMode GetUpdateMode()
    {
#pragma warning disable CS0618
        if (m_AnimatePhysics)
        {
            m_AnimatePhysics = false;
#pragma warning restore CS0618
            _updateMode = AnimatorUpdateMode.AnimatePhysics;
        }

        return _updateMode;
    }

    private void OnEnable()
    {
        Initialize();
        _graph.Play();
        if (m_PlayAutomatically)
        {
            Stop();
            Play();
        }
    }

    private void OnDisable()
    {
        if (_initialized)
        {
            Stop();
            _graph.Stop();
        }
    }

    private void Reset()
    {
        if (_graph.IsValid())
            _graph.Destroy();

        _initialized = false;
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _animator = GetComponent<Animator>();
        _animator.updateMode = GetUpdateMode();
        _animator.cullingMode = m_CullingMode;
        _graph = PlayableGraph.Create();
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var template = new SimpleAnimationPlayable();

        var playable = ScriptPlayable<SimpleAnimationPlayable>.Create(_graph, template, 1);
        _playable = playable.GetBehaviour();
        _playable.onDone += OnPlayableDone;
        if (m_States == null)
        {
            m_States = new EditorState[1];
            m_States[0] = new EditorState
            {
                defaultState = true,
                name = "Default"
            };
        }


        if (m_States != null)
        {
            foreach (var state in m_States)
            {
                if (state.clip)
                {
                    _playable.AddClip(state.clip, state.name);
                }
            }
        }

        EnsureDefaultStateExists();

        AnimationPlayableUtilities.Play(_animator, _playable.playable, _graph);
        Play();
        Kick();
        _initialized = true;
    }

    private void EnsureDefaultStateExists()
    {
        if (_playable != null && m_Clip != null && _playable.GetState(DefaultStateName) == null)
        {
            _playable.AddClip(m_Clip, DefaultStateName);
            Kick();
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }
    }

    private void OnPlayableDone()
    {
        _graph.Stop();
        _isPlaying = false;
    }

    private void RebuildStates()
    {
        var playableStates = GetStates();
        var list = new List<EditorState>();
        foreach (var state in playableStates)
        {
            var newState = new EditorState();
            newState.clip = state.clip;
            newState.name = state.name;
            list.Add(newState);
        }

        m_States = list.ToArray();
    }

    private EditorState CreateDefaultEditorState()
    {
        var defaultState = new EditorState
        {
            name = "Default",
            clip = m_Clip,
            defaultState = true
        };

        return defaultState;
    }

    private static void LegacyClipCheck(AnimationClip clip)
    {
        if (clip && clip.legacy)
        {
            throw new ArgumentException(
                $"Legacy clip {clip} cannot be used in this component. Set .legacy property to false before using this clip");
        }
    }

    private void InvalidLegacyClipError(string clipName, string stateName)
    {
        Debug.LogErrorFormat(this.gameObject,
            "Animation clip {0} in state {1} is Legacy. Set clip.legacy to false, or reimport as Generic to use it with SimpleAnimationComponent",
            clipName, stateName);
    }

    private void OnValidate()
    {
        //Don't mess with runtime data
        if (Application.isPlaying)
            return;

        if (m_Clip && m_Clip.legacy)
        {
            Debug.LogErrorFormat(this.gameObject,
                "Animation clip {0} is Legacy. Set clip.legacy to false, or reimport as Generic to use it with SimpleAnimationComponent",
                m_Clip.name);
            m_Clip = null;
        }

        //Ensure at least one state exists
        if (m_States == null || m_States.Length == 0)
        {
            m_States = new EditorState[1];
        }

        //Create default state if it's null
        if (m_States[0] == null)
        {
            m_States[0] = CreateDefaultEditorState();
        }

        //If first state is not the default state, create a new default state at index 0 and push back the rest
        if (m_States[0].defaultState == false || m_States[0].name != "Default")
        {
            var oldArray = m_States;
            m_States = new EditorState[oldArray.Length + 1];
            m_States[0] = CreateDefaultEditorState();
            oldArray.CopyTo(m_States, 1);
        }

        //If default clip changed, update the default state
        if (m_States[0].clip != m_Clip)
            m_States[0].clip = m_Clip;


        //Make sure only one state is default
        for (var i = 1; i < m_States.Length; i++)
        {
            if (m_States[i] == null)
            {
                m_States[i] = new EditorState();
            }

            m_States[i].defaultState = false;
        }

        //Ensure state names are unique
        var stateCount = m_States.Length;
        var names = new string[stateCount];

        for (var i = 0; i < stateCount; i++)
        {
            var state = m_States[i];
            if (state.name == "" && state.clip)
            {
                state.name = state.clip.name;
            }

#if UNITY_EDITOR
            state.name = ObjectNames.GetUniqueName(names, state.name);
#endif
            names[i] = state.name;

            if (state.clip && state.clip.legacy)
            {
                InvalidLegacyClipError(state.clip.name, state.name);
                state.clip = null;
            }
        }

        _animator = GetComponent<Animator>();
        _animator.updateMode = GetUpdateMode();
        _animator.cullingMode = m_CullingMode;
    }

    public void GetAnimationClips(List<AnimationClip> results)
    {
        foreach (var state in m_States)
        {
            if (state.clip != null)
                results.Add(state.clip);
        }
    }
}