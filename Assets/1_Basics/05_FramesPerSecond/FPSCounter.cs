using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public int FrameRange = 60;

    public int AverageFPS { get; private set; }
    public int HighestFPS { get; private set; }
    public int LowestFPS { get; private set; }

    private int[] _fpsBuffer;
    private int _fpsBufferIndex;

    private void Update()
    {
        if (_fpsBuffer == null || _fpsBuffer.Length != FrameRange)
        {
            InitializeBuffer();
        }

        UpdateBuffer();
        CalculateFPS();
    }

    private void InitializeBuffer()
    {
        if (FrameRange <= 0)
        {
            FrameRange = 1;
        }

        _fpsBuffer = new int[FrameRange];
        _fpsBufferIndex = 0;
    }

    private void UpdateBuffer()
    {
        _fpsBuffer[_fpsBufferIndex++] = (int) (1f / Time.unscaledDeltaTime);
        if (_fpsBufferIndex >= FrameRange)
        {
            _fpsBufferIndex = 0;
        }
    }

    private void CalculateFPS()
    {
        var sum = 0;
        var highest = 0;
        var lowest = int.MaxValue;
        for (int i = 0; i < FrameRange; i++)
        {
            var fps = _fpsBuffer[i];
            sum += fps;
            if (fps > highest)
            {
                highest = fps;
            }

            if (fps < lowest)
            {
                lowest = fps;
            }
        }

        AverageFPS = sum / FrameRange;
        HighestFPS = highest;
        LowestFPS = lowest;
    }
}