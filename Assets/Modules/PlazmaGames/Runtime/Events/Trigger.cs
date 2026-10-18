using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlazmaGames.Core.Events;
using PlazmaGames.Core;
using UnityEngine.EventSystems;
using System;

namespace PlazmaGames.Events
{
    //[RequireComponent(typeof(Collider), typeof(Rigidbody))]
    //public abstract class BaseTrigger<TEvent> : MonoBehaviour where TEvent : IComparable
    //{
    //    [Header("Settings")]
    //    [SerializeField] protected TEvent _eventType;
    //    [SerializeField] protected string _triggerTag = string.Empty;
    //    [SerializeField] protected bool _allowMultipleTriggers = false;

    //    protected Collider _col;
    //    protected Rigidbody _rb;
    //    protected bool _isTriggered;

    //    protected virtual void OnTriggerEnter(Collider other)
    //    {
    //        bool canTrigger = !_isTriggered || _allowMultipleTriggers;

    //        if (canTrigger && (_triggerTag == string.Empty || other.tag == _triggerTag))
    //        {
    //            _isTriggered = true;
    //            GameManager.EmitEvent(_eventType, other.GetComponent<Component>(), null);
    //        }
    //    }

    //    protected virtual void Awake()
    //    {
    //        _col = GetComponent<Collider>();
    //        _rb = GetComponent<Rigidbody>();
    //        _rb.isKinematic = true;
    //        _col.isTrigger = true;
    //    }
    //}
}
