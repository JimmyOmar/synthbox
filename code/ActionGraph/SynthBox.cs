using Sandbox;
using System;
using System.Collections.Concurrent;

public static class AudioConstants
{
    public const int BufferSize = 1024; // Buffer size (unfortunately, the effectiveness of adjusting the buffer size to reduce delay and pops/clicks was limited)
    public const int SampleRate = 44100;
    public const int ProcessAudioCalls = 3; // Number of times ProcessAudio is called
}

public class SboxSynthesizer
{
    private SoundStream soundStream;
    private SoundHandle soundHandle;
    private float gain;
    private ConcurrentQueue<short> bufferQueue;
    private float previousSample;
    private float[] hanningWindow;

    public SboxSynthesizer()
    {
        soundStream = new SoundStream(AudioConstants.SampleRate);
        gain = 1;
        bufferQueue = new ConcurrentQueue<short>();
        previousSample = 0;
        hanningWindow = GenerateHanningWindow(AudioConstants.BufferSize);
    }

    public void SetGain(float newGain)
    {
        gain = newGain;
    }

    public void Play(Vector3 position)
    {
        if (!soundHandle.IsValid())
        {
            soundHandle = soundStream.Play();
            soundHandle.Position = position;
        }
        // Ensure audio is processed frequently
        ProcessAudio(AudioConstants.ProcessAudioCalls);
    }

    public void SetPosition(Vector3 position)
    {
        if (soundHandle.IsValid())
        {
            soundHandle.Position = position;
        }
    }

    public void WriteData(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float sample = data[i] * hanningWindow[i];
            float smoothedSample = SmoothSample(sample);
            short scaledSample = (short)(Math.Clamp(smoothedSample * gain, -1.0f, 1.0f) * short.MaxValue);
            bufferQueue.Enqueue(scaledSample);
        }
        // Process audio immediately after writing data
        ProcessAudio(AudioConstants.ProcessAudioCalls);
    }

    private float SmoothSample(float sample)
    {
        float smoothedSample = (previousSample + sample) / 2;
        previousSample = smoothedSample;
        return smoothedSample;
    }

    private float[] GenerateHanningWindow(int size)
    {
        float[] window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.5f * (1 - (float)Math.Cos(2 * Math.PI * i / (size - 1)));
        }
        return window;
    }

    public void ProcessAudio()
    {
        int samplesToWrite = Math.Min(AudioConstants.BufferSize, soundStream.MaxWriteSampleCount - soundStream.QueuedSampleCount);
        short[] outputBuffer = new short[samplesToWrite];
        
        for (int i = 0; i < samplesToWrite; i++)
        {
            if (bufferQueue.TryDequeue(out short sample))
            {
                outputBuffer[i] = sample;
            }
            else
            {
                outputBuffer[i] = 0; // Fill with silence if no data
            }
        }

        // Send outputBuffer to the audio output
        soundStream.WriteData(outputBuffer);
    }

    public void ProcessAudio(int times)
    {
        for (int i = 0; i < times; i++)
        {
            ProcessAudio();
        }
    }

    public void Dispose()
    {
        soundStream.Dispose();
    }
}

public enum EnvelopeState
{
    Idle,
    Attack,
    Decay,
    Sustain,
    Release
}

public class ADSREnvelope
{
    public EnvelopeState State { get; private set; } = EnvelopeState.Idle;
    private int sampleIndex = 0;
    private float envelopeValue = 0.0f;
    private int attackSamples;
    private int decaySamples;
    private int releaseSamples;
    private float sustainLevel;

    public void Trigger(float attack, float decay, float sustain, float release)
    {
        State = EnvelopeState.Attack;
        sampleIndex = 0;
        envelopeValue = 0.0f;
        attackSamples = (int)(attack * AudioConstants.SampleRate);
        decaySamples = (int)(decay * AudioConstants.SampleRate);
        releaseSamples = (int)(release * AudioConstants.SampleRate);
        sustainLevel = sustain;
    }

    public void Release()
    {
        if (State == EnvelopeState.Sustain)
        {
            State = EnvelopeState.Release;
            sampleIndex = 0;
        }
    }

    public float[] Apply(float[] waveform)
    {
        float[] result = new float[AudioConstants.BufferSize];

        for (int i = 0; i < AudioConstants.BufferSize; i++)
        {
            switch (State)
            {
                case EnvelopeState.Attack:
                    if (sampleIndex < attackSamples)
                    {
                        envelopeValue = sampleIndex / (float)attackSamples;
                        result[i] = waveform[i] * envelopeValue;
                        sampleIndex++;
                    }
                    else
                    {
                        State = EnvelopeState.Decay;
                        sampleIndex = 0;
                    }
                    break;

                case EnvelopeState.Decay:
                    if (sampleIndex < decaySamples)
                    {
                        envelopeValue = 1 - ((sampleIndex / (float)decaySamples) * (1 - sustainLevel));
                        result[i] = waveform[i] * envelopeValue;
                        sampleIndex++;
                    }
                    else
                    {
                        State = EnvelopeState.Sustain;
                        sampleIndex = 0;
                    }
                    break;

                case EnvelopeState.Sustain:
                    result[i] = waveform[i] * sustainLevel;
                    break;

                case EnvelopeState.Release:
                    if (sampleIndex < releaseSamples)
                    {
                        envelopeValue *= 1 - (sampleIndex / (float)releaseSamples);
                        result[i] = waveform[i] * envelopeValue;
                        sampleIndex++;
                    }
                    else
                    {
                        State = EnvelopeState.Idle;
                        sampleIndex = 0;
                        envelopeValue = 0.0f;
                    }
                    break;

                case EnvelopeState.Idle:
                    result[i] = 0;
                    break;
            }
        }

        return result;
    }
}

public class SineWaveNode
{
    private static double phase = 0.0;

    /// Generates a continuous sine wave at a specified frequency.
    [ActionGraphNode("audio.generatesinewave"), Pure]
    [Title("Generate Sine Wave"), Group("Audio"), Icon("waves")]
    public static float[] GenerateSineWave(float[] frequencyArray)
    {
        float[] output = new float[AudioConstants.BufferSize];
        
        for (int i = 0; i < AudioConstants.BufferSize; i++)
        {
            double phaseIncrement = 2.0 * Math.PI * frequencyArray[i] / AudioConstants.SampleRate;
            output[i] = (float)Math.Sin(phase);
            phase += phaseIncrement;

            if (phase >= 2.0 * Math.PI)
            {
                phase -= 2.0 * Math.PI;
            }
        }
        return output;
    }
}

public class CustomAudioNodes
{
    private static SboxSynthesizer sboxSynthesizer = new SboxSynthesizer();
    private static ADSREnvelope envelope = new ADSREnvelope();

    /// Directs float input data to the custom audio stream.
    [ActionGraphNode("audio.outputAudio")]
    [Title("Output Audio"), Group("Audio"), Icon("volume_up")]
    public static void OutputFloatData(float[] data, Vector3 position)
    {
        sboxSynthesizer.Play(position);
        sboxSynthesizer.WriteData(data);
        sboxSynthesizer.ProcessAudio(); // Ensure audio is processed and sent to output
    }

    /// Multiplies two arrays of float data element-wise, but we recall it to 'waveform'.
    [ActionGraphNode("audio.multiplyWaveforms"), Pure]
    [Title("Multiply Waveforms"), Group("Audio"), Icon("calculate")]
    public static float[] MultiplyFloatArrays(float[] waveform1, float[] waveform2)
    {
        float[] result = new float[AudioConstants.BufferSize];
        for (int i = 0; i < AudioConstants.BufferSize; i++)
        {
            result[i] = waveform1[i] * waveform2[i];
        }

        return result;
    }

    /// Adds two arrays of float data (we refer to it as waveform, but it is just float arrays in reality).
    [ActionGraphNode("audio.addWaveforms"), Pure]
    [Title("Add Waveforms"), Group("Audio"), Icon("add")]
    public static float[] AddFloatArrays(float[] waveform1, float[] waveform2)
    {
        float[] result = new float[AudioConstants.BufferSize];
        for (int i = 0; i < AudioConstants.BufferSize; i++)
        {
            result[i] = (waveform1[i] + waveform2[i]) / 2.0f;
        }

        return result;
    }

    /// Converts a float to a float array.
    [ActionGraphNode("audio.floatToWaveform"), Pure]
    [Title("Float to Waveform"), Group("Audio"), Icon("array")]
    public static float[] FloatToFloatArray(float value)
    {
        float[] result = new float[AudioConstants.BufferSize];
        for (int i = 0; i < AudioConstants.BufferSize; i++)
        {
            result[i] = value;
        }

        return result;
    }

    /// Applies an ADSR envelope to a waveform.
    [ActionGraphNode("audio.applyADSR"), Pure]
    [Title("ADSR"), Group("Audio"), Icon("envelope")]
    public static float[] ApplyADSREnvelope(bool trigger, float[] waveform, float attack, float decay, float sustain, float release)
    {
        if (trigger)
        {
            envelope.Trigger(attack, decay, sustain, release);
        }
        else
        {
            envelope.Release();
        }
        return envelope.Apply(waveform);
    }
}