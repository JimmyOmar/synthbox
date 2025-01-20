using Sandbox;
using System;
using System.Collections.Generic;

public class KnobReceiver : Component
{
    [Property] public Action<Vector3, Vector3, float, bool, bool> UpdateAction { get; set; }

    private List<Action> rotationSenders = new List<Action>();
    private HighlightOutline highlightOutline;
    private bool isHovering;
    private List<KnobSender> knobSenders = new List<KnobSender>();
    private ButtonSender buttonSender1;
    private ButtonSender buttonSender2;
    private bool button1State;
    private bool button2State;

    protected override void OnStart()
    {
        // Find the HighlightOutline component
        highlightOutline = GameObject.GetComponent<HighlightOutline>();

        // Recursively find all KnobSender components
        FindKnobSenders(GameObject);

        // Recursively find all ButtonSender components
        FindButtonSenders(GameObject);

        // Subscribe to button events
        if (buttonSender1 != null)
        {
            buttonSender1.OnBoolChanged += HandleButton1Changed;
            Log.Info("Found ButtonSender1");
        }

        if (buttonSender2 != null)
        {
            buttonSender2.OnBoolChanged += HandleButton2Changed;
            Log.Info("Found ButtonSender2");
        }
    }

    private void FindKnobSenders(GameObject obj)
    {
        foreach (var child in obj.Children)
        {
            var knobSender = child.GetComponent<KnobSender>();
            if (knobSender != null)
            {
                knobSender.OnPitchChanged += HandlePitchChanged;
                knobSenders.Add(knobSender);
            }
            // Recursively find KnobSender components in the child's children
            FindKnobSenders(child);
        }
    }

    private void FindButtonSenders(GameObject obj)
    {
        foreach (var child in obj.Children)
        {
            var buttonSender = child.GetComponent<ButtonSender>();
            if (buttonSender != null)
            {
                if (buttonSender1 == null)
                {
                    buttonSender1 = buttonSender;
                }
                else if (buttonSender2 == null)
                {
                    buttonSender2 = buttonSender;
                }
            }
            // Recursively find ButtonSender components in the child's children
            FindButtonSenders(child);
        }
    }

    private void HandlePitchChanged(float pitch)
    {
        // Handle the pitch value received from KnobSender
    }

    private void HandleButton1Changed(bool state)
    {
        button1State = state;
        // Handle the state change from ButtonSender1
    }

    private void HandleButton2Changed(bool state)
    {
        button2State = state;
        // Handle the state change from ButtonSender2
    }

    public void HandleButtonChanged(bool state)
    {
        // Handle the state change from any ButtonSender
    }

    protected override void OnUpdate()
    {
        // Create a Vector3 to hold the scaled pitch values from the first three KnobSender components
        Vector3 knobValues1 = new Vector3();
        for (int i = 0; i < Math.Min(3, knobSenders.Count); i++)
        {
            knobValues1[i] = knobSenders[i].ScaledPitch;
        }

        // Create a Vector3 to hold the scaled pitch values from the next three KnobSender components
        Vector3 knobValues2 = new Vector3();
        for (int i = 3; i < Math.Min(6, knobSenders.Count); i++)
        {
            knobValues2[i - 3] = knobSenders[i].ScaledPitch;
        }

        // Get the scaled pitch value from the seventh KnobSender component
        float knobValue7 = knobSenders.Count > 6 ? knobSenders[6].ScaledPitch : 0.0f;

        // Continuously update the action graph with the knob values and button states
        UpdateAction?.Invoke(knobValues1, knobValues2, knobValue7, button1State, button2State);

        // Update the action graph with the rotation values from all senders
        foreach (var sender in rotationSenders)
        {
            sender.Invoke();
        }

        // Update highlight outline based on hover state
        if (highlightOutline != null)
        {
            highlightOutline.InsideColor = highlightOutline.InsideColor.WithAlpha(isHovering ? 0.5f : 0.0f);
        }
    }

    public void RegisterRotationSender(Action sender)
    {
        rotationSenders.Add(sender);
        UpdateAction?.Invoke(new Vector3(), new Vector3(), 0.0f, button1State, button2State);
    }
}