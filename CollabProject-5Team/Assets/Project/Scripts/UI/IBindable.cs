namespace GameDevTycoon.UI
{
    /// <summary>
    /// 프리팹 데이터 바인딩 공통 인터페이스.
    /// Canvas에서 Bind(data) 한 줄로 호출하기 위한 컨벤션.
    /// </summary>
    public interface IBindable<TData>
    {
        void Bind(TData data);
    }
}