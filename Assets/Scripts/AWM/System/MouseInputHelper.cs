using UnityEngine;

/// <summary>
/// Used to be able to provide mouse input information when the game is running and when working in the editor and the game is not running.
/// </summary>
public class MouseInputHelper
{
    private static readonly bool[] m_mouseButtonPressed = new bool[3];
    private static Vector2 m_lastMousePosition = new Vector2();

    /// <summary>
    /// The current mouse position.
    /// </summary>
    public static Vector2 MousePosition
    {
        get
        {
            return Application.isPlaying ? (Vector2)Input.mousePosition : m_lastMousePosition;
        }
    }

    /// <summary>
    /// Updates the pressed state of all mouse buttons and the mouse position.
    /// Needed to make GetMouseButton work in the editor when not playing.
    /// </summary>
    public static void Update()
    {
        if (Event.current != null)
        {
            m_lastMousePosition.x = Event.current.mousePosition.x;
            m_lastMousePosition.y = -Event.current.mousePosition.y;
        }

        for (int i = 0; i < m_mouseButtonPressed.Length; i++)
        {
            if (m_mouseButtonPressed[i] && GetMouseButtonUp(i))
            {
                m_mouseButtonPressed[i] = false;
            }
            else if (!m_mouseButtonPressed[i] && GetMouseButtonDown(i))
            {
                m_mouseButtonPressed[i] = true;
            }
        }
    }

    /// <summary>
    /// Returns true while the mouse button is being pressed. (dragged when not playing)
    /// </summary>
    /// <param name="mouseButtonIndex">Index of the mouse button to check.</param>
    public static bool GetMouseButton(int mouseButtonIndex)
    {
        if (Application.isPlaying)
        {
            return Input.GetMouseButton(mouseButtonIndex);
        }

        return m_mouseButtonPressed[mouseButtonIndex];
    }

    /// <summary>
    /// Returns true when the mouse button was pressed down in this frame.
    /// </summary>
    /// <param name="mouseButtonIndex">Index of the mouse button to check.</param>
    public static bool GetMouseButtonDown(int mouseButtonIndex)
    {
        if (Application.isPlaying)
        {
            return Input.GetMouseButtonDown(mouseButtonIndex);
        }

        return Event.current != null && Event.current.type == EventType.MouseDown && 
            Event.current.button == mouseButtonIndex;
    }


    /// <summary>
    /// Returns true when the mouse button was lifted up in this frame.
    /// </summary>
    /// <param name="mouseButtonIndex">Index of the mouse button to check.</param>
    public static bool GetMouseButtonUp(int mouseButtonIndex)
    {
        if (Application.isPlaying)
        {
            return Input.GetMouseButtonUp(mouseButtonIndex);
        }

        return Event.current != null && Event.current.type == EventType.MouseUp && 
            Event.current.button == mouseButtonIndex;
    }
}
