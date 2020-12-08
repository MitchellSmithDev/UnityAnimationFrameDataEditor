using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Characters;

public class CharacterAnimationEditor : EditorWindow
{
    static CharacterAnimationEditor window;

    static int currentFrame;
    static Sprite currentSprite => animation != null && currentFrame < animation.frames.Count ? animation.frames[currentFrame].sprite : null;
    static float spriteScale = 1f;
    const float defaultSpriteScale = 2f;
    const float spriteScaleSpeed = 0.1f;
    static Vector2 spritePosition = Vector2.zero;
    static float defaultPixelsPerUnit = 64f;

    static float pixelsPerUnit => currentSprite != null ? currentSprite.pixelsPerUnit : defaultPixelsPerUnit;
    
    static Texture2D backgroundTexture;
    const int backgroundWidth = 100;
    const int backgroundHeight = 100;

    static Rect spriteArea => new Rect(320, 185, window.position.width - 320, window.position.height - 185);

    static CharacterAnimation animation;

    bool isPlaying;

    Vector2 scrollPosition;

    static float previousTime;
    static float deltaTime;
    const float framesPerSecond = 1f / 60f;

    float deltaCount;
    int frameCount;
    float playbackSpeed = 1f;

    static HashSet<int> selectedFrames = new HashSet<int>();
    int selectedDuration = 3;

    static SortedDictionary<int, CharacterFrame> clipboardFrames = new SortedDictionary<int, CharacterFrame>();

    Event currentEvent;

    [MenuItem("Window/Character Animation Editor")]
    static void Init()
    {
        if(window == null)
            SetNewAnimation();
        
        SetWindow();

        selectedFrames.Clear();
        clipboardFrames.Clear();
    }

    static void SetWindow()
    {
        window = (CharacterAnimationEditor)EditorWindow.GetWindow(typeof(CharacterAnimationEditor));
        window.Show();
        window.minSize = new Vector2(900, 600);

        MakeBackground(backgroundWidth, backgroundHeight);

        ResetSprite();

        previousTime = (float)EditorApplication.timeSinceStartup;
        deltaTime = 0;
    }

    static void SetNewAnimation()
    {
        animation = ScriptableObject.CreateInstance<CharacterAnimation>();
        AddFrame();
        currentFrame = 0;
    }

    void Update()
    {
        deltaTime = (float)EditorApplication.timeSinceStartup - previousTime;

        if(isPlaying && animation != null)
        {
            deltaCount += deltaTime;
            frameCount += (int)(deltaCount / (framesPerSecond / playbackSpeed));
            deltaCount = deltaCount % (framesPerSecond / playbackSpeed);

            if(frameCount > animation.frames[currentFrame].duration)
            {
                frameCount = 0;
                currentFrame++;
                if(currentFrame >= animation.frames.Count)
                {
                    if(animation.loop)
                    {
                        currentFrame = 0;
                    } else
                    {
                        currentFrame = animation.frames.Count - 1;
                        isPlaying = false;
                    }
                }
            }
        } else
        {
            deltaCount = 0;
            frameCount = 0;
            isPlaying = false;
        }

        Repaint();

        previousTime = (float)EditorApplication.timeSinceStartup;
    }

    void OnGUI()
    {
        if(window == null)
            SetWindow();
        
        currentEvent = Event.current;

        if(animation == null)
            SetNewAnimation();
        
        if(animation.frames.Count == 0)
        {
            AddFrame();
            selectedFrames.Clear();
        }
        
        currentFrame = Mathf.Min(currentFrame, animation.frames.Count - 1);

        defaultPixelsPerUnit = pixelsPerUnit;

        if(spriteArea.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.ScrollWheel)
        {
            float oldScale = spriteScale;
            spriteScale = Mathf.Max(spriteScale - currentEvent.delta.y * spriteScaleSpeed, 0);
            if(spriteScale > 0.5f)
                spritePosition = currentEvent.mousePosition + (spritePosition - currentEvent.mousePosition) * (spriteScale / oldScale);
        }

        if(currentEvent.type == EventType.MouseDrag && currentEvent.button == 2)
            spritePosition += currentEvent.delta;

        if(currentSprite != null)
        {
            DrawBackground();
            DrawSprite();
        }

        DrawMeshes();

        TopBar();

        SideBar();

        RightClickMenu();

        //GUI.DrawTexture(spriteArea, Texture2D.whiteTexture);
    }

    static void ResetSprite()
    {
        spriteScale = defaultSpriteScale;
        spritePosition = spriteArea.center;
    }

    static void MakeBackground(int width, int height)
    {
        backgroundTexture = new Texture2D(width, height);

        Color lightGray = new Color(0.75f, 0.75f, 0.75f, 1f);

        Color[] pixels = new Color[width * height];

        int i = 0;
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                if(x % 2 == 0)
                    if(y % 2 == 0)
                        pixels[i] = Color.gray;
                    else
                        pixels[i] = lightGray;
                else
                    if(y % 2 == 0)
                        pixels[i] = lightGray;
                    else
                        pixels[i] = Color.gray;
                i++;
            }
        }

        backgroundTexture.SetPixels(pixels, 0);
        backgroundTexture.filterMode = FilterMode.Point;
        backgroundTexture.Apply();
    }

    void DrawBackground()
    {
        if(backgroundTexture == null)
            MakeBackground(backgroundWidth, backgroundHeight);
        
        if(currentSprite != null)
        {
            Vector2 position = new Vector2(spritePosition.x - currentSprite.pivot.x * spriteScale,
                                            spritePosition.y - currentSprite.pivot.y * spriteScale);

            Vector2 backgroundSize = new Vector2(currentSprite.pixelsPerUnit * backgroundWidth,
                                                currentSprite.pixelsPerUnit * backgroundHeight);
            
            for(float x = 0; x < currentSprite.rect.width; x += backgroundSize.x)
            {
                for(float y = currentSprite.rect.height; y > 0; y -= backgroundSize.y)
                {
                    float verticalOverlap = Mathf.Min(y - backgroundSize.y, 0);
                    Rect backgroundRect = new Rect(Mathf.Floor(position.x + x * spriteScale),
                                                    Mathf.Floor(position.y + (y - backgroundSize.y - verticalOverlap) * spriteScale),
                                                    Mathf.Floor((Mathf.Min(x + backgroundSize.x, currentSprite.rect.width) - x) * spriteScale),
                                                    Mathf.Floor((backgroundSize.y + verticalOverlap) * spriteScale));
                    
                    Rect backgroundCoords = new Rect(0, 0, backgroundRect.width / (backgroundSize.x * spriteScale), backgroundRect.height / (backgroundSize.y * spriteScale));

                    GUI.DrawTextureWithTexCoords(backgroundRect, backgroundTexture, backgroundCoords);
                }
            }
        }
    }

    void DrawSprite()
    {
        Rect textureRect = new Rect(Mathf.Floor(spritePosition.x - currentSprite.pivot.x * spriteScale),
                                    Mathf.Floor(spritePosition.y - currentSprite.pivot.y * spriteScale),
                                    Mathf.Floor(currentSprite.rect.width * spriteScale), Mathf.Floor(currentSprite.rect.height * spriteScale));
        
        Rect textureCoords = new Rect(currentSprite.rect.x / currentSprite.texture.width, currentSprite.rect.y / currentSprite.texture.height,
                                    currentSprite.rect.width/ currentSprite.texture.width, currentSprite.rect.height / currentSprite.texture.height);
            
        GUI.DrawTextureWithTexCoords(textureRect, currentSprite.texture, textureCoords);
    }

    void TopBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(180), GUILayout.Width(window.position.width - 5));
        
        EditorGUILayout.BeginVertical(EditorStyles.label, GUILayout.Width(100));

        if(GUILayout.Button("New..."))
        {
            isPlaying = false;
            NewFile();
        }
        
        if(GUILayout.Button("Open..."))
        {
            isPlaying = false;
            OpenFile();
        }
        
        if(GUILayout.Button("Save") || currentEvent.modifiers == EventModifiers.Control && currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.S)
        {
            isPlaying = false;
            SaveFile();
        }
        
        if(GUILayout.Button("Save As..."))
        {
            isPlaying = false;
            SaveFileAs();
        }

        if(GUILayout.Button("Import"))
        {
            isPlaying = false;
            Import();
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(EditorStyles.label, GUILayout.Width(200));

        bool loop = EditorGUILayout.Toggle("Loop Animation", animation.loop);
        if(!isPlaying)
            animation.loop = loop;

        GUILayout.Label("Current Frame Information", EditorStyles.boldLabel);

        GUILayout.Label("Sprite");
        Sprite sprite = (Sprite)EditorGUILayout.ObjectField(animation.frames[currentFrame].sprite, typeof(Sprite), false);
        if(!isPlaying)
            animation.frames[currentFrame].sprite = sprite;

        GUILayout.Label("Pixels Per Unit : " + pixelsPerUnit);
        GUILayout.Label("Pixel Size : " + (currentSprite != null ? "(" + currentSprite.rect.width + ", " + currentSprite.rect.height + ")" : "N/A"));
        GUILayout.Label("Unit Size : " + (currentSprite != null ? "(" + currentSprite.rect.width / currentSprite.pixelsPerUnit + ", " +
                                                                        currentSprite.rect.height / currentSprite.pixelsPerUnit + ")" : "N/A"));

        GUILayout.Label("Duration in game frames [60 per second]");
        int duration = EditorGUILayout.IntField(animation.frames[currentFrame].duration);
        if(!isPlaying)
            animation.frames[currentFrame].duration = duration;

        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical();
        
        EditorGUIUtility.labelWidth = 100;
        
        GUILayout.Label("Timeline [Total Frames " + animation.frames.Count + "]", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();

        if(isPlaying)
            GUI.backgroundColor = Color.red;
        else
            GUI.backgroundColor = Color.green;

        if(GUILayout.Button(isPlaying ? "Stop" : "Play", GUILayout.Width(100)))
        {
            isPlaying = !isPlaying;
            GUI.FocusControl(null);
            selectedFrames.Clear();
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUIUtility.labelWidth = 100;
        playbackSpeed = Mathf.Max(EditorGUILayout.FloatField("Playback Speed", playbackSpeed, GUILayout.ExpandWidth(false)), 0);

        if(GUILayout.Button("Reset Position & Scale", GUILayout.ExpandWidth(false)))
            ResetSprite();
        
        EditorGUIUtility.labelWidth = 40;
        spriteScale = Mathf.Max(EditorGUILayout.FloatField("Scale", spriteScale, GUILayout.ExpandWidth(false)), 0);
        EditorGUIUtility.labelWidth = 100;
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Delete Frame(s)", GUILayout.Width(125)) && !isPlaying)
        {
            if(EditorUtility.DisplayDialog("DELETE all selected frames?", "Are you sure you want to DELETE all selected frames?\nThis cannot be undone.", "Yes", "No"))
            {
                List<CharacterFrame> deletedFrames = new List<CharacterFrame>();
                deletedFrames.Add(animation.frames[currentFrame]);

                foreach(int frame in selectedFrames)
                    if(frame != currentFrame)
                        deletedFrames.Add(animation.frames[frame]);
                selectedFrames.Clear();

                foreach(CharacterFrame frame in deletedFrames)
                    animation.frames.Remove(frame);
                
                if(animation.frames.Count == 0)
                    AddFrame();

                if(currentFrame >= animation.frames.Count)
                    currentFrame = animation.frames.Count - 1;
            }
        }

        if(GUILayout.Button("Add Frame", GUILayout.Width(125)) && !isPlaying)
            AddFrame();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Select All", GUILayout.Width(75)) && !isPlaying)
            for(int i = 0; i < animation.frames.Count; i++)
                selectedFrames.Add(i);

        string labelSelectedFrames = "Selected Frame(s) : " + currentFrame + ", ";
        foreach(int frame in selectedFrames)
            if(frame != currentFrame)
                labelSelectedFrames += frame + ", ";
        labelSelectedFrames = labelSelectedFrames.Substring(0, labelSelectedFrames.Length - 2);

        GUILayout.Label(labelSelectedFrames);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Copy", GUILayout.Width(75)) && !isPlaying)
        {
            clipboardFrames.Clear();
            clipboardFrames.Add(currentFrame, animation.frames[currentFrame].Clone());
            foreach(int frame in selectedFrames)
                if(frame != currentFrame)
                    clipboardFrames.Add(frame, animation.frames[frame].Clone());
        }

        if(GUILayout.Button("Cut", GUILayout.Width(75)) && !isPlaying)
        {
            clipboardFrames.Clear();
            clipboardFrames.Add(currentFrame, animation.frames[currentFrame].Clone());
            List<CharacterFrame> deletedFrames = new List<CharacterFrame>();
            deletedFrames.Add(animation.frames[currentFrame]);

            foreach(int frame in selectedFrames)
            {
                if(frame != currentFrame)
                {
                    deletedFrames.Add(animation.frames[frame]);
                    clipboardFrames.Add(frame, animation.frames[frame].Clone());
                }
            }
            selectedFrames.Clear();

            foreach(CharacterFrame frame in deletedFrames)
                animation.frames.Remove(frame);
            
            if(animation.frames.Count == 0)
                AddFrame();

            if(currentFrame >= animation.frames.Count)
                currentFrame = animation.frames.Count - 1;
        }

        if(GUILayout.Button("Paste", GUILayout.Width(75)) && !isPlaying)
        {
            int insertFrame = currentFrame;
            foreach(int frame in clipboardFrames.Keys)
            {
                animation.frames.Insert(insertFrame, clipboardFrames[frame].Clone());
                insertFrame++;
            }
        }

        string labelClipboardFrames = "Frame(s) in Clipboard : ";
        if(clipboardFrames.Keys.Count > 0)
        {
            foreach(int frame in clipboardFrames.Keys)
                labelClipboardFrames += frame + ", ";
            labelClipboardFrames = labelClipboardFrames.Substring(0, labelClipboardFrames.Length - 2);
        } else
        {
            labelClipboardFrames += "N/A";
        }

        GUILayout.Label(labelClipboardFrames);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Set Duration for Selected", GUILayout.Width(175)) && !isPlaying)
        {
            if(EditorUtility.DisplayDialog("Set DURATION for all selected frames?", "Are you sure you want to set DURATION for all selected frames to " + selectedDuration+ "?\nThis cannot be undone.", "Yes", "No"))
            {
                animation.frames[currentFrame].duration = selectedDuration;

                foreach(int frame in selectedFrames)
                    if(frame != currentFrame)
                        animation.frames[frame].duration = selectedDuration;
            }
        }
        
        selectedDuration = EditorGUILayout.IntField(selectedDuration, GUILayout.Width(50), GUILayout.ExpandWidth(false));

        EditorGUILayout.EndHorizontal();

        int frameSlider = EditorGUILayout.IntSlider("Current Frame", currentFrame, 0, animation.frames.Count - 1);
        if(!isPlaying)
            currentFrame = frameSlider;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.BeginHorizontal();

        int forceScroll = 0;
        for(int i = 0; i < animation.frames.Count; i++)
        {
            if(i == currentFrame)
            {
                if(isPlaying)
                {
                    scrollPosition.x = forceScroll;
                    GUI.backgroundColor = Color.green;
                } else
                {
                    GUI.backgroundColor = Color.red;
                }
            } else if(!isPlaying && selectedFrames.Contains(i))
            {
                GUI.backgroundColor = Color.yellow;
            }
                
            if(GUILayout.Button("" + i, GUILayout.Width(25 * Mathf.Max(animation.frames[i].duration, 1))) && !isPlaying)
            {
                if(currentEvent.modifiers == EventModifiers.Shift)
                {
                    if(selectedFrames.Contains(i))
                        selectedFrames.Remove(i);
                    else
                        selectedFrames.Add(i);
                } else
                {
                    currentFrame = i;
                    GUI.FocusControl(null);
                    selectedFrames.Clear();
                }
            }

            GUI.backgroundColor = Color.white;
            forceScroll += 26 * animation.frames[i].duration;
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    static void NewFile()
    {
        if(EditorUtility.DisplayDialog("Make a NEW animation?", "Are you sure you want to make a NEW animation?\nUnsaved progress will be lost.", "Yes", "No"))
            SetNewAnimation();
    }

    static void OpenFile()
    {
        if(EditorUtility.DisplayDialog("OPEN an animation?", "Are you sure you want to OPEN an animation?\nUnsaved progress will be lost.", "Yes", "No"))
        {
            string filePath = EditorUtility.OpenFilePanel("Open", "", "asset");
            if(filePath.Length != 0)
            {
                animation = (CharacterAnimation)AssetDatabase.LoadAssetAtPath(filePath.Replace(Application.dataPath, "Assets"), typeof(CharacterAnimation));
                if(animation == null)
                    animation = ScriptableObject.CreateInstance<CharacterAnimation>();
                if(animation.frames.Count == 0)
                    AddFrame();
                currentFrame = 0;
            }
        }
    }

    static void SaveFile()
    {
        if(!AssetDatabase.Contains(animation))
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Save", "CharacterAnimation", "asset", "Please enter a file name to save the animation to");
            if(filePath.Length != 0)
            {
                AssetDatabase.DeleteAsset(filePath);
                AssetDatabase.CreateAsset(animation, filePath);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void SaveFileAs()
    {
        string filePath = EditorUtility.SaveFilePanelInProject("Save As", "CharacterAnimation", "asset", "Please enter a file name to save the animation to");
        if(filePath.Length != 0)
        {
            animation = Instantiate(animation);
            AssetDatabase.DeleteAsset(filePath);
            AssetDatabase.CreateAsset(animation, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    static void Import()
    {
        if(EditorUtility.DisplayDialog("IMPORT an animation?", "Are you sure you want to IMPORT an animation?\nUnsaved progress will be lost.", "Yes", "No"))
        {
            string filePath = EditorUtility.OpenFilePanel("Open", "", "png");
            if(filePath.Length != 0)
            {
                UnityEngine.Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath.Replace(Application.dataPath, "Assets"));
                animation = ScriptableObject.CreateInstance<CharacterAnimation>();

                for(int i = 0; i < sprites.Length; i++)
                {
                    if(sprites[i] is Sprite)
                    {
                        AddFrame();
                        animation.frames[animation.frames.Count - 1].sprite = (Sprite)sprites[i];
                    }
                }

                if(animation.frames.Count == 0)
                    AddFrame();
                
                currentFrame = 0;
            }
        }
    }

    static void AddFrame()
    {
        animation.frames.Add(new CharacterFrame());
    }

    bool lockToPixels;

    void SideBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(315), GUILayout.Height(window.position.height - 185));

        EditorGUIUtility.labelWidth = 125;
        lockToPixels = EditorGUILayout.Toggle("Lock To Pixel Grid", lockToPixels);
        EditorGUIUtility.labelWidth = 100;

        previewCollider = EditorGUILayout.BeginToggleGroup("Preview Character Collider", previewCollider);
        
        float opacity = EditorGUILayout.Slider("Collider Opacity", colliderOpacity, 0, 1);
        colliderOpacity = Mathf.Clamp(opacity, 0, 1);
        
        Vector2 newSize = EditorGUILayout.Vector2Field("Collider Size", colliderSize);
        colliderSize = new Vector2(Mathf.Max(newSize.x, 0), Mathf.Max(newSize.y, 0));

        EditorGUILayout.EndToggleGroup();

        GUILayout.Label("Current Collider", EditorStyles.boldLabel);

        if(!isPlaying && spriteArea.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            Vector2 localMousePosition = ScreenToLocal(currentEvent.mousePosition);

            currentCollider = null;

            foreach(HurtTriggerData hurtTrigger in animation.frames[currentFrame].hurtTriggers)
                if(hurtTrigger.collider.Contains(localMousePosition))
                    currentCollider = hurtTrigger.collider;

            foreach(HitTriggerData hitTrigger in animation.frames[currentFrame].hitTriggers)
                if(hitTrigger.collider.Contains(localMousePosition))
                    currentCollider = hitTrigger.collider;
        } else if(currentCollider != null)
        {
            ColliderType currentColliderType = (ColliderType)EditorGUILayout.EnumPopup("Collider Type", currentCollider.type);
            if(!isPlaying)
                currentCollider.type = currentColliderType;

            Vector2 currentColliderPosition = EditorGUILayout.Vector2Field("Position", currentCollider.position);
            if(!isPlaying)
                currentCollider.position = currentColliderPosition;

            float currentColliderRotation = EditorGUILayout.FloatField("Rotation", currentCollider.rotation);
            if(!isPlaying)
                currentCollider.rotation = currentColliderRotation;
            
            Vector2 currentColliderSize = Vector2.zero;
            switch(currentCollider.type)
            {
                case ColliderType.Box :
                    currentColliderSize = EditorGUILayout.Vector2Field("Size", currentCollider.size);
                    if(!isPlaying)
                        currentCollider.size = currentColliderSize;
                    break;
                case ColliderType.Capsule :
                    currentColliderSize = EditorGUILayout.Vector2Field("Size", currentCollider.size);
                    if(!isPlaying)
                        currentCollider.size = currentColliderSize;

                    CapsuleDirection2D currentColliderDirection = (CapsuleDirection2D)EditorGUILayout.EnumPopup("Direction", currentCollider.direction);
                    if(!isPlaying)
                        currentCollider.direction = currentColliderDirection;
                    break;
                case ColliderType.Circle :
                    float currentColliderRadius = EditorGUILayout.FloatField("Radius", currentCollider.radius);
                    if(!isPlaying)
                        currentCollider.radius = currentColliderRadius;
                    break;
                case ColliderType.Polygon :
                    List<PolygonPath> currentColliderPaths = GUIPaths(currentCollider.paths);
                    if(!isPlaying)
                        currentCollider.paths = currentColliderPaths;
                    break;
            }
        }

        EditorGUILayout.EndVertical();
    }

    bool listFoldout = false;
    bool[] pathFoldout = new bool[0];
    Vector2 pathScroll;

    List<PolygonPath> GUIPaths(List<PolygonPath> paths)
    {
        listFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(listFoldout, "Polygon Paths");
        pathScroll = GUILayout.BeginScrollView(pathScroll);

        if(listFoldout)
        {
            int listSize = Mathf.Max(EditorGUILayout.DelayedIntField("Size", paths.Count), 1);
            if(listSize > paths.Count)
                for(int i = paths.Count; i < listSize; i++)
                    paths.Add(new PolygonPath());
            else if(listSize < paths.Count)
                for(int i = paths.Count; i > listSize; i--)
                    paths.RemoveAt(paths.Count - 1);
            
            if(pathFoldout.Length != paths.Count)
                Array.Resize(ref pathFoldout, paths.Count);
            
            for(int i = 0; i < paths.Count; i++)
            {
                pathFoldout[i] = EditorGUILayout.Foldout(pathFoldout[i], "Path " + i);
                if(pathFoldout[i])
                {
                    int pathSize = Mathf.Max(EditorGUILayout.DelayedIntField("Size", paths[i].Count), 3);
                    if(pathSize > paths[i].Count)
                        for(int j = paths[i].Count; j < pathSize; j++)
                            paths[i].Add(Vector2.zero);
                    else if(pathSize < paths[i].Count)
                        for(int j = paths[i].Count; j > pathSize; j--)
                            paths[i].RemoveAt(paths[i].Count - 1);
                    for(int j = 0; j < paths[i].Count; j++)
                        paths[i][j] = EditorGUILayout.Vector2Field("", paths[i][j]);
                }
            }
        }

        GUILayout.EndScrollView();
        EditorGUILayout.EndFoldoutHeaderGroup();

        return paths;
    }

    Vector2 rightClickMousePosition;

    void RightClickMenu()
    {
        Rect clickArea = new Rect(0, 0, window.position.width, window.position.height);
    
        if(!isPlaying && clickArea.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.ContextClick)
        {
            rightClickMousePosition = currentEvent.mousePosition;

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("New Hit Trigger/Box"), false, MenuAddHitTrigger, ColliderType.Box);
            menu.AddItem(new GUIContent("New Hit Trigger/Capsule"), false, MenuAddHitTrigger, ColliderType.Capsule);
            menu.AddItem(new GUIContent("New Hit Trigger/Circle"), false, MenuAddHitTrigger, ColliderType.Circle);
            menu.AddItem(new GUIContent("New Hit Trigger/Polygon"), false, MenuAddHitTrigger, ColliderType.Polygon);

            menu.AddItem(new GUIContent("New Hurt Trigger/Box"), false, MenuAddHurtTrigger, ColliderType.Box);
            menu.AddItem(new GUIContent("New Hurt Trigger/Capsule"), false, MenuAddHurtTrigger, ColliderType.Capsule);
            menu.AddItem(new GUIContent("New Hurt Trigger/Circle"), false, MenuAddHurtTrigger, ColliderType.Circle);
            menu.AddItem(new GUIContent("New Hurt Trigger/Polygon"), false, MenuAddHurtTrigger, ColliderType.Polygon);

            menu.ShowAsContext();
        
            currentEvent.Use(); 
        }
    }

    void MenuAddHitTrigger(object type)
    {
        Vector2 position = rightClickMousePosition;
        if(lockToPixels)
            position = PixelToLocal(LockPixel(ScreenToPixel(position)));
        else
            position = ScreenToLocal(position);
        AddHitTrigger((ColliderType)type, position);
    }

    void AddHitTrigger(ColliderType type, Vector2 position)
    {
        HitTriggerData hitTrigger = new HitTriggerData(type);
        hitTrigger.collider.position = position;
        animation.frames[currentFrame].hitTriggers.Add(hitTrigger);
    }

    void MenuAddHurtTrigger(object type)
    {
        Vector2 position = rightClickMousePosition;
        if(lockToPixels)
            position = PixelToLocal(LockPixel(ScreenToPixel(position)));
        else
            position = ScreenToLocal(position);
        AddHurtTrigger((ColliderType)type, position);
    }

    void AddHurtTrigger(ColliderType type, Vector2 position)
    {
        HurtTriggerData hurtTrigger = new HurtTriggerData(type);
        hurtTrigger.collider.position = position;
        animation.frames[currentFrame].hurtTriggers.Add(hurtTrigger);
    }

    bool previewCollider = false;
    Vector2 colliderSize = new Vector2(1, 2);
    float colliderOpacity = 0.5f;
    Material colliderMaterial = null;

    Material hurtMaterial = null;
    Material hitMaterial = null;

    ColliderData currentCollider = null;

    void DrawMeshes()
    {
        if(currentEvent.type == EventType.Repaint)
        {
            if(colliderMaterial == null)
                colliderMaterial = new Material(Shader.Find("GUI/Text Shader"));
            colliderMaterial.color = new Color(0, 0, 1, colliderOpacity);

            if(hurtMaterial == null)
                hurtMaterial = new Material(Shader.Find("GUI/Text Shader"));
            hurtMaterial.color = new Color(0, 1, 0, colliderOpacity);

            if(hitMaterial == null)
                hitMaterial = new Material(Shader.Find("GUI/Text Shader"));
            hitMaterial.color = new Color(1, 0, 0, colliderOpacity);

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            if(previewCollider)
            {
                colliderMaterial.SetPass(0);
                DrawMesh(ColliderData.MakeCapsuleMesh(colliderSize, colliderSize.x < colliderSize.y ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal, Vector2.zero, true), Vector2.zero);
            }

            hurtMaterial.SetPass(0);
            foreach(HurtTriggerData hurtTrigger in animation.frames[currentFrame].hurtTriggers)
                DrawMesh(hurtTrigger.collider.MakeMesh(true), hurtTrigger.collider.position, hurtTrigger.collider.rotation);

            hitMaterial.SetPass(0);
            foreach(HitTriggerData hitTrigger in animation.frames[currentFrame].hitTriggers)
                DrawMesh(hitTrigger.collider.MakeMesh(true), hitTrigger.collider.position, hitTrigger.collider.rotation);

            GL.End();
            GL.PopMatrix();
        }
    }

    void DrawMesh(Mesh mesh, Vector2 position, float rotation = 0)
    {
        float scale = pixelsPerUnit * spriteScale;
        Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(LocalToScreen(position), Quaternion.Euler(0, 0, rotation), new Vector3(scale, scale, 0)));
    }

    Vector2 MouseLocalPosition(bool lockToPixels = false)
    {
        if(lockToPixels)
            return PixelToLocal(LockPixel(ScreenToPixel(currentEvent.mousePosition)));
        return ScreenToLocal(currentEvent.mousePosition);
    }

    Vector2 ScreenToLocal(Vector2 screenPosition)
    {
        float scale = pixelsPerUnit * spriteScale;
        return new Vector2((screenPosition.x - spritePosition.x) / scale, (screenPosition.y - spritePosition.y) / -scale);
    }

    Vector2 LocalToScreen(Vector2 localPosition)
    {
        float scale = pixelsPerUnit * spriteScale;
        return spritePosition + new Vector2(localPosition.x * scale, localPosition.y * -scale);
    }

    Vector2 LocalToPixel(Vector2 localPosition)
    {
        Vector2 pixelPosition = localPosition * pixelsPerUnit;
        if(currentSprite != null)
            pixelPosition += currentSprite.pivot;
        return pixelPosition;
    }

    Vector2 PixelToLocal(Vector2 pixelPosition)
    {
        Vector2 localPosition = pixelPosition;
        if(currentSprite != null)
            localPosition -= currentSprite.pivot;
        return localPosition / pixelsPerUnit;
    }

    Vector2 ScreenToPixel(Vector2 screenPosition)
    {
        Vector2 pixelPosition = new Vector2((screenPosition.x - spritePosition.x) / spriteScale, (screenPosition.y - spritePosition.y) / -spriteScale);
        if(currentSprite != null)
            pixelPosition += currentSprite.pivot;
        return pixelPosition;
    }

    Vector2 PixelToScreen(Vector2 pixelPosition)
    {
        Vector2 screenPosition = pixelPosition;
        if(currentSprite != null)
            screenPosition -= currentSprite.pivot;
        return spritePosition + new Vector2(screenPosition.x * spriteScale, screenPosition.y * -spriteScale);
    }

    Vector2 LockPixel(Vector2 pixelPosition)
    {
        return new Vector2(Mathf.Round(pixelPosition.x), Mathf.Round(pixelPosition.y));
    }
}