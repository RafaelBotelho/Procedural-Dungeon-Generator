using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Decoration", menuName = "Procedural Room/ Decoration")]
public class SO_Decorations : ScriptableObject
{
    #region Variables / Components

    [SerializeField] private GameObject _prefab = default;
    [SerializeField] private float _size = 0;
    [SerializeField] private Vector3 _positionOffSet = Vector3.zero;
    [SerializeField] private Vector3 _rotationOffSet = Vector3.zero;
    [SerializeField] private bool _allowRandomRotation = false;

    #endregion

    #region Properties

    public GameObject prefab => _prefab;
    public float size => _size;
    public Vector3 positionOffSet => _positionOffSet;
    public Vector3 rotationOffSet => _rotationOffSet;
    public bool allowRandomRotation => _allowRandomRotation;

    #endregion
}
