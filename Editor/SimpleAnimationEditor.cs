using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleAnimation))]
public class SimpleAnimationEditor : Editor
{
    private static class Styles
    {
        public static readonly GUIContent Animation = new("Animation",
            "The clip that will be played if Play() is called, or if \"Play Automatically\" is enabled");

        public static readonly GUIContent Animations =
            new("Animations", "These clips will define the States the component will start with");

        public static readonly GUIContent PlayAutomatically =
            new("Play Automatically", "If checked, the default clip will automatically be played");

        public static readonly GUIContent UpdateMode =
            new("Update Mode", "Controls when and how often the Animator is updated");

        public static readonly GUIContent CullingMode =
            new("Culling Mode", "Controls what is updated when the object has been culled");
    }

    private SerializedProperty _clip;
    private SerializedProperty _states;
    private SerializedProperty _playAutomatically;
    private SerializedProperty _updateMode;
    private SerializedProperty _cullingMode;

    private void OnEnable()
    {
        _clip = serializedObject.FindProperty("m_Clip");
        _states = serializedObject.FindProperty("m_States");
        _playAutomatically = serializedObject.FindProperty("m_PlayAutomatically");
        _updateMode = serializedObject.FindProperty("_updateMode");
        _cullingMode = serializedObject.FindProperty("m_CullingMode");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(_clip, Styles.Animation);
        EditorGUILayout.PropertyField(_states, Styles.Animations, true);
        EditorGUILayout.PropertyField(_playAutomatically, Styles.PlayAutomatically);
        EditorGUILayout.PropertyField(_updateMode, Styles.UpdateMode);
        EditorGUILayout.PropertyField(_cullingMode, Styles.CullingMode);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(SimpleAnimation.EditorState))]
class StateDrawer : PropertyDrawer
{
    private class Styles
    {
        public static readonly GUIContent DisabledTooltip = new("",
            "The Default state cannot be edited, change the Animation clip to change the Default State");
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        EditorGUILayout.BeginHorizontal();
        // Calculate rects
        var clipRect = new Rect(position.x, position.y, position.width / 2 - 5, position.height);
        var nameRect = new Rect(position.x + position.width / 2 + 5, position.y, position.width / 2 - 5,
            position.height);


        EditorGUI.BeginDisabledGroup(property.FindPropertyRelative("defaultState").boolValue);
        EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("clip"), GUIContent.none);
        EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("name"), GUIContent.none);
        if (property.FindPropertyRelative("defaultState").boolValue)
        {
            EditorGUI.LabelField(position, Styles.DisabledTooltip);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}