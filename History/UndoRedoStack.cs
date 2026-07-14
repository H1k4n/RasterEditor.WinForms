namespace RasterEditor.WinFormsDemo.History;

public sealed class UndoRedoStack<T> : IDisposable where T : class, IDisposable
{
    private readonly Stack<T> _undo = new();
    private readonly Stack<T> _redo = new();
    private readonly int _capacity;

    public UndoRedoStack(int capacity = 20)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void PushUndo(T item)
    {
        _undo.Push(item);
        _redo.Clear();
        Trim(_undo);
    }

    public T PopUndo()
    {
        var item = _undo.Pop();
        return item;
    }

    public bool TryPopUndo(out T? item)
    {
        if (_undo.Count == 0)
        {
            item = null;
            return false;
        }

        item = _undo.Pop();
        return true;
    }

    public void PushRedo(T item)
    {
        _redo.Push(item);
        Trim(_redo);
    }

    public T PopRedo()
    {
        var item = _redo.Pop();
        return item;
    }

    public bool TryPopRedo(out T? item)
    {
        if (_redo.Count == 0)
        {
            item = null;
            return false;
        }

        item = _redo.Pop();
        return true;
    }

    public void Clear()
    {
        DisposeStack(_undo);
        DisposeStack(_redo);
    }

    private void Trim(Stack<T> stack)
    {
        if (stack.Count <= _capacity) return;

        // ToArray: index 0 = top of stack (newest), last index = bottom (oldest).
        // Keep the newest _capacity entries (indices 0.._capacity-1), dispose the rest.
        var arr = stack.ToArray();
        stack.Clear();
        var keep = Math.Min(_capacity, arr.Length);
        for (var i = keep - 1; i >= 0; i--)
            stack.Push(arr[i]);
        for (var i = _capacity; i < arr.Length; i++)
            arr[i].Dispose();
    }

    private static void DisposeStack(Stack<T> stack)
    {
        while (stack.Count > 0)
        {
            stack.Pop().Dispose();
        }
    }

    public void Dispose()
    {
        Clear();
    }
}

