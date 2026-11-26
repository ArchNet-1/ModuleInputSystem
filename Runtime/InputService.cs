// ArchNet · Input Module — input service
// Turns on the game's Input Actions, keeps the keys the player changed (saved in
// persistentDataPath/input-bindings.json) and handles the Back action (Escape, gamepad Start
// or B / circle, Android back): it closes the last ArchNet screen opened (popup, settings,
// pause menu…); when none is open it raises BackPressed (the gameplay scene pauses on it).
// The Hub creates GameInput (your class) with one property per action:
//
//   Vector2 move = Services.Get<GameInput>().Move.ReadValue<Vector2>();
//   if (Services.Get<GameInput>().Jump.WasPressedThisFrame()) …
//
// Docs: https://archnet-plateform.fr/#ModuleInputSystem

using System;
using System.Runtime.CompilerServices;
using ArchNet.Core;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: InternalsVisibleTo("ArchNet.Module.InputSystem.Tests")]

namespace ArchNet.Module
{
    public class InputService : GameService
    {
        [Serializable]
        class BindingsFile
        {
            public string overrides;
        }

        [SerializeField] InputActionAsset _actions;
        [Tooltip("Action that closes screens, then raises BackPressed (\"Map/Action\").")]
        [SerializeField] string _backAction = "UI/Back";
        [Tooltip("File under Application.persistentDataPath.")]
        [SerializeField] string _bindingsFile = "input-bindings.json";

        InputAction _back;

        public InputActionAsset Actions => _actions;

        /// <summary>True while the Controls screen waits for a key; Back is then ignored.</summary>
        public bool IsRebinding { get; internal set; }

        /// <summary>Back was pressed while no ArchNet screen was open.</summary>
        public event Action BackPressed;

        internal void Configure(InputActionAsset actions, string bindingsFile)
        {
            _actions = actions;
            _bindingsFile = bindingsFile;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_actions == null)
            {
                Debug.LogError("[ArchNet] The input service has no Input Actions asset assigned.", this);
                return;
            }
            LoadBindings();
            _actions.Enable();
            _back = _actions.FindAction(_backAction);
            if (_back != null) _back.performed += OnBack;
        }

        protected override void OnDestroy()
        {
            if (_back != null) _back.performed -= OnBack;
            if (_actions != null) _actions.Disable();
            base.OnDestroy();
        }

        /// <summary>An action by "Map/Action" (or a unique action name). Throws when it does not exist.</summary>
        public InputAction Action(string path) => _actions.FindAction(path, true);

        public void SaveBindings() =>
            JsonStorage.Save(_bindingsFile, new BindingsFile { overrides = _actions.SaveBindingOverridesAsJson() });

        public void LoadBindings()
        {
            if (JsonStorage.TryLoad(_bindingsFile, out BindingsFile file) && !string.IsNullOrEmpty(file.overrides))
                _actions.LoadBindingOverridesFromJson(file.overrides);
        }

        /// <summary>Back to the keys of the Input Actions asset.</summary>
        public void ResetBindings()
        {
            _actions.RemoveAllBindingOverrides();
            SaveBindings();
        }

        void OnBack(InputAction.CallbackContext _)
        {
            if (IsRebinding) return;
            if (!UIPanel.CloseTop()) BackPressed?.Invoke();
        }
    }
}
