using System;
using System.Collections.Generic;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// A stack machine implementation that can be used for both instruction stack and operand stack
/// </summary>
/// <typeparam name="T">The type of items in the stack</typeparam>
public class StackMachine<T>
{
    // Stack frames, where each frame is a range of items in the stack
    public List<int> FrameBottomIdxStack = new();

    // The actual items in the stack
    public List<T> Items = new();

    // The top index of the stack
    public int StackTop = -1;

    /// <summary>
    /// Creates a new stack machine
    /// </summary>
    /// <param name="createInitFrame">Whether to create an initial frame</param>
    public StackMachine(bool createInitFrame = false)
    {
        if (createInitFrame)
        {
            PushFrame();
        }
    }

    /// <summary>
    /// Gets a view of the items in the current frame
    /// </summary>
    public List<T> FrameStackView
    {
        get
        {
            var result = new List<T>();
            var currentFrameIdx = CurFrameBottomIdx();

            if (currentFrameIdx <= StackTop)
            {
                for (var i = currentFrameIdx; i <= StackTop; i++)
                {
                    result.Add(Items[i]);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Creates a copy of this stack machine
    /// </summary>
    public StackMachine<T> Copy()
    {
        var result = new StackMachine<T>();
        result.FrameBottomIdxStack = new List<int>(FrameBottomIdxStack);
        result.Items = new List<T>(Items);
        result.StackTop = StackTop;

        return result;
    }

    public void Restore(StackMachine<T> other)
    {
        FrameBottomIdxStack = new List<int>(other.FrameBottomIdxStack);
        Items = new List<T>(other.Items);
        StackTop = other.StackTop;
    }

    /// <summary>
    /// Gets the current top index of the stack
    /// </summary>
    public int GetCurTopIdx() => StackTop;

    /// <summary>
    /// Swaps two items in the stack by their indices
    /// </summary>
    /// <param name="index1">First index</param>
    /// <param name="index2">Second index</param>
    public void SwapByIndex(int index1, int index2)
    {
        (this.Items[index1], this.Items[index2]) = (this.Items[index2], this.Items[index1]);
    }

    /// <summary>
    /// Gets an item from the stack by its index
    /// </summary>
    /// <param name="idx">The index</param>
    /// <returns>The item at the given index</returns>
    public T GetByIndex(int idx) => Items[idx];

    /// <summary>
    /// Pushes a new frame onto the stack
    /// </summary>
    public void PushFrame()
    {
        FrameBottomIdxStack.Add(StackTop + 1);
    }

    /// <summary>
    /// Jumps to a specific index in the stack
    /// </summary>
    /// <param name="valStackIdx">The index to jump to</param>
    public void JumpTo(int valStackIdx)
    {
        var popTimes = StackTop - valStackIdx;
        for (var i = 0; i < popTimes; i++)
        {
            PopValue();
        }
    }

    /// <summary>
    /// Pushes multiple items onto the stack
    /// </summary>
    /// <param name="items">The items to push</param>
    public void PushItems(List<T> items)
    {
        foreach (var item in items)
        {
            PushValue(item);
        }
    }

    /// <summary>
    /// Pushes multiple items onto the stack in reverse order
    /// </summary>
    /// <param name="items">The items to push</param>
    public void ReversePushItems(List<T> items)
    {
        for (var i = items.Count - 1; i >= 0; i--)
        {
            PushValue(items[i]);
        }
    }

    /// <summary>
    /// Pushes a single value onto the stack
    /// </summary>
    /// <param name="value">The value to push</param>
    public void PushValue(T value)
    {
        if (Items.Count <= (StackTop + 1))
        {
            Items.Add(value);
        }
        else
        {
            Items[StackTop + 1] = value;
        }

        StackTop += 1;
    }

    /// <summary>
    /// Pops a value from the stack
    /// </summary>
    /// <returns>The popped value</returns>
    public T PopValue()
    {
        if (StackTop < 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty stack");
        }

        var top = Items[StackTop];
        StackTop -= 1;

        return top;
    }

    /// <summary>
    /// Peeks at the top value of the stack without removing it
    /// </summary>
    /// <returns>The top value</returns>
    public T PeekTop()
    {
        if (StackTop < 0)
        {
            throw new InvalidOperationException("Cannot peek an empty stack");
        }

        return Items[StackTop];
    }

    /// <summary>
    /// Peeks at the bottom value of the current frame without removing it
    /// </summary>
    /// <returns>The bottom value of the current frame</returns>
    public T PeekBottomOfCurFrame()
    {
        var bottomIdx = CurFrameBottomIdx();

        if (bottomIdx < 0 || bottomIdx > StackTop)
        {
            throw new InvalidOperationException("Invalid frame bottom index");
        }

        return Items[bottomIdx];
    }

    /// <summary>
    /// Peeks at the bottom value of all frames without removing it
    /// </summary>
    /// <returns>The bottom value of all frames</returns>
    public T PeekBottomOfAllFrames()
    {
        if (Items.Count == 0)
        {
            throw new InvalidOperationException("Cannot peek an empty stack");
        }

        return Items[0];
    }

    /// <summary>
    /// Pops all values in the current frame
    /// </summary>
    /// <returns>The popped values</returns>
    public List<T> PopFrameAllValues()
    {
        var result = new List<T>();

        if (FrameBottomIdxStack.Count == 0)
        {
            throw new InvalidOperationException("No frames to pop");
        }

        var currentFrameIdx = FrameBottomIdxStack[^1];
        FrameBottomIdxStack.RemoveAt(FrameBottomIdxStack.Count - 1);

        var frameValCnt = StackTop - currentFrameIdx + 1;

        if (frameValCnt >= 0)
        {
            for (var i = 0; i < frameValCnt; i++)
            {
                var v = PopValue();
                result.Insert(0, v);
            }
        }

        return result;
    }

    /// <summary>
    /// Peeks at all values in the current frame and clears the frame
    /// </summary>
    /// <returns>The values in the frame</returns>
    public List<T> PeekAndClearFrameAllValues()
    {
        var result = PopFrameAllValues();
        PushFrame();
        return result;
    }

    /// <summary>
    /// Pops the current frame and pushes the top value
    /// </summary>
    public void PopFrameAndPushTopVal()
    {
        var frameValues = PopFrameAllValues();

        if (frameValues.Count > 0)
        {
            PushValue(frameValues[^1]);
        }
        else
        {
            PushValue(default);
        }
    }

    /// <summary>
    /// Gets the index of the bottom of the current frame
    /// </summary>
    /// <returns>The index of the bottom of the current frame</returns>
    public int CurFrameBottomIdx()
    {
        if (FrameBottomIdxStack.Count == 0)
        {
            throw new InvalidOperationException("No frames");
        }

        return FrameBottomIdxStack[^1];
    }
}