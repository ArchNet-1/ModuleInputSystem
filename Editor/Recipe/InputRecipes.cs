// ArchNet · Input Module — recipes (ArchNet Hub > Services)
//   Inputs   → Assets/ArchNet/Inputs/: GameControls.inputactions (Gameplay: Move, Look, Jump,
//              Interact, Sprint; UI: Back — keyboard & mouse and gamepad), GameInput.cs and
//              Inputs.prefab under the ArchNet Bootstrap.
//   Controls → Assets/ArchNet/Controls/Controls.prefab (change keys screen) under the Bootstrap.
// This assembly only compiles when the ArchNet Hub (com.archnet.hub 2.0.0+) is installed.
// Docs: https://archnet-plateform.fr/#ModuleInputSystem

using System;
using ArchNet.Core.Editor;
using ArchNet.Installer.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArchNet.Module.Editor
{
    public sealed class InputsRecipe : ServiceRecipe
    {
        public const string RecipeId = "inputs";
        const string Template = "Packages/com.archnet.module.inputsystem/Editor/Recipe/Templates/GameInput.cs.txt";
        const string ServiceScript = "Scripts/GameInput.cs";
        const string ActionsFile = "GameControls.inputactions";
        const string PrefabFile = "Prefabs/Inputs.prefab";
        const string Keyboard = "Keyboard&Mouse";
        const string Gamepad = "Gamepad";

        public override string Id => RecipeId;
        public override string DisplayName => "Inputs";
        public override string Description => "Keyboard, mouse and gamepad controls on the Input System (move, look, jump, interact, sprint) and the Escape / Back key that closes menus and pauses. Services.Get<GameInput>().Move.";
        public override string Category => "Input";
        public override string FolderName => "Inputs";
        public override string[] RequiredServices => new[] { BootstrapRecipe.RecipeId };
        public override bool HasTool => true;

        public static string ActionsPath => $"{RecipeRunner.RootFolder}/Inputs/{ActionsFile}";

        public override void Create(RecipeContext context)
        {
            context.WriteText(ActionsFile, DefaultControls());
            context.WriteTemplate(ServiceScript, RecipeContext.LoadTemplate(Template));
        }

        public override void AfterScriptsCompiled(RecipeContext context)
        {
            Type serviceType = RecipeTools.GeneratedType<InputService>(context, "GameInput", ServiceScript);
            AssetDatabase.ImportAsset(context.PathOf(ActionsFile), ImportAssetOptions.ForceSynchronousImport);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(context.PathOf(ActionsFile))
                ?? throw new InvalidOperationException($"{context.PathOf(ActionsFile)} could not be read as Input Actions.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(context.PathOf(PrefabFile));
            if (prefab == null)
            {
                var root = new GameObject("Inputs", serviceType);
                RecipeTools.Assign(root.GetComponent(serviceType), "_actions", actions);
                prefab = context.SavePrefab(PrefabFile, root);
            }
            BootstrapRecipe.AddUnderBootstrap(prefab);
        }

        public override void OpenTool() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(ActionsPath));

        // Built with the Input System's own API, then written as the JSON of a .inputactions file.
        static string DefaultControls()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                asset.name = "GameControls";
                asset.AddControlScheme(Keyboard).WithBindingGroup(Keyboard).WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");
                asset.AddControlScheme(Gamepad).WithBindingGroup(Gamepad).WithRequiredDevice("<Gamepad>");

                InputActionMap gameplay = asset.AddActionMap("Gameplay");
                InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w", Keyboard).With("Down", "<Keyboard>/s", Keyboard)
                    .With("Left", "<Keyboard>/a", Keyboard).With("Right", "<Keyboard>/d", Keyboard);
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow", Keyboard).With("Down", "<Keyboard>/downArrow", Keyboard)
                    .With("Left", "<Keyboard>/leftArrow", Keyboard).With("Right", "<Keyboard>/rightArrow", Keyboard);
                move.AddBinding("<Gamepad>/leftStick", groups: Gamepad);

                InputAction look = gameplay.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
                look.AddBinding("<Mouse>/delta", groups: Keyboard);
                look.AddBinding("<Gamepad>/rightStick", groups: Gamepad);

                Button(gameplay, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
                Button(gameplay, "Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
                Button(gameplay, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");

                InputActionMap ui = asset.AddActionMap("UI");
                InputAction back = Button(ui, "Back", "<Keyboard>/escape", "<Gamepad>/start");
                back.AddBinding("<Gamepad>/buttonEast", groups: Gamepad);

                return asset.ToJson();
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        static InputAction Button(InputActionMap map, string name, string key, string padButton)
        {
            InputAction action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(key, groups: Keyboard);
            action.AddBinding(padButton, groups: Gamepad);
            return action;
        }
    }

    public sealed class ControlsRecipe : ServiceRecipe
    {
        public const string RecipeId = "controls";
        const string PrefabFile = "Prefabs/Controls.prefab";
        const int SortingOrder = 810; // just above the settings menu it is often opened from

        public override string Id => RecipeId;
        public override string DisplayName => "Controls";
        public override string Description => "A screen where players change their keyboard and gamepad keys, saved for the next launch. Services.Get<ControlsScreen>().Open().";
        public override string Category => "Input";
        public override string FolderName => "Controls";
        public override string[] RequiredServices => new[] { BootstrapRecipe.RecipeId, InputsRecipe.RecipeId };
        public override bool HasTool => true;

        public override void Create(RecipeContext context)
        {
            UIBuilder.EnsureTextMeshPro();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(context.PathOf(PrefabFile))
                                ?? context.SavePrefab(PrefabFile, BuildScreen());
            BootstrapRecipe.AddUnderBootstrap(prefab);
        }

        public override void OpenTool() =>
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>($"{RecipeRunner.RootFolder}/Controls/{PrefabFile}"));

        static GameObject BuildScreen()
        {
            GameObject root = UIBuilder.ScreenCanvas("Controls", SortingOrder);
            UIBuilder.Stretch(UIBuilder.Image("Shade", root.transform, UIBuilder.Shade).rectTransform);
            Image window = UIBuilder.Image("Window", root.transform, UIBuilder.Window);
            UIBuilder.Anchor(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1240f, 900f), Vector2.zero);
            UIBuilder.Column(window.rectTransform, 14, 40);

            UIBuilder.Text("Title", window.transform, "Controls", 56, style: FontStyles.Bold).gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            RectTransform header = Line("Header", window.transform, out _, out _, out TextMeshProUGUI keyboardHeader, out TextMeshProUGUI gamepadHeader, buttons: false);
            header.Find("Label").GetComponent<TMP_Text>().text = "";
            keyboardHeader.text = "Keyboard / mouse";
            gamepadHeader.text = "Gamepad";

            RectTransform list = UIBuilder.Rect("List", window.transform);
            UIBuilder.Column(list, 10);
            list.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            RectTransform template = Line("Row Template", list, out Button keyboard, out Button gamepad, out TextMeshProUGUI keyboardText, out TextMeshProUGUI gamepadText, buttons: true);
            var row = template.gameObject.AddComponent<ControlRow>();
            RecipeTools.Assign(row, "_label", template.Find("Label").GetComponent<TMP_Text>());
            RecipeTools.Assign(row, "_keyboard", keyboard);
            RecipeTools.Assign(row, "_keyboardText", keyboardText);
            RecipeTools.Assign(row, "_gamepad", gamepad);
            RecipeTools.Assign(row, "_gamepadText", gamepadText);

            RectTransform footer = UIBuilder.Rect("Footer", window.transform);
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 20;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = footerLayout.childForceExpandHeight = false;
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
            Button reset = UIBuilder.Button("Reset", footer, "Reset keys", UIBuilder.Neutral, out _);
            Button back = UIBuilder.Button("Back", footer, "Back", UIBuilder.Accent, out _);

            var screen = root.AddComponent<ControlsScreen>();
            RecipeTools.Assign(screen, "_list", list);
            RecipeTools.Assign(screen, "_rowTemplate", row);
            RecipeTools.Assign(screen, "_back", back);
            RecipeTools.Assign(screen, "_reset", reset);
            return root;
        }

        // A line: the action name, then the keyboard and the gamepad columns (buttons or headers).
        static RectTransform Line(string name, Transform parent, out Button keyboard, out Button gamepad,
            out TextMeshProUGUI keyboardText, out TextMeshProUGUI gamepadText, bool buttons)
        {
            RectTransform line = UIBuilder.Rect(name, parent);
            var layout = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = buttons ? 64 : 44;
            TextMeshProUGUI label = UIBuilder.Text("Label", line, "Action", 34, TextAlignmentOptions.Left);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;
            if (buttons)
            {
                keyboard = Column(line, out keyboardText);
                gamepad = Column(line, out gamepadText);
            }
            else
            {
                keyboard = gamepad = null;
                keyboardText = Header(line);
                gamepadText = Header(line);
            }
            return line;
        }

        static Button Column(Transform line, out TextMeshProUGUI text)
        {
            Button button = UIBuilder.Button("Key", line, "Key", UIBuilder.Neutral, out text);
            text.fontSize = 30;
            text.fontStyle = FontStyles.Normal;
            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 400;
            layout.preferredHeight = 64;
            return button;
        }

        static TextMeshProUGUI Header(Transform line)
        {
            TextMeshProUGUI text = UIBuilder.Text("Header", line, "", 28, style: FontStyles.Bold);
            text.color = UIBuilder.MutedText;
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = 400;
            return text;
        }
    }
}
