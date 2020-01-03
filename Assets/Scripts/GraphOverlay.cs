using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// This is a really simple graphing solution for the WheelCollider's friction slips.
/// </summary> 
public class GraphOverlay : MonoBehaviour
{
    public class Series
    {
        public string name;
        public Color color = Color.red;
        public List<float> values;
        public float scale = 1f;
    }

    [NonSerialized]
    private Dictionary<string, Series> series = new Dictionary<string, Series>();

    public Series GetSeries(string name)
    {
        if(!series.ContainsKey(name))
        {
            series.Add(name, new Series()
            {
                name = name,
                values = new List<float>()
            });
        }
        return series[name];
    }

    protected RawImage imageComponent;

    public float y_scale = 1f;

    public int samplesOnScreen = 20; 

	public float height = 1f;
	public Color32 bgColor = Color.white;
	public Color32 forwardColor = Color.red;
	public Color32 sidewaysColor = Color.green;
	public Color32 guidesColor = Color.blue;
	public Color32 zeroColor = Color.black;

	[Range(0f, 10f)]
	public float timeTravel;

	Color32[] m_PixelsBg;
	Color32[] m_Pixels;
	Text m_SpeedText;
	Texture2D m_Texture;	
	int m_WidthPixels;
	int m_HeightPixels;

    const string k_EventSystemName = "EventSystem";
    const string k_GraphCanvasName = "GraphCanvas";
    const string k_GraphImageName = "RawImage";
    const string k_InfoTextName = "InfoText";
    const float k_GUIScreenEdgeOffset = 10f;
    const int k_InfoFontSize = 16;
    const float k_MaxRecordTimeTravel = 0.01f;

    public void SetLabel(string label, string content)
    {
        GetComponentsInChildren<Text>().Where(c => c.gameObject.name == label).First().text = content;
    }

    private void Awake()
    {
        imageComponent = GetComponent<RawImage>();
    }

    void Start()
	{
        var canvas = FindObjectOfType<Canvas>().gameObject;

        // Set up our texture.
        m_WidthPixels = (int)imageComponent.rectTransform.rect.width;
        m_HeightPixels = (int)imageComponent.rectTransform.rect.height;
        m_Texture = new Texture2D(m_WidthPixels, m_HeightPixels);

		imageComponent.texture = m_Texture;
        imageComponent.SetNativeSize();

		m_Pixels = new Color32[m_WidthPixels * m_HeightPixels];
		m_PixelsBg = new Color32[m_WidthPixels * m_HeightPixels];

	    for (int i = 0; i < m_Pixels.Length; ++i)
	    {
	        m_PixelsBg[i] = bgColor;
	    }
	}

	void Update()
	{
		// Clear.
		Array.Copy(m_PixelsBg, m_Pixels, m_Pixels.Length);

		// Draw guides.
        DrawLine(new Vector2(0f, m_HeightPixels * 0.5f), new Vector2(m_WidthPixels, m_HeightPixels * 0.5f), zeroColor);

		float guide = 1f / height * m_HeightPixels;
        float upperGuide = m_HeightPixels * 0.5f - guide;
        float lowerGuide = m_HeightPixels * 0.5f + guide;
		DrawLine(new Vector2(0f, upperGuide), new Vector2(m_WidthPixels, upperGuide), guidesColor);
		DrawLine(new Vector2(0f, lowerGuide), new Vector2(m_WidthPixels, lowerGuide), guidesColor);

		int stepsBack = (int)(timeTravel / Time.fixedDeltaTime);

        foreach (var series in this.series.Values)
        {
            int cursor = Mathf.Max(series.values.Count - samplesOnScreen - stepsBack, 0);

            for (int i = cursor; i < series.values.Count - 1 - stepsBack; ++i)
            {
                DrawLine(PlotSpace(cursor, i, series.values[i] * series.scale * y_scale), PlotSpace(cursor, i + 1, series.values[i + 1] * series.scale * y_scale), series.color);
            }
        }

        m_Texture.SetPixels32(m_Pixels);
		m_Texture.Apply();
	}

	// Convert time-value to the pixel plot space.
	Vector2 PlotSpace(int cursor, int sample, float value)
	{
		float x = (sample - cursor) *  1f / (float)samplesOnScreen * m_WidthPixels;

		float v = value + height / 2;
		float y = v / height * m_HeightPixels;

		if (y < 0)
			y = 0;

		if (y >= m_HeightPixels)
			y = m_HeightPixels - 1;

		return new Vector2(x, y);
	}

	void DrawLine(Vector2 from, Vector2 to, Color32 color)
	{
		int i;
		int j;

        if(float.IsNaN(from.x) || float.IsInfinity(from.x))
        {
            from.x = 0;
        }
        if (float.IsNaN(from.y) || float.IsInfinity(from.y))
        {
            from.y = 0;
        }
        if (float.IsNaN(to.x) || float.IsInfinity(to.x))
        {
            to.x = 0;
        }
        if (float.IsNaN(to.y) || float.IsInfinity(to.y))
        {
            to.y = 0;
        }

        if (Mathf.Abs(to.x - from.x) > Mathf.Abs(to.y - from.y))
		{
			// Horizontal line.
			i = 0;
			j = 1;
		}
		else
        {
			// Vertical line.
			i = 1;
			j = 0;
		}

		int x = (int)from[i];
		int delta = (int)Mathf.Sign(to[i] - from[i]);
		while (x != (int)to[i])
		{
			int y = (int)Mathf.Round(from[j] + (x - from[i]) * (to[j] - from[j]) / (to[i] - from[i]));

		    int index;
		    if (i == 0)
		        index = y * m_WidthPixels + x;
		    else
		        index = x * m_WidthPixels + y;

            index = Mathf.Clamp(index, 0, m_Pixels.Length - 1);
            m_Pixels[index] = color;

			x += delta;
		}
	}
}