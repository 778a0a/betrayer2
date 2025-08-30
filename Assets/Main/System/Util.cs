using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public static class Util
{
    public static float MaxWith(this float value, float max) => Mathf.Min(value, max);
    public static float MaxWith(this float value, float max, float max2) => Mathf.Min(value, Mathf.Min(max, max2));
    public static float MaxWith(this float value, float max, float max2, float max3) => Mathf.Min(value, Mathf.Min(max, Mathf.Min(max2, max3)));
    public static int MaxWith(this int value, int max) => Mathf.Min(value, max);
    public static int MaxWith(this int value, int max, int max2) => Mathf.Min(value, Mathf.Min(max, max2));
    public static int MaxWith(this int value, int max, int max2, int max3) => Mathf.Min(value, Mathf.Min(max, Mathf.Min(max2, max3)));

    public static float MinWith(this float value, float min) => Mathf.Max(value, min);
    public static float MinWith(this float value, float min, float min2) => Mathf.Max(value, Mathf.Max(min, min2));
    public static float MinWith(this float value, float min, float min2, float min3) => Mathf.Max(value, Mathf.Max(min, Mathf.Max(min2, min3)));
    public static int MinWith(this int value, int min) => Mathf.Max(value, min);
    public static int MinWith(this int value, int min, int min2) => Mathf.Max(value, Mathf.Max(min, min2));
    public static int MinWith(this int value, int min, int min2, int min3) => Mathf.Max(value, Mathf.Max(min, Mathf.Max(min2, min3)));

    public static float Clamp(this float value, float min, float max) => Mathf.Clamp(value, min, max);
    public static int Clamp(this int value, int min, int max) => Mathf.Clamp(value, min, max);
    public static float RandomRange(this float min, float max) => Random.Range(min, max);
    public static int RandomRange(this int min, int max) => Random.Range(min, max);

    public static bool Chance(this float probability) => Random.value < probability;
    public static bool Chance(this double probability) => Random.value < (float)probability;

    public static TEnum[] EnumArray<TEnum>()
    {
        return (TEnum[])Enum.GetValues(typeof(TEnum));
    }

    public static T RandomPick<T>(this IList<T> list) => list[Random.Range(0, list.Count)];
    public static T RandomPick<T>(this IEnumerable<T> list) => list.ElementAt(Random.Range(0, list.Count()));
    public static T RandomPickDefault<T>(this IList<T> list) => list.Count == 0 ? default : RandomPick(list);
    public static T RandomPickDefault<T>(this IEnumerable<T> list) => list.Count() == 0 ? default : RandomPick(list);
    public static T RandomPickWeighted<T>(this IEnumerable<T> l, Func<T, float> weightFunc, bool debug = false)
    {
        var list = l.ToList();
        if (list.Count == 0) return default;

        var totalWeight = list.Sum(weightFunc);
        if (debug)
        {
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                var weight = weightFunc(item);
                if (weight == 0) continue;
                sb.Append($"{100 * weight / totalWeight:00}% {item} ({weight})\n");
            }
            Debug.Log("重み:\n" + sb.ToString());
        }

        var value = Random.Range(0f, totalWeight);
        foreach (var item in list)
        {
            var weight = weightFunc(item);
            if (value < weight) return item;
            value -= weight;
        }
        return list[^1];
    }


    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(_ => Random.value);
    public static T[] ShuffleAsArray<T>(this IEnumerable<T> source) => source.ToArray().ShuffleInPlace();
    public static T[] ShuffleInPlace<T>(this T[] source) => (T[])ShuffleInPlace((IList<T>)source);
    public static IList<T> ShuffleInPlace<T>(this IList<T> source)
    {
        // Fisher-Yatesアルゴリズムでシャッフルを行う。
        for (var i = source.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (source[j], source[i]) = (source[i], source[j]);
        }
        return source;
    }


    public static Color Color(string code)
    {
        if (code[0] == '#')
        {
            code = code[1..];
        }
        if (code.Length == 3)
        {
            var r = Convert.ToInt32(code[0].ToString(), 16) / 15f;
            var g = Convert.ToInt32(code[1].ToString(), 16) / 15f;
            var b = Convert.ToInt32(code[2].ToString(), 16) / 15f;
            return new Color(r, g, b);
        }
        else
        {
            var r = Convert.ToInt32(code.Substring(0, 2), 16) / 255f;
            var g = Convert.ToInt32(code.Substring(2, 2), 16) / 255f;
            var b = Convert.ToInt32(code.Substring(4, 2), 16) / 255f;
            return new Color(r, g, b);
        }
    }

    public static Color Color(long val)
    {
        var r = (val >> 16) & 0xFF;
        var g = (val >> 8) & 0xFF;
        var b = val & 0xFF;
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    public static Color Color(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    public static DisplayStyle Display(bool on)
    {
        return on ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public static IDisposable Defer(Action act)
    {
        return new Defer(act);
    }

    /// <summary>
    /// awaitしない非同期メソッド呼び出しの警告を抑制するためのメソッド。
    /// </summary>
    public static void Foreget(this Awaitable _) { }
    /// <summary>
    /// awaitしない非同期メソッド呼び出しの警告を抑制するためのメソッド。
    /// </summary>
    public static void Foreget(this ValueTask _) { }

    [Obsolete("TODO", false)]
    public static void Todo(string memo = "")
    {
    }

    /// <summary>
    /// GZIP圧縮してBase64エンコードした文字列を返します。
    /// </summary>
    public static string CompressGzipBase64(string text)
    {
        var raw = Encoding.UTF8.GetBytes(text);
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(raw, 0, raw.Length);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Base64エンコードされたGZIP圧縮データを展開して文字列を返します。
    /// </summary>
    public static string DecompressGzipBase64(string encodedText)
    {
        var base64 = Convert.FromBase64String(encodedText);
        using var ms = new MemoryStream(base64);
        using var raw = new GZipStream(ms, CompressionMode.Decompress);
        using var rawMs = new MemoryStream();
        raw.CopyTo(rawMs);
        return Encoding.UTF8.GetString(rawMs.ToArray());
    }


    public static bool IsMarine(Traits t) => t.HasFlag(Traits.Admiral) || t.HasFlag(Traits.Pirate);
    public static bool IsMarine(Terrain t) => t == Terrain.River || t == Terrain.LargeRiver;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void IsTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            Debug.LogError($"アサーションエラー {message}");
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                System.Diagnostics.Debugger.Break();
                Debug.Break();
            }
#endif
            // throw new System.Exception("Assert failed: " + message);
        }
    }

    // 動的生成した要素を破棄するときのイベント解除が面倒なので自動でできるものを用意する。
    // うまく動くかは不明。（リストビューのアイテムのように、Detachされたあとにまた再利用されるものには使えなかった）
    public static void Register<TEventType>(this VisualElement el, EventCallback<TEventType> callback) where TEventType : EventBase<TEventType>, new ()
    {
        el.RegisterCallback(callback);
        void Dispose(DetachFromPanelEvent _)
        {
            el.UnregisterCallback(callback);
            el.UnregisterCallback<DetachFromPanelEvent>(Dispose);
        }
        el.RegisterCallback<DetachFromPanelEvent>(Dispose);
    }

    public static int IndexOf<T>(this IEnumerable<T> source, T target)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (EqualityComparer<T>.Default.Equals(item, target)) return index;
            index++;
        }
        return -1;
    }
}

public class Disposable : IDisposable
{
    private readonly Action dispose;
    public Disposable(Action dispose)
    {
        this.dispose = dispose;
    }
    public void Dispose() => dispose();
}

public class Defer : IDisposable
{
    private readonly Action act;
    public Defer(Action act) => this.act = act;
    public void Dispose() => act();
}

public class ValueTaskCompletionSource<TResult> : IValueTaskSource<TResult>
{
    private ManualResetValueTaskSourceCore<TResult> _core = new();
    private short _token;

    public ValueTaskCompletionSource() => _token = _core.Version;
    public ValueTask<TResult> Task => new(this, _token);
    public void SetResult(TResult result) => _core.SetResult(result);
    public void SetException(Exception exception) => _core.SetException(exception);
    public void Reset()
    {
        _token = (short)(_core.Version + 1);
        _core.Reset();
    }

    TResult IValueTaskSource<TResult>.GetResult(short token) => _core.GetResult(token);
    ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource<TResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}

public class ValueTaskCompletionSource : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<int> _core = new();
    private short _token;

    public ValueTaskCompletionSource() => _token = _core.Version;
    public ValueTask Task => new(this, _token);
    public void SetResult() => _core.SetResult(default);
    public void SetException(Exception exception) => _core.SetException(exception);
    public void Reset()
    {
        _token = (short)(_core.Version + 1);
        _core.Reset();
    }

    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}

// record型用
namespace System.Runtime.CompilerServices
{
    //[EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit
    {
    }
}