using Sandbox;
using System;

/// <summary>
/// ButtonSender is responsible for sending a boolean value to a KnobReceiver component upon press/using the object.
/// </summary>
public class ButtonSender : Component, Component.IPressable, PlayerController.IEvents
{
    private KnobReceiver knobReceiver;
    private HighlightOutline highlightOutline;
    private GameObject currentHoverer;
    private GameObject currentPresser;
    private bool isPressed;
    private bool isToggled;

    public event Action<bool> OnBoolChanged;

    [Property, Title("Toggle Mode")]
    public bool ToggleMode { get; set; }

    /// <summary>
    /// Called when the component starts. Finds and registers the KnobReceiver component.
    /// </summary>
    protected override void OnStart()
    {
        // Find the KnobReceiver component in the parent or ancestor objects
        knobReceiver = FindKnobReceiverComponent(GameObject);

        if (highlightOutline != null)
        {
            highlightOutline.Color = highlightOutline.Color.WithAlpha(0.0f);
        }

        // Find the HighlightOutline component
        highlightOutline = GameObject.GetComponent<HighlightOutline>();
    }

    /// <summary>
    /// Recursively finds the KnobReceiver component in the parent or ancestor objects.
    /// </summary>
    private KnobReceiver FindKnobReceiverComponent(GameObject obj)
    {
        while (obj != null)
        {
            var receiver = obj.GetComponent<KnobReceiver>();
            if (receiver != null)
            {
                return receiver;
            }
            obj = obj.Parent;
        }
        return null;
    }

    bool Component.IPressable.Press(Component.IPressable.Event e)
    {
        currentPresser = e.Source.GameObject;
        isPressed = true;

        if (ToggleMode)
        {
            isToggled = !isToggled;
            OnBoolChanged?.Invoke(isToggled);
            knobReceiver?.HandleButtonChanged(isToggled);
            Log.Info($"Button Toggled: {isToggled}");
            if (highlightOutline != null)
            {
                highlightOutline.Color = isToggled ? (Color)Color.Parse("#19FF80") : (currentHoverer != null ? (Color)Color.Parse("#E9FF3D") : highlightOutline.Color.WithAlpha(0.0f));
            }
        }
        else
        {
            OnBoolChanged?.Invoke(true);
            knobReceiver?.HandleButtonChanged(true);
            Log.Info("Button Pressed: true");
            if (highlightOutline != null)
            {
                highlightOutline.Color = (Color)Color.Parse("#19FF80");
            }
        }
        return true;
    }

    void Component.IPressable.Release(Component.IPressable.Event e)
    {
        isPressed = false;
        if (!ToggleMode)
        {
            OnBoolChanged?.Invoke(false);
            knobReceiver?.HandleButtonChanged(false);
            Log.Info("Button Released: false");
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
            if (isToggled)
            {
                highlightOutline.Color = (Color)Color.Parse("#19FF80");
            }
            else
            {
                highlightOutline.Color = highlightOutline.Color.WithAlpha(0.0f);
            }
        }
    }

    protected override void OnUpdate()
    {
        // Continuously send the boolean value if not in toggle mode
        if (!ToggleMode)
        {
            OnBoolChanged?.Invoke(isPressed);
            knobReceiver?.HandleButtonChanged(isPressed);
        }
    }
}
