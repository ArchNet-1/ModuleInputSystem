// ArchNet · Input Module — Controls screen
// Lists the actions of one map (Gameplay by default) with their keyboard / mouse key and
// gamepad button; the player clicks one and presses the new key. Changes are saved at once
// and come back on the next launch. Open it from anywhere:
//
//   Services.Get<ControlsScreen>().Open();
//
// Docs: https://archnet-plateform.fr/#ModuleInputSystem

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArchNet.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArchNet.Module
{
    [RequireComponent(typeof(UIPanel))]
    public class ControlsScreen : GameService
    {
        const string KeyboardGroup = "Keyboard&Mouse";
        const string GamepadGroup = "Gamepad";

        [SerializeField] RectTransform _list;
        [Tooltip("Hidden template copied for each line.")]
        [SerializeField] ControlRow _rowTemplate;
        [SerializeField] Button _back;
        [SerializeField] Button _reset;
        [Tooltip("Action map whose keys the player may change.")]
        [SerializeField] string _map = "Gameplay";

        readonly List<ControlRow> _rows = new List<ControlRow>();
        InputActionRebindingExtensions.RebindingOperation _operation;
        UIPanel _panel;

        UIPanel Panel => _panel != null ? _panel : _panel = GetComponent<UIPanel>();

        protected override void Awake()
        {
            base.Awake();
            if (_rowTemplate != null) _rowTemplate.gameObject.SetActive(false);
            if (_back != null) _back.onClick.AddListener(Close);
            if (_reset != null) _reset.onClick.AddListener(ResetKeys);
            Panel.Hidden.AddListener(CancelRebinding);
        }

        public void Open()
        {
            if (!Services.TryGet(out InputService input))
            {
                Debug.LogError("[ArchNet] The Controls screen needs the input service (ArchNet > Hub > Services > Inputs).", this);
                return;
            }
            UISupport.EnsureEventSystem();
            Build(input);
            Panel.Show();
        }

        public void Close() => Panel.Hide();

        void Build(InputService input)
        {
            foreach (ControlRow row in _rows) Destroy(row.gameObject);
            _rows.Clear();
            InputActionMap map = input.Actions.FindActionMap(_map);
            if (map == null || _rowTemplate == null)
            {
                Debug.LogError($"[ArchNet] Controls screen: no action map \"{_map}\" or no row template.", this);
                return;
            }

            foreach (InputAction action in map.actions)
            {
                // One line per action, or per direction for movements (Up, Down…). First binding of each kind wins.
                var lines = new List<(string label, int keyboard, int gamepad)>();
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    InputBinding binding = action.bindings[i];
                    if (binding.isComposite) continue;
                    if (!binding.isPartOfComposite && action.expectedControlType == "Vector2") continue; // sticks and mouse motion: nothing to press
                    string label = binding.isPartOfComposite ? $"{action.name} {binding.name}" : action.name;
                    int line = lines.FindIndex(l => l.label == label);
                    if (line < 0)
                    {
                        lines.Add((label, -1, -1));
                        line = lines.Count - 1;
                    }
                    (string label, int keyboard, int gamepad) entry = lines[line];
                    if (entry.keyboard < 0 && Has(binding, KeyboardGroup)) entry.keyboard = i;
                    else if (entry.gamepad < 0 && Has(binding, GamepadGroup)) entry.gamepad = i;
                    lines[line] = entry;
                }
                foreach ((string label, int keyboard, int gamepad) in lines)
                {
                    ControlRow row = Instantiate(_rowTemplate, _list);
                    row.gameObject.SetActive(true);
                    row.Set(this, label, action, keyboard, gamepad);
                    _rows.Add(row);
                }
            }
        }

        static bool Has(InputBinding binding, string group) =>
            binding.groups != null && binding.groups.Split(InputBinding.Separator).Contains(group);

        /// <summary>Waits for the new key of one binding; Escape cancels.</summary>
        public void Rebind(InputAction action, int bindingIndex, string devices, ControlRow row)
        {
            if (bindingIndex < 0 || !Services.TryGet(out InputService input)) return;
            CancelRebinding();
            input.IsRebinding = true;
            action.Disable();
            row.ShowWaiting(bindingIndex);
            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath(devices)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => Finish(action, input, save: true))
                .OnCancel(_ => Finish(action, input, save: false))
                .Start();
        }

        void Finish(InputAction action, InputService input, bool save)
        {
            _operation?.Dispose();
            _operation = null;
            action.Enable();
            if (save) input.SaveBindings();
            foreach (ControlRow row in _rows) row.Refresh();
            StartCoroutine(EndRebindingNextFrame(input)); // the Escape that cancelled must not also close the screen
        }

        static IEnumerator EndRebindingNextFrame(InputService input)
        {
            yield return null;
            input.IsRebinding = false;
        }

        void CancelRebinding() => _operation?.Cancel();

        void ResetKeys()
        {
            if (!Services.TryGet(out InputService input)) return;
            CancelRebinding();
            input.ResetBindings();
            foreach (ControlRow row in _rows) row.Refresh();
        }
    }
}
