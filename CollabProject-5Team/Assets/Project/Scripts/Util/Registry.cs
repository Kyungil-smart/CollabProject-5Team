using System;
using System.Collections.Generic;
using System.Linq;

public delegate T SelectionStrategy<T>(IEnumerable<T> items);
/// <summary>
/// 다양한 타입의 객체를 등록하고 관리하는 C# 클래스
/// </summary>
public static class Registry<T> where T : class
{
    static HashSet<T> _items = new();
    public static int Count => _items.Count;
    public static event Action<T> OnRemoved;

    public static bool TryAdd(T item)
    {
        return item != null && _items.Add(item);
    }

    public static void Remove(T item)
    {
        if (_items.Remove(item)) OnRemoved?.Invoke(item);
    }

    public static T GetFirst()
    {
        return _items.FirstOrDefault();
    }

    public static T Get(SelectionStrategy<T> strategy) => strategy(_items);

    public static IEnumerable<T> All => _items;

    public static void Reset()
    {
        _items = new HashSet<T>();
        OnRemoved = null;
    }

    // 도메인 리로드 OFF 환경에서 플레이 시작 전 Registry 상태를 초기화
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    //static void ResetAll()
    //{
    //    Registry<>.Reset();
    //}
}
