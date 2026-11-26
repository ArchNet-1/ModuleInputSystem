using System;
using ArchNet.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArchNet.Module.Tests
{
    public class InputServiceTests
    {
        GameObject _object;
        InputService _input;
        InputActionAsset _actions;
        string _file;

        [SetUp]
        public void SetUp()
        {
            _file = $"archnet-tests/bindings-{Guid.NewGuid():N}.json";
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            _actions.AddActionMap("Gameplay").AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _object = new GameObject("Inputs");
            _input = _object.AddComponent<InputService>();
            _input.Configure(_actions, _file);
        }

        [TearDown]
        public void TearDown()
        {
            JsonStorage.Delete(_file);
            UnityEngine.Object.DestroyImmediate(_object);
            UnityEngine.Object.DestroyImmediate(_actions);
        }

        [Test]
        public void ChangedKeysComeBackOnTheNextLaunch()
        {
            InputAction jump = _input.Action("Gameplay/Jump");
            jump.ApplyBindingOverride(0, "<Keyboard>/j");
            _input.SaveBindings();

            jump.RemoveAllBindingOverrides();
            Assert.AreEqual("<Keyboard>/space", jump.bindings[0].effectivePath);

            _input.LoadBindings();
            Assert.AreEqual("<Keyboard>/j", jump.bindings[0].effectivePath);
        }

        [Test]
        public void ResetGoesBackToTheAssetKeys()
        {
            InputAction jump = _input.Action("Jump");
            jump.ApplyBindingOverride(0, "<Keyboard>/j");
            _input.ResetBindings();

            Assert.AreEqual("<Keyboard>/space", jump.bindings[0].effectivePath);
        }

        [Test]
        public void AnUnknownActionSaysSo() =>
            Assert.Throws<ArgumentException>(() => _input.Action("Gameplay/Fly"));
    }
}
