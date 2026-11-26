// ArchNet · Input Module — one line of the Controls screen
// An action (or one direction of a movement) with its keyboard / mouse key and its gamepad
// button. Clicking a button waits for the new key.
// Docs: https://archnet-plateform.fr/#ModuleInputSystem

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArchNet.Module
{
    public class ControlRow : MonoBehaviour
    {
        const string KeyboardDevices = "<Keyboard>";
        const string GamepadDevices = "<Gamepad>";

        [SerializeField] TMP_Text _label;
        [SerializeField] Button _keyboard;
        [SerializeField] TMP_Text _keyboardText;
        [SerializeField] Button _gamepad;
        [SerializeField] TMP_Text _gamepadText;

        InputAction _action;
        int _keyboardIndex = -1;
        int _gamepadIndex = -1;

        public void Set(ControlsScreen screen, string label, InputAction action, int keyboardIndex, int gamepadIndex)
        {
            _action = action;
            _keyboardIndex = keyboardIndex;
            _gamepadIndex = gamepadIndex;
            if (_label != null) _label.text = label;
            Wire(_keyboard, () => screen.Rebind(_action, _keyboardIndex, KeyboardDevices, this));
            Wire(_gamepad, () => screen.Rebind(_action, _gamepadIndex, GamepadDevices, this));
            Refresh();
        }

        public void Refresh()
        {
            Show(_keyboard, _keyboardText, _keyboardIndex);
            Show(_gamepad, _gamepadText, _gamepadIndex);
        }

        public void ShowWaiting(int bindingIndex)
        {
            TMP_Text text = bindingIndex == _keyboardIndex ? _keyboardText : _gamepadText;
            if (text != null) text.text = "Press a key… (Esc: cancel)";
        }

        void Show(Button button, TMP_Text text, int index)
        {
            if (button != null) button.interactable = index >= 0;
            if (text != null) text.text = index >= 0 ? _action.GetBindingDisplayString(index) : "—";
        }

        static void Wire(Button button, UnityEngine.Events.UnityAction onClick)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }
}
