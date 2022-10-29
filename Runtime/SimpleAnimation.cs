using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed partial class SimpleAnimation : MonoBehaviour
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public interface IState
    {
        bool enabled { get; set; }
        bool isValid { get; }
        float time { get; set; }
        float normalizedTime { get; set; }
        float speed { get; set; }
        string name { get; set; }
        float weight { get; set; }
        float length { get; }
        AnimationClip clip { get; }
        WrapMode wrapMode { get; set; }
    }

    // ReSharper disable InconsistentNaming

    public Animator animator
    {
        get
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            return _animator;
        }
    }

    [Obsolete] public bool animatePhysics => m_AnimatePhysics;

    public AnimatorCullingMode cullingMode
    {
        get => animator.cullingMode;
        set
        {
            m_CullingMode = value;
            animator.cullingMode = m_CullingMode;
        }
    }

    public bool isPlaying => _playable.IsPlaying();

    public bool playAutomatically
    {
        get => m_PlayAutomatically;
        set => m_PlayAutomatically = value;
    }

    public AnimationClip clip
    {
        get => m_Clip;
        set
        {
            LegacyClipCheck(value);
            m_Clip = value;
        }
    }

    public WrapMode wrapMode
    {
        get => m_WrapMode;
        set => m_WrapMode = value;
    }

    // ReSharper restore InconsistentNaming

    public void AddClip(AnimationClip clip, string newName)
    {
        LegacyClipCheck(clip);
        AddState(clip, newName);
    }

    public void Blend(string stateName, float targetWeight, float fadeLength)
    {
        _animator.enabled = true;
        Kick();
        _playable.Blend(stateName, targetWeight, fadeLength);
    }

    public void CrossFade(string stateName, float fadeLength)
    {
        _animator.enabled = true;
        Kick();
        _playable.Crossfade(stateName, fadeLength);
    }

    public void CrossFadeQueued(string stateName, float fadeLength, QueueMode queueMode)
    {
        _animator.enabled = true;
        Kick();
        _playable.CrossfadeQueued(stateName, fadeLength, queueMode);
    }

    public int GetClipCount()
    {
        return _playable.GetClipCount();
    }

    public bool IsPlaying(string stateName)
    {
        return _playable.IsPlaying(stateName);
    }

    public void Stop()
    {
        _playable.StopAll();
    }

    public void Stop(string stateName)
    {
        _playable.Stop(stateName);
    }

    public void Sample()
    {
        _graph.Evaluate();
    }

    public bool Play()
    {
        _animator.enabled = true;
        Kick();
        if (m_Clip != null && m_PlayAutomatically)
        {
            _playable.Play(DefaultStateName);
        }

        return false;
    }

    public void AddState(AnimationClip clip, string name)
    {
        LegacyClipCheck(clip);
        Kick();
        if (_playable.AddClip(clip, name))
        {
            RebuildStates();
        }
    }

    public void RemoveState(string name)
    {
        if (_playable.RemoveClip(name))
        {
            RebuildStates();
        }
    }

    public bool Play(string stateName)
    {
        _animator.enabled = true;
        Kick();
        return _playable.Play(stateName);
    }

    public void PlayQueued(string stateName, QueueMode queueMode)
    {
        _animator.enabled = true;
        Kick();
        _playable.PlayQueued(stateName, queueMode);
    }

    public void RemoveClip(AnimationClip clip)
    {
        if (clip == null)
            throw new System.NullReferenceException("clip");

        if (_playable.RemoveClip(clip))
        {
            RebuildStates();
        }
    }

    public void Rewind()
    {
        Kick();
        _playable.Rewind();
    }

    public void Rewind(string stateName)
    {
        Kick();
        _playable.Rewind(stateName);
    }

    public IState GetState(string stateName)
    {
        SimpleAnimationPlayable.IState state = _playable.GetState(stateName);
        if (state == null)
            return null;

        return new StateImpl(state, this);
    }

    public IEnumerable<IState> GetStates()
    {
        return new StateEnumerable(this);
    }

    public IState this[string name] => GetState(name);
}