using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using Terrain;

public class GameMenu : MonoBehaviour
{
    [Header("Settings Config")]
    [SerializeField] private Terrain.TerrainSettings settings;
    [SerializeField] private WorldManagerSettings worldManagerSettings;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    
    private GUIStyle _windowStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _textFieldStyle;
    private bool _stylesInitialized = false;
    private Texture2D _whiteTexture;

    private bool _isVisible = true;
    private Vector2 _scrollPosition;
    private Rect _windowRect = new(20, 20, 350, 600);

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            _isVisible = !_isVisible;
    }
    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _whiteTexture = MakeTex(1, 1, Color.white);

        // Window
        _windowStyle = new GUIStyle(GUI.skin.window);
        _windowStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));
        _windowStyle.onNormal.background = _windowStyle.normal.background;
        _windowStyle.normal.textColor = Color.white;
        _windowStyle.fontSize = 14;
        _windowStyle.padding = new RectOffset(15, 15, 30, 15);

        // Buttons
        _buttonStyle = new GUIStyle(GUI.skin.button);
        _buttonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));
        _buttonStyle.hover.background = MakeTex(2, 2, new Color(0.35f, 0.65f, 0.85f, 1f));
        _buttonStyle.active.background = MakeTex(2, 2, new Color(0.35f, 0.65f, 0.85f, 0.8f));
        _buttonStyle.normal.textColor = Color.white;
        _buttonStyle.fontSize = 13;
        _buttonStyle.margin = new RectOffset(0, 0, 5, 5);
        _buttonStyle.fixedHeight = 30;

        // Text
        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        _labelStyle.fontSize = 13;
        _labelStyle.alignment = TextAnchor.MiddleLeft;

        // Headers
        _headerStyle = new GUIStyle(GUI.skin.label);
        _headerStyle.normal.textColor = new Color(0.35f, 0.65f, 0.85f);
        _headerStyle.fontSize = 16;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.margin = new RectOffset(0, 0, 15, 5);
        _headerStyle.alignment = TextAnchor.MiddleCenter;

        // Input fields
        _textFieldStyle = new GUIStyle(GUI.skin.textField);
        _textFieldStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f));
        _textFieldStyle.normal.textColor = Color.white;
        _textFieldStyle.alignment = TextAnchor.MiddleCenter;
        _textFieldStyle.fixedHeight = 25;

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        InitStyles();

        _windowRect = GUILayout.Window(0, _windowRect, DrawWindowContent, "", _windowStyle);
    }

    private void DrawWindowContent(int windowID)
    {
        GUILayout.Label("CONTROL PANEL", _headerStyle);
        DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.3f));
        
        GUILayout.Space(15);

        SettingsTab();
        
        GUI.DragWindow();
    }

    private void SettingsTab()
    {
        GUILayout.Label("GENERATION SETTINGS", _headerStyle);
        
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false);
        
        if (settings != null)
        {
            DrawAutoSettings(settings);
        }

        GUILayout.Space(15);

        GUILayout.Label("VIEW DISTANCE SETTINGS", _headerStyle);

        if (worldManagerSettings != null)
            DrawAutoSettings(worldManagerSettings);
        
        GUILayout.Space(15);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("REGENERATE", _buttonStyle))
        {
            var worldManager = FindFirstObjectByType<Terrain.WorldManager>();
            if (worldManager != null)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndScrollView();
    }

    private void DrawAutoSettings(object target)
    {
        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.Name == "WorldSeed") continue; 

            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.Label(FormatName(field.Name), _labelStyle, GUILayout.Width(180));

            DrawField(field, target);

            GUILayout.EndHorizontal();
        }
    }

    private void DrawField(FieldInfo field, object target)
    {
        object value = field.GetValue(target);
        object newValue = null;

        // Float
        if (field.FieldType == typeof(float))
        {
            float val = (float)value;

            var rangeAttribute = field.GetCustomAttribute<RangeAttribute>();

            float min = 0f;
            float max = 100f;

            if (rangeAttribute != null)
            {
                min = rangeAttribute.min;
                max = rangeAttribute.max;
            }

            val = GUILayout.HorizontalSlider(val, min, max, GUILayout.Height(20));
            
            string strVal = GUILayout.TextField(val.ToString("F3"), _textFieldStyle, GUILayout.Width(60));
            
            if (float.TryParse(strVal, out float parsed)) 
                val = Mathf.Clamp(parsed, min, max); 
            
            newValue = val;

        }
        // Int
        else if (field.FieldType == typeof(int))
        {
            int val = (int)value;
            string strVal = GUILayout.TextField(val.ToString(), _textFieldStyle, GUILayout.Width(60));
            if (int.TryParse(strVal, out int parsed)) val = parsed;
            newValue = val;
        }
        // Bool
        else if (field.FieldType == typeof(bool))
        {
            newValue = GUILayout.Toggle((bool)value, "");
        }
        // Enum
        else if (field.FieldType.IsEnum)
        {
            GUILayout.Label(value.ToString());
        }

        if (newValue != null && !newValue.Equals(value))
        {
            field.SetValue(target, newValue);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty((UnityEngine.Object)target);
#endif
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color32[] pix = new Color32[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new(width, height);
        result.SetPixels32(pix);
        result.Apply();
        return result;
    }

    private void DrawLine(Color color)
    {
        var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        rect.height = 1;

        if (_whiteTexture == null) 
            _whiteTexture = MakeTex(1, 1, Color.white);

        var savedColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _whiteTexture);
        GUI.color = savedColor;
    }
    
    private string FormatName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");
    }

    private void LoadScene(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
            SceneManager.LoadScene(sceneName);
    }
}