using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SoundThief.Editor
{
    [CustomPropertyDrawer(typeof(ContractRepeatPolicyAttribute))]
    public sealed class ContractRepeatPolicyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var toggle = new Toggle("재수락 불가능") { tooltip = property.tooltip, value = property.boolValue };
            toggle.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            toggle.BindProperty(property);
            return toggle;
        }
    }
}
