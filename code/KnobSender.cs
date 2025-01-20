using Sandbox;
using System;

/// KnobSender is responsible for sending automation updates to a KnobReceiver component (for adjust synth parameters).
public class KnobSender : Component, Component.IPressable, PlayerController.IEvents
{
    private KnobReceiver pressComponent;
    private HighlightOutline highlightOutline;
    private GameObject currentHoverer;
    private GameObject currentPresser;
    private bool isPressed;
    private float mousePitch;
    private float accumulatedPitch;

    public event Action<float> OnPitchChanged;

    [Property, Range(0, 1), Title("Scaled Pitch")]
    public float ScaledPitch { get; private set; }

    /// Called when the component starts. Finds and registers the KnobReceiver component.
    protected override void OnStart()
    {
        // Find the KnobReceiver component in the parent or ancestor objects
        pressComponent = FindKnobReceiverComponent(GameObject);

        if (highlightOutline != null)
        {
            highlightOutline.Color = highlightOutline.Color.WithAlpha(0.0f);
        }

        // Find the HighlightOutline component
        highlightOutline = GameObject.GetComponent<HighlightOutline>();
    }

    /// Recursively finds the KnobReceiver component in the parent or ancestor objects.
    private KnobReceiver FindKnobReceiverComponent(GameObject obj)
    {
        while (obj != null)
        {
            var press = obj.GetComponent<KnobReceiver>();
            if (press != null)
            {
                return press;
            }
            obj = obj.Parent;
        }
        return null;
    }

    bool Component.IPressable.Press(Component.IPressable.Event e)
    {
        currentPresser = e.Source.GameObject;
        isPressed = true;
        mousePitch = 0; // Reset mousePitch on press
        if (highlightOutline != null)
        {
            highlightOutline.Color = (Color)Color.Parse("#19FF80");
        }
        return true;
    }

    void Component.IPressable.Release(Component.IPressable.Event e)
    {
        isPressed = false;
        if (highlightOutline != null)
        {
            if (currentHoverer == null)
            {
                highlightOutline.Color = (Color)Color.Parse("#E9FF3D00");
            }
            else
            {
                highlightOutline.Color = (Color)Color.Parse("#E9FF3D");
            }
        }
    }

    bool Component.IPressable.CanPress(Component.IPressable.Event e)
    {
        return true;
    }

    void Component.IPressable.Hover(Component.IPressable.Event e)
    {
        if (highlightOutline != null)
        {
            highlightOutline.Color = highlightOutline.Color.WithAlpha(1f);
        }
        currentHoverer = e.Source.GameObject;
    }

    void Component.IPressable.Blur(Component.IPressable.Event e)
    {
        currentHoverer = null;
        if (highlightOutline != null)
        {
            highlightOutline.Color = highlightOutline.Color.WithAlpha(0.0f);
        }
    }

    protected override void OnUpdate()
    {
        if (isPressed)
        {
            float pitchChange = Input.AnalogLook.pitch;
            mousePitch += pitchChange;
            mousePitch = mousePitch.Clamp(-45, 45);
            accumulatedPitch += pitchChange;
            accumulatedPitch = accumulatedPitch.Clamp(-180, 180);
            ScaledPitch = 1 - ((accumulatedPitch + 180) / 360); // Scale to 1 to 0
            
            OnPitchChanged?.Invoke(ScaledPitch);

            if (highlightOutline != null)
            {
                // Interpolate color from green to red based on ScaledPitch
                float redValue = 1 - ScaledPitch;
                float greenValue = ScaledPitch;
                highlightOutline.Color = new Color(redValue, greenValue, 0);
            }
        }
    }
}