/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2024 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using KS.Reactor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Property drawer for <see cref="ksMultiType"/>.</summary>
    [CustomPropertyDrawer(typeof(ksMultiType))]
    public class ksMultiTypeDrawer : PropertyDrawer
    {
        private delegate T Drawer<T>(Rect position, GUIContent label, T value);

        private const float LINE_HEIGHT = 18f;      // The height of a line.
        private const float PADDING = 2f;           // The padding between lines.
        private const float MIN_TYPE_WIDTH = 50f;   // The minimum width of the type selector.
        private const float MAX_TYPE_WIDTH = 120f;  // The maximum width of the type selector.
        private const float BUTTON_WIDTH = 20f;     // The width of the + and - buttons for arrays.
        private const string INDENT = "    ";       // Indentation for string labels.

        /// <summary>Stores expanded property state.</summary>
        private class ExpandedSet : ksInspectorStateMap<bool, ExpandedSet>
        {

        }

        private static GUIContent[] m_options;
        private static PseudoTypes[] m_types;
        private static Dictionary<PseudoTypes, int> m_typeToIndexMap;

        /// <summary>
        /// Types the property drawer can display. Some of these are true <see cref="ksMultiType.Types"/> and some
        /// are pseudo-types such as Vectors which internally are stored as float arrays. True types have only the
        /// first three bits set. Pseudotypes have higher bits set, and the first three bits represent the true type
        /// the multitype uses to store that type internally.
        /// </summary>
        private enum PseudoTypes : byte
        {
            // True types
            INT = ksMultiType.Types.INT,
            FLOAT = ksMultiType.Types.FLOAT,
            STRING = ksMultiType.Types.STRING,
            BOOL = ksMultiType.Types.BOOL,
            UINT = ksMultiType.Types.UINT,
            BYTE = ksMultiType.Types.BYTE,
            LONG = ksMultiType.Types.LONG,
            INT_ARRAY = ksMultiType.Types.INT_ARRAY,
            FLOAT_ARRAY = ksMultiType.Types.FLOAT_ARRAY,
            STRING_ARRAY = ksMultiType.Types.STRING_ARRAY,
            BOOL_ARRAY = ksMultiType.Types.BOOL_ARRAY,
            UINT_ARRAY = ksMultiType.Types.UINT_ARRAY,
            BYTE_ARRAY = ksMultiType.Types.BYTE_ARRAY,
            LONG_ARRAY = ksMultiType.Types.LONG_ARRAY,
            // Pseudo types
            VECTOR2 = ksMultiType.Types.FLOAT_ARRAY | (1 << 4),
            VECTOR3 = ksMultiType.Types.FLOAT_ARRAY | (2 << 4),
            COLOR = ksMultiType.Types.FLOAT_ARRAY | (3 << 4),
            VECTOR2_ARRAY = VECTOR2 | PSEUDO_ARRAY_FLAG,
            VECTOR3_ARRAY = VECTOR3 | PSEUDO_ARRAY_FLAG,
            COLOR_ARRAY = COLOR | PSEUDO_ARRAY_FLAG
        }

        private const byte PSEUDO_ARRAY_FLAG = 1 << 7; // Array flag for pseudo types.
        private const byte TRUE_ARRAY_FLAG = 8; // Array float for true types.
        private const byte TRUE_TYPE_MASK = 15; // Bitmask to get the true underlying type used by a pseudo-type.

        /// <summary>Static intialization.</summary>
        static ksMultiTypeDrawer()
        {
            // Create the type options.
            m_options = new GUIContent[20];
            m_types = new PseudoTypes[m_options.Length];
            m_typeToIndexMap = new Dictionary<PseudoTypes, int>();
            int i = 0;
            AddOption(i++, "Int", PseudoTypes.INT);
            AddOption(i++, "Float", PseudoTypes.FLOAT);
            AddOption(i++, "String", PseudoTypes.STRING);
            AddOption(i++, "Bool", PseudoTypes.BOOL);
            AddOption(i++, "UInt", PseudoTypes.UINT);
            AddOption(i++, "Byte", PseudoTypes.BYTE);
            AddOption(i++, "Long", PseudoTypes.LONG);
            AddOption(i++, "Vector2", PseudoTypes.VECTOR2);
            AddOption(i++, "Vector3", PseudoTypes.VECTOR3);
            AddOption(i++, "Color", PseudoTypes.COLOR);
            AddOption(i++, "Int Array", PseudoTypes.INT_ARRAY);
            AddOption(i++, "Float Array", PseudoTypes.FLOAT_ARRAY);
            AddOption(i++, "String Array", PseudoTypes.STRING_ARRAY);
            AddOption(i++, "Bool Array", PseudoTypes.BOOL_ARRAY);
            AddOption(i++, "UInt Array", PseudoTypes.UINT_ARRAY);
            AddOption(i++, "Byte Array", PseudoTypes.BYTE_ARRAY);
            AddOption(i++, "Long Array", PseudoTypes.LONG_ARRAY);
            AddOption(i++, "Vector2 Array", PseudoTypes.VECTOR2_ARRAY);
            AddOption(i++, "Vector3 Array", PseudoTypes.VECTOR3_ARRAY);
            AddOption(i++, "Color Array", PseudoTypes.COLOR_ARRAY);
        }

        /// <summary>
        /// Sets the type drop-down label at the specified index, and creates mappings between the type and index.
        /// </summary>
        /// <param name="index">Index of the option.</param>
        /// <param name="label">Label to display in the dropdown menu.</param>
        /// <param name="type">Type for this index.</param>
        private static void AddOption(int index, string label, PseudoTypes type)
        {
            m_options[index] = new GUIContent(label);
            m_types[index] = type;
            m_typeToIndexMap[type] = index;
        }

        /// <summary>
        /// Checks if a type is a true type. A type is true if it is one of the <see cref="ksMultiType.Types"/>.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <returns>True if the type is a true type.</returns>
        private static bool IsTrueType(PseudoTypes type)
        {
            return ((byte)type & ~TRUE_TYPE_MASK) == 0;
        }

        /// <summary>Checks if a type is an array type.</summary>
        /// <param name="type">Type to check.</param>
        /// <returns>True if the type is an array type.</returns>
        private static bool IsArrayType(PseudoTypes type)
        {
            return ((byte)type & (IsTrueType(type) ? TRUE_ARRAY_FLAG : PSEUDO_ARRAY_FLAG)) != 0;
        }

        /// <summary>Draws the property.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="property">Property to draw.</param>
        /// <param name="label">Label for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = LINE_HEIGHT;
            EditorGUI.BeginProperty(position, label, property);
            // Get the ksMultiType from the property.
            property.serializedObject.ApplyModifiedProperties();
            ksMultiType multiType = ksEditorUtils.GetPropertyValue<ksMultiType>(property);
            if (multiType == null)
            {
                position = EditorGUI.PrefixLabel(position, label);
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                EditorGUI.LabelField(position, "Error", style);
                EditorGUI.EndProperty();
                return;
            }
            // Clone the multi type to avoid modifying the one controlled by the property so undo works properly.
            multiType = multiType.Clone();

            Rect typePosition = position;
            typePosition.width = ksMath.Clamp(position.width / 5f, MIN_TYPE_WIDTH, MAX_TYPE_WIDTH);
            position.width -= typePosition.width;
            typePosition.x += position.width;
            SerializedProperty serializedProp = property.FindPropertyRelative("m_serialized");
            SerializedProperty typeProperty = serializedProp.FindPropertyRelative("PseudoType");
            PseudoTypes type = (PseudoTypes)typeProperty.intValue;
            bool isArray = IsArrayType(type);
            int index;
            if (!m_typeToIndexMap.TryGetValue(type, out index))
            {
                // If it's not one of the known types, clear the array flag.
                isArray = false;
            }

            // Get the expanded state.
            bool expanded = ExpandedSet.Get().Get(property);

            // Draw the multitype value.
            SerializedProperty dataProp = serializedProp.FindPropertyRelative("Data");
            SerializedProperty arrayLengthProp = serializedProp.FindPropertyRelative("ArrayLength");
            EditorGUI.showMixedValue = dataProp.hasMultipleDifferentValues ||
                (isArray && arrayLengthProp.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            DrawMultiType(position, typePosition.width, multiType, type, label, ref expanded);
            bool changed = EditorGUI.EndChangeCheck();

            // Draw the type selector.
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = typeProperty.hasMultipleDifferentValues;
            index = EditorGUI.Popup(typePosition, index, m_options);
            type = m_types[index];
            if (EditorGUI.EndChangeCheck() || changed)
            {
                // Expand when the type is changed to an array. Collapse when it is changed to a non-array.
                expanded = IsArrayType(type);
                typeProperty.intValue = (int)type;
                changed = true;
                // Convert the multitype to the new type.
                multiType.ConvertTo((ksMultiType.Types)((byte)type & TRUE_TYPE_MASK));
            }

            if (changed)
            {
                // Apply the changes.
                SetByteArrayProperty(dataProp, multiType.Data);
                arrayLengthProp.intValue = multiType.ArrayLength;
            }
            // Save the expanded state.
            ExpandedSet.Get().Set(property, expanded);
            EditorGUI.EndProperty();
        }

        /// <summary>Gets the height of the property.</summary>
        /// <param name="property">Property to get height for.</param>
        /// <param name="label">Property label.</param>
        /// <returns>The height of the property.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int numLines = 1;
            ksMultiType multiType = ksEditorUtils.GetPropertyValue<ksMultiType>(property);
            if (multiType != null && ExpandedSet.Get().Get(property))
            {
                SerializedProperty prop = property.FindPropertyRelative("m_serialized");
                prop = prop.FindPropertyRelative("PseudoType");
                PseudoTypes type = (PseudoTypes)prop.intValue;
                int arrayLength = multiType.ArrayLength;
                bool isArray = multiType.IsArray;
                switch (type)
                {
                    case PseudoTypes.VECTOR2:
                    case PseudoTypes.VECTOR3:
                    case PseudoTypes.COLOR:
                    {
                        isArray = false;
                        break;
                    }
                    case PseudoTypes.VECTOR2_ARRAY:
                    {
                        if (arrayLength > 0)
                        {
                            arrayLength /= 2;
                        }
                        break;
                    }
                    case PseudoTypes.VECTOR3_ARRAY:
                    {
                        if (arrayLength > 0)
                        {
                            arrayLength /= 3;
                        }
                        break;
                    }
                    case PseudoTypes.COLOR_ARRAY:
                    {
                        if (arrayLength > 0)
                        {
                            arrayLength /= 4;
                        }
                        break;
                    }
                }
                // If array length is less than 1, add 1 for the null toggle line.
                if (arrayLength < 1)
                {
                    arrayLength++;
                }
                numLines = isArray ? arrayLength + 2 : 1;
            }
            return LINE_HEIGHT * numLines + PADDING * (numLines - 1);
        }

        /// <summary>Sets the value of a byte array property.</summary>
        /// <param name="property">Byte array property to set.</param>
        /// <param name="array">Array value.</param>
        private void SetByteArrayProperty(SerializedProperty property, byte[] array)
        {
            property.arraySize = array.Length;
            for (int i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = array[i];
            }
        }

        /// <summary>Draws an editor for a byte.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="label">Label for the value.</param>
        /// <param name="value">Value</param>
        /// <returns>New value</returns>
        private byte DrawByte(Rect position, GUIContent label, byte value)
        {
            return (byte)ksMath.Clamp(EditorGUI.IntField(position, label, value), 0, 255);
        }

        /// <summary>Draws an editor for a uint.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="label">Label for the value.</param>
        /// <param name="value">Value</param>
        /// <returns>New value</returns>
        private uint DrawUInt(Rect position, GUIContent label, uint value)
        {
            return (uint)ksMath.Clamp(EditorGUI.LongField(position, label, value), 0, uint.MaxValue);
        }

        /// <summary>Draws an editor for a <see cref="Vector2"/>.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="label">Label for the value.</param>
        /// <param name="value">Value</param>
        /// <returns>New value</returns>
        private Vector2 DrawVector2(Rect position, GUIContent label, Vector2 value)
        {
            // If you pass a non-empty label to Unity's Vector2Field, it sometimes takes up 2 lines instead of 1
            // depending on how much width is available, which messes up our positioning. To force it to use 1 line,
            // we add a separate prefix label, then call Vector2Field with an empty label.
            Rect labelPosition = position;
            position = EditorGUI.PrefixLabel(labelPosition, label);
            float dx = (position.x - labelPosition.x) / 3f;
            position.x -= dx;
            position.width += dx;
            return EditorGUI.Vector2Field(position, "", value);
        }

        /// <summary>Draws an editor for a <see cref="Vector3"/>.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="label">Label for the value.</param>
        /// <param name="value">Value</param>
        /// <returns>New value</returns>
        private Vector3 DrawVector3(Rect position, GUIContent label, Vector3 value)
        {
            // If you pass a non-empty label to Unity's Vector3Field, it sometimes takes up 2 lines instead of 1
            // depending on how much width is available, which messes up our positioning. To force it to use 1 line,
            // we add a separate prefix label, then call Vector3Field with an empty label.
            Rect labelPosition = position;
            position = EditorGUI.PrefixLabel(labelPosition, label);
            float dx = (position.x - labelPosition.x) / 3f;
            position.x -= dx;
            position.width += dx;
            return EditorGUI.Vector3Field(position, "", value);
        }

        /// <summary>Draws an editor for a templated array.</summary>
        /// <typeparam name="T">Array element type.</typeparam>
        /// <param name="position">Position to draw at.</param>
        /// <param name="typeWidth">The width used by the type selector.</param>
        /// <param name="label">Label to draw.</param>
        /// <param name="array">Array value.</param>
        /// <param name="drawer">Delegate for drawing an element of the array.</param>
        /// <param name="expanded">Is the foldout expanded? If false, does not draw the array elements.</param>
        /// <returns>New array value.</returns>
        private T[] DrawArray<T>(Rect position, float typeWidth, GUIContent label, T[] array, Drawer<T> drawer, ref bool expanded)
        {
            Rect valuePosition = EditorGUI.PrefixLabel(position, new GUIContent(" "));
            Rect labelPosition = position;
            labelPosition.width -= valuePosition.width + PADDING;
            // Draw the foldout.
            expanded = EditorGUI.Foldout(labelPosition, expanded, label, true);

            // Draw the array size.
            int arrayLength = array == null ? -1 : array.Length;
            int length = Math.Max(-1, EditorGUI.IntField(valuePosition, "", arrayLength));
            // You can't give a tooltip to something without a label, so create an empty label over the int field to
            // hold a tooltip.
            EditorGUI.LabelField(position, new GUIContent("", "Array Size"));
            if (length != arrayLength)
            {
                if (length < 0)
                {
                    array = null;
                }
                else
                {
                    T[] newArray = new T[length];
                    int count = Math.Min(length, arrayLength);
                    for (int i = 0; i < count; i++)
                    {
                        newArray[i] = array[i];
                    }
                    array = newArray;
                }
                expanded = true;
            }
            if (!expanded)
            {
                return array;
            }

            position.width += typeWidth;
            position.y += LINE_HEIGHT + PADDING;
            // If the array is null or empty, show a null toggle.
            if (array == null || array.Length == 0)
            {
                GUIContent labelTooltip = new GUIContent(INDENT + "Null", 
                    "Does the ksMultiType represent a null array?");
                if (EditorGUI.Toggle(position, labelTooltip, array == null))
                {
                    // If the null toggle is checked, don't draw any further controls.
                    return null;
                }
                position.y += LINE_HEIGHT + PADDING;
                if (array == null)
                {
                    array = new T[0];
                }
            }
            // Draw each element.
            for (int i = 0; i < array.Length; i++)
            {
                // Set the control name to the index. We use the control name to get the selected index later.
                GUI.SetNextControlName(i.ToString());
                array[i] = drawer(position, new GUIContent(INDENT + i), array[i]);
                position.y += LINE_HEIGHT + PADDING;
            }
            position.x += position.width - BUTTON_WIDTH;
            position.width = BUTTON_WIDTH;
            // Draw the - button to delete an element.
            if (GUI.Button(position, "-"))
            {
                int index = GetSelectedIndex(array.Length);
                T[] newArray = new T[array.Length - 1];
                for (int i = 0; i < newArray.Length; i++)
                {
                    newArray[i] = array[i < index ? i : (i + 1)];
                }
                array = newArray;
            }
            position.x -= BUTTON_WIDTH + PADDING;
            // Draw the + button to add a new element.
            if (GUI.Button(position, "+"))
            {
                int index = GetSelectedIndex(array.Length);
                T[] newArray = new T[array.Length + 1];
                for (int i = 0; i < array.Length; i++)
                {
                    newArray[i < index ? i : (i + 1)] = array[i];
                }
                array = newArray;
            }
            return array;
        }

        /// <summary>Gets the selected array index by converting focused control name to an int.</summary>
        /// <param name="arrayLength">Array length</param>
        /// <returns>The selected index, or <paramref name="arrayLength"/> if no index is selected.</returns>
        private int GetSelectedIndex(int arrayLength)
        {
            int index;
            if (!int.TryParse(GUI.GetNameOfFocusedControl(), out index))
            {
                index = arrayLength;
            }
            return index;
        }

        /// <summary>
        /// Draws an editor for a multitype's value for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="typeWidth">The width used by the type selector.</param>
        /// <param name="multiType">Multi type value.</param>
        /// <param name="type">Type of editor to draw.</param>
        /// <param name="label">Label to draw.</param>
        /// <param name="expanded">Is the foldout expanded? If false, does not draw the array elements.</param>
        private void DrawMultiType(
            Rect position,
            float typeWidth,
            ksMultiType multiType,
            PseudoTypes type,
            GUIContent label,
            ref bool expanded)
        {
            switch (type)
            {
                case PseudoTypes.BOOL:
                {
                    multiType.Bool = EditorGUI.Toggle(position, label, multiType);
                    break;
                }
                case PseudoTypes.BOOL_ARRAY:
                {
                    multiType.BoolArray = DrawArray<bool>(position, typeWidth, label, multiType, EditorGUI.Toggle,
                        ref expanded);
                    break;
                }
                case PseudoTypes.BYTE:
                {
                    multiType.Byte = DrawByte(position, label, multiType);
                    break;
                }
                case PseudoTypes.BYTE_ARRAY:
                {
                    multiType.ByteArray = DrawArray<byte>(position, typeWidth, label, multiType, DrawByte,
                        ref expanded);
                    break;
                }
                case PseudoTypes.FLOAT:
                {
                    multiType.Float = EditorGUI.FloatField(position, label, multiType);
                    break;
                }
                case PseudoTypes.FLOAT_ARRAY:
                {
                    multiType.FloatArray = DrawArray<float>(position, typeWidth, label, multiType,
                        EditorGUI.FloatField, ref expanded);
                    break;
                }
                default:
                case PseudoTypes.INT:
                {
                    multiType.Int = EditorGUI.IntField(position, label, multiType);
                    break;
                }
                case PseudoTypes.INT_ARRAY:
                {
                    multiType.IntArray = DrawArray<int>(position, typeWidth, label, multiType, EditorGUI.IntField,
                        ref expanded);
                    break;
                }
                case PseudoTypes.LONG:
                {
                    multiType.Long = EditorGUI.LongField(position, label, multiType);
                    break;
                }
                case PseudoTypes.LONG_ARRAY:
                {
                    multiType.LongArray = DrawArray<long>(position, typeWidth, label, multiType, EditorGUI.LongField,
                        ref expanded);
                    break;
                }
                case PseudoTypes.STRING:
                {
                    multiType.String = EditorGUI.TextField(position, label, multiType);
                    break;
                }
                case PseudoTypes.STRING_ARRAY:
                {
                    multiType.StringArray = DrawArray<string>(position, typeWidth, label, multiType,
                        EditorGUI.TextField, ref expanded);
                    break;
                }
                case PseudoTypes.UINT:
                {
                    multiType.UInt = DrawUInt(position, label, multiType);
                    break;
                }
                case PseudoTypes.UINT_ARRAY:
                {
                    multiType.UIntArray = DrawArray<uint>(position, typeWidth, label, multiType, DrawUInt,
                        ref expanded);
                    break;
                }
                case PseudoTypes.VECTOR2:
                {
                    multiType.Vector2 = DrawVector2(position, label, multiType);
                    break;
                }
                case PseudoTypes.VECTOR2_ARRAY:
                {
                    multiType.Vector2Array = DrawArray<Vector2>(position, typeWidth, label, multiType, DrawVector2,
                        ref expanded);
                    break;
                }
                case PseudoTypes.VECTOR3:
                {
                    multiType.Vector3 = DrawVector3(position, label, multiType);
                    break;
                }
                case PseudoTypes.VECTOR3_ARRAY:
                {
                    multiType.Vector3Array = DrawArray<Vector3>(position, typeWidth, label, multiType, DrawVector3,
                        ref expanded);
                    break;
                }
                case PseudoTypes.COLOR:
                {
                    multiType.Color = EditorGUI.ColorField(position, label, multiType);
                    break;
                }
                case PseudoTypes.COLOR_ARRAY:
                {
                    multiType.ColorArray = DrawArray<Color>(position, typeWidth, label, multiType,
                        EditorGUI.ColorField, ref expanded);
                    break;
                }
            }
        }
    }
}