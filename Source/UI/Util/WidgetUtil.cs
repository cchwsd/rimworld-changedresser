using ChangeDresser.UI.DTO;
using ChangeDresser.UI.DTO.SelectionWidgetDTOs;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Text.RegularExpressions;
using System.IO;

namespace ChangeDresser.UI.Util
{
    delegate void SelectionChangeListener(object sender);
    delegate void UpdatePawnListener(object sender, object value);
    delegate void ClearColorLayers();

    [StaticConstructorOnStartup]
    static class WidgetUtil
    {
        public static Texture2D nextTexture;
        public static Texture2D previousTexture;
        public static Texture2D cantTexture;
        public static Texture2D colorPickerTexture;
        public static Texture2D copyIconTexture;
        public static Texture2D pasteIconTexture;
        public static Texture2D dropTexture;
        public static Texture2D colorFinder;
        public static Texture2D noneTexture;
        public static Texture2D yesSellTexture;
        public static Texture2D noSellTexture;
        public static Texture2D yesDressFromTexture;
        public static Texture2D noDressFromTexture;

        public static Texture2D manageapparelTexture;
        public static Texture2D assignweaponsTexture;
        public static Texture2D customapparelTexture;
        public static Texture2D emptyTexture;
        public static Texture2D collectTexture;

        static WidgetUtil()
        {
            nextTexture = ContentFinder<Texture2D>.Get("UI/next", true);
            previousTexture = ContentFinder<Texture2D>.Get("UI/previous", true);
            cantTexture = ContentFinder<Texture2D>.Get("UI/x", true);
            colorPickerTexture = ContentFinder<Texture2D>.Get("UI/colorpicker", true);
            copyIconTexture = ContentFinder<Texture2D>.Get("UI/copy", true);
            pasteIconTexture = ContentFinder<Texture2D>.Get("UI/paste", true);
            dropTexture = ContentFinder<Texture2D>.Get("UI/drop", true);
            noneTexture = ContentFinder<Texture2D>.Get("UI/none", true);
            yesSellTexture = ContentFinder<Texture2D>.Get("UI/yessell", true);
            noSellTexture = ContentFinder<Texture2D>.Get("UI/nosell", true);
            yesDressFromTexture = ContentFinder<Texture2D>.Get("UI/dressfrom", true);
            noDressFromTexture = ContentFinder<Texture2D>.Get("UI/nodressfrom", true);

            manageapparelTexture = ContentFinder<Texture2D>.Get("UI/manageapparel", true);
            assignweaponsTexture = ContentFinder<Texture2D>.Get("UI/assignweapons", true);
            customapparelTexture = ContentFinder<Texture2D>.Get("UI/customapparel", true);
            emptyTexture = ContentFinder<Texture2D>.Get("UI/empty", true);
            collectTexture = ContentFinder<Texture2D>.Get("UI/collect", true);

            foreach (ModContentPack current in LoadedModManager.RunningMods)
            {
                if (current.GetContentHolder<Texture2D>().Get("UI/colorpicker"))
                {
                    byte[] data = File.ReadAllBytes(current.RootDir + "/Textures/UI/colorpicker.png");
                    colorFinder = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                    colorFinder.LoadImage(data, false);
                    break;
                }
            }
        }

        public static readonly Vector2 NavButtonSize = new Vector2(30f, 30f);
        public static readonly Vector2 ButtonSize = new Vector2(150f, 30f);
        public static readonly Vector2 PortraitSize = new Vector2(192f, 192f);
        private static Vector2 scrollPos = new Vector2(0, 0);
        private static Vector2 hairScrollPos = new Vector2(0, 0);
        private static readonly Texture2D ColorPreset = new Texture2D(20, 20);
        private static readonly Texture2D LayerColor = new Texture2D(50, 30);

        public static float SelectionRowHeight { get { return NavButtonSize.y; } }

        private static GUIStyle middleCenterGuiStyle = null;

        public static GUIStyle MiddleCenter
        {
            get
            {
                if (middleCenterGuiStyle == null)
                {
                    middleCenterGuiStyle = GUI.skin.label;
                    middleCenterGuiStyle.alignment = TextAnchor.MiddleCenter;
                }
                return middleCenterGuiStyle;
            }
        }

        public static string AddNumberTextInput(float labelLeft, float top, float inputLeft, float inputWidth, string label, string value)
        {
            GUI.color = Color.white;
            GUI.BeginGroup(new Rect(labelLeft, top, inputLeft + inputWidth, SelectionRowHeight));
            GUI.Label(new Rect(0, 0, inputLeft - 5, SelectionRowHeight), label, MiddleCenter);
            Rect inputRect = new Rect(inputLeft, 0, inputWidth, SelectionRowHeight);
            string result = GUI.TextField(inputRect, value, MiddleCenter).Trim();
            GUI.color = Color.grey;
            Widgets.DrawBox(inputRect, 1);
            GUI.EndGroup();

            if (result.Length > 0 && !Regex.IsMatch(result, "^[0-9]*$"))
            {
                result = value;
            }
            return result;
        }

        public static void AddSelectorWidget(float left, float top, float width, string label, ASelectionWidgetDTO selectionWidgetDto)
        {
            const float buffer = 5f;

            Rect rect = new Rect(left, top, width, SelectionRowHeight);
            GUI.BeginGroup(rect);
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            left = 0;
            if (label != null)
            {
                //Text.Anchor = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(0, 0, 75, SelectionRowHeight), label, MiddleCenter);
                left = 80;
            }
            Text.Anchor = TextAnchor.MiddleCenter;

            Rect previousButtonRect = new Rect(left, 0, NavButtonSize.x, NavButtonSize.y);
            if (GUI.Button(previousButtonRect, previousTexture))
            {
                selectionWidgetDto.DecreaseIndex();
            }

            Rect labelRect = new Rect(NavButtonSize.x + buffer + left, 0, rect.width - (2 * NavButtonSize.x) - (2 * buffer) - left, NavButtonSize.y);
            GUI.Label(labelRect, selectionWidgetDto.SelectedItemLabel, MiddleCenter);

            GUI.color = Color.grey;
            Widgets.DrawBox(labelRect, 1);

            Rect nextButtonRect = new Rect(rect.width - NavButtonSize.x, 0, NavButtonSize.x, NavButtonSize.y);
            if (GUI.Button(nextButtonRect, nextTexture))
            {
                selectionWidgetDto.IncreaseIndex();
            }
            GUI.EndGroup();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        
        private static Color GetColorFromTexture(Vector2 mousePosition, Rect rect, Texture2D texture)
        {
            float localMouseX = mousePosition.x - rect.x;
            float localMouseY = mousePosition.y - rect.y;
            int imageX = (int)(localMouseX * ((float)colorPickerTexture.width / (rect.width + 0f)));
            int imageY = (int)((rect.height - localMouseY) * ((float)colorPickerTexture.height / (rect.height + 0f)));
            Color pixel = texture.GetPixel(imageX, imageY);
            return pixel;
        }

        private static string ColorConvert(float f)
        {
            try
            {
                int i = (int)(f * 255);
                if (i > 255)
                {
                    i = 255;
                }
                else if (i < 0)
                {
                    i = 0;
                }
                return i.ToString();
            }
            catch
            {
                return "0";
            }
        }

        private static float ColorConvert(string intText)
        {
            try
            {
                float f = int.Parse(intText) / 255f;
                if (f > 1)
                {
                    f = 1;
                }
                else if (f < 0)
                {
                    f = 0;
                }
                return f;
            }
            catch
            {
                return 0;
            }

        }
    }
}