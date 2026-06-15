using System.Collections.Generic;
using UnityEngine;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// UGUI 프리팹 전용 오브젝트 풀.
    /// Presenter가 인스턴스를 소유하고, Get/Return으로 재사용.
    /// </summary>
    public sealed class UIObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _defaultParent;
        private readonly Stack<T> _pool = new();

        /// <param name="prefab">풀링할 프리팹</param>
        /// <param name="defaultParent">Get 시 기본 부모 Transform</param>
        /// <param name="initialSize">씬 로드 시 미리 생성할 수</param>
        public UIObjectPool(T prefab, Transform defaultParent, int initialSize = 0)
        {
            _prefab = prefab;
            _defaultParent = defaultParent;

            for (int i = 0; i < initialSize; i++)
                _pool.Push(CreateInstance(_defaultParent));
        }

        /// <summary>
        /// 풀에서 꺼내 활성화. 풀이 비어 있으면 새로 생성.
        /// </summary>
        public T Get(Transform parent = null)
        {
            var instance = _pool.Count > 0 ? _pool.Pop() : CreateInstance(parent ?? _defaultParent);

            instance.transform.SetParent(parent ?? _defaultParent, false);
            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// 사용 완료된 인스턴스를 풀에 반납.
        /// </summary>
        public void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            _pool.Push(instance);
        }

        /// <summary>
        /// 활성화된 인스턴스를 전부 반납. 탭 전환 등 목록 초기화 시 사용.
        /// </summary>
        public void ReleaseAll(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.TryGetComponent<T>(out var instance))
                    Return(instance);
            }
        }

        private T CreateInstance(Transform parent)
        {
            var instance = Object.Instantiate(_prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}