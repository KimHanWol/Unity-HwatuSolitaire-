using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>uGUI를 코드로 조립하는 도우미. 글꼴은 주아(OFL).</summary>
public static class Ui
{
    static Font font;

    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.Load<Font>("Fonts/Jua-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>부모를 꽉 채운다 (안쪽 여백 inset).</summary>
    public static RectTransform Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    /// <summary>앵커 한 점(ax, ay: 0~1)에 크기 size로 놓는다.</summary>
    public static RectTransform Place(RectTransform rt, float ax, float ay, Vector2 size, Vector2 offset = default)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        return rt;
    }

    public static Image Image(Transform parent, Sprite sprite, Color color, string name = "Image")
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
        return img;
    }

    public static Text Text(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, string name = "Text")
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        return t;
    }

    /// <summary>글자에 테두리를 두른다 (금박 제목, 도장 글씨 등).</summary>
    public static Text Outline(Text t, Color color, float width = 3f)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(width, -width);
        var o2 = t.gameObject.AddComponent<Outline>();
        o2.effectColor = color;
        o2.effectDistance = new Vector2(-width, width);
        return t;
    }

    public static Text Shadow(Text t, Color color, float distance = 4f)
    {
        var s = t.gameObject.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = new Vector2(0f, -distance);
        return t;
    }

    /// <summary>
    /// 입체 버튼: 아래쪽 진한 "입술" 위에 밝은 면. 누르면 면이 내려앉고 딸깍 소리가 난다.
    /// label이나 icon 중 하나 또는 둘 다.
    /// </summary>
    public static Button Button(Transform parent, Vector2 size, Color face, Color lip, string label, Sprite icon, Action onClick, int fontSize = 52, string name = "Button")
    {
        var root = Rect(name, parent);
        root.sizeDelta = size;
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);
        var button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        const float lipHeight = 10f;
        var lipImg = Image(root, Art.Round, lip, "Lip");
        Stretch(lipImg.rectTransform);
        var faceImg = Image(root, Art.Round, face, "Face");
        Stretch(faceImg.rectTransform);
        faceImg.rectTransform.offsetMin = new Vector2(0f, lipHeight);
        // 윗면 반사광
        var shine = Image(faceImg.rectTransform, Art.Round, new Color(1f, 1f, 1f, 0.18f), "Shine");
        Stretch(shine.rectTransform);
        shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        shine.rectTransform.offsetMin = new Vector2(8f, 0f);
        shine.rectTransform.offsetMax = new Vector2(-8f, -6f);

        var content = Rect("Content", faceImg.rectTransform);
        Stretch(content);
        float textLeft = 0f;
        if (icon != null)
        {
            var ic = Image(content, icon, Color.white, "Icon");
            float s = Mathf.Min(size.y - lipHeight, 120f) * 0.52f;
            if (string.IsNullOrEmpty(label)) Place(ic.rectTransform, 0.5f, 0.5f, new Vector2(s, s));
            else
            {
                Place(ic.rectTransform, 0f, 0.5f, new Vector2(s, s), new Vector2(s * 0.5f + 34f, 0f));
                textLeft = s + 30f;
            }
        }
        if (!string.IsNullOrEmpty(label))
        {
            var t = Text(content, label, fontSize, Color.white, TextAnchor.MiddleCenter, "Label");
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(textLeft, 2f);
            Shadow(t, new Color(0f, 0f, 0f, 0.35f), 3f);
        }

        var press = root.gameObject.AddComponent<PressFeedback>();
        press.face = faceImg.rectTransform;
        press.lipHeight = lipHeight;
        button.onClick.AddListener(() =>
        {
            Sfx.Click();
            onClick?.Invoke();
        });
        return button;
    }

    /// <summary>보상형 광고 버튼: 글자 왼쪽에 "AD" 꼬리표를 붙인다 (아이콘 없이 만든 버튼에).</summary>
    public static Button AdTag(Button b, Color ink)
    {
        var content = (RectTransform)b.transform.Find("Face/Content");
        var label = content.Find("Label") as RectTransform;
        var tag = Image(content, Art.Round, new Color(1f, 0.96f, 0.82f), "AdTag");
        Place(tag.rectTransform, 0f, 0.5f, new Vector2(92f, 56f), new Vector2(46f + 30f, 2f));
        var t = Text(tag.rectTransform, "AD", 36, ink, TextAnchor.MiddleCenter, "Text");
        Stretch(t.rectTransform);
        if (label != null) label.offsetMin = new Vector2(92f + 30f, label.offsetMin.y);
        return b;
    }

    /// <summary>버튼 글자를 바꾼다 (언어 전환 등).</summary>
    public static void SetLabel(Button b, string label)
    {
        var t = b.transform.Find("Face/Content/Label")?.GetComponent<Text>();
        if (t != null) t.text = label;
    }

    /// <summary>버튼 색을 바꾼다 (켜기/끄기 스위치 등).</summary>
    public static void SetColors(Button b, Color face, Color lip)
    {
        var f = b.transform.Find("Face")?.GetComponent<Image>();
        var l = b.transform.Find("Lip")?.GetComponent<Image>();
        if (f != null) f.color = face;
        if (l != null) l.color = lip;
    }

    /// <summary>둥근 버튼 (아이콘만).</summary>
    public static Button IconButton(Transform parent, float size, Color face, Color lip, Sprite icon, Action onClick, string name = "IconButton")
        => Button(parent, new Vector2(size, size), face, lip, null, icon, onClick, 40, name);

    /// <summary>화면 위를 덮는 반투명 막 (팝업 뒤). 누르면 onTap.</summary>
    public static Image Dim(Transform parent, float alpha, Action onTap = null)
    {
        var img = Image(parent, null, new Color(0f, 0f, 0f, alpha), "Dim");
        Stretch(img.rectTransform);
        img.raycastTarget = true;
        if (onTap != null)
        {
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => onTap());
        }
        return img;
    }

    /// <summary>한지 느낌 판: 진한 테두리 + 크림색 속지 + 금색 가는 줄.</summary>
    public static RectTransform Panel(Transform parent, Vector2 size, string name = "Panel")
    {
        var root = Rect(name, parent);
        root.sizeDelta = size;
        var shadow = Image(root, Art.Round, new Color(0f, 0f, 0f, 0.35f), "Shadow");
        Stretch(shadow.rectTransform);
        shadow.rectTransform.offsetMin = new Vector2(6f, -16f);
        shadow.rectTransform.offsetMax = new Vector2(6f, -4f);
        var frame = Image(root, Art.Round, Art.Wood, "Frame");
        Stretch(frame.rectTransform);
        var paper = Image(root, Art.Round, Art.Paper, "Paper");
        Stretch(paper.rectTransform, 14f);
        var line = Image(root, Art.RoundOutline, Art.GoldDark, "Line");
        Stretch(line.rectTransform, 22f);
        line.color = new Color(Art.GoldDark.r, Art.GoldDark.g, Art.GoldDark.b, 0.55f);
        var block = root.gameObject.AddComponent<Image>();
        block.color = new Color(0, 0, 0, 0);
        block.raycastTarget = true;
        return root;
    }

    /// <summary>EventSystem이 없으면 만든다.</summary>
    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(go);
    }
}

/// <summary>버튼을 누르는 동안 윗면이 입술 높이만큼 내려앉는다.</summary>
public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform face;
    public float lipHeight = 10f;
    bool down;

    public void OnPointerDown(PointerEventData e) => Set(true);
    public void OnPointerUp(PointerEventData e) => Set(false);
    public void OnPointerExit(PointerEventData e) => Set(false);

    void OnDisable() => Set(false);

    void Set(bool pressed)
    {
        if (face == null || down == pressed) return;
        down = pressed;
        face.offsetMin = new Vector2(0f, pressed ? 2f : lipHeight);
        face.offsetMax = new Vector2(0f, pressed ? -(lipHeight - 2f) : 0f);
    }
}
