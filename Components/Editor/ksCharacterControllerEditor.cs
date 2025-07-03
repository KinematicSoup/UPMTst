using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// A class that used to override Unity character controller editor. This class appends extra collider data
    /// used by Reactor to the Unity character controller in the inspector.
    /// </summary>
    [CustomEditor(typeof(CharacterController))]
    [CanEditMultipleObjects]
    public class ksCharacterControllerEditor : ksOverrideEditor
    {
        private static GUIContent m_foldoutLabel;

        // Don't show these fields.
        private static readonly HashSet<string> m_hideFields = new HashSet<string>
        {
            "ShapeId", "IsSimulation", "IsQuery", "ContactOffset"
        };

        /// <summary>Static initialization</summary>
        static ksCharacterControllerEditor()
        {
            ksInspectorNames.Get().Add((CharacterController controller) => ksColliderEditor.GetName(controller));
        }

        protected override void OnEnable() { LoadBaseEditor("CharacterControllerEditor"); }

        /// <summary>
        /// Draw the base inspector without changes and append inspector GUI elements for the <see cref="ksColliderData"/>
        /// associated with the target collider.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ksColliderEditor.DrawColliderData(targets, m_hideFields);
        }
    }
}
