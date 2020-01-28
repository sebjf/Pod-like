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

        public Text upperLimitLabel;
        public Text lowerLimitLabel;
        public Text labelLabel;
    }

    [NonSerialized]
    private Dictionary<string, Series> series = new Dictionary<string, Series>();

    public Series GetSeries(string name)
    {
        if(!series.ContainsKey(name))
        {
            AddSeries(name);
        }
        return series[name];
    }

    private Series AddSeries(string name)
    {
        var series = new Series();
        series.name = name;
        series.values = new List<float>();

        series.upperLimitLabel = new GameObject(name).AddComponent<Text>();
        series.upperLimitLabel.rectTransform.SetParent(scaleLabelsUpper.transform, false);
        series.upperLimitLabel.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        series.lowerLimitLabel = new GameObject(name).AddComponent<Text>();
        series.lowerLimitLabel.rectTransform.SetParent(scaleLabelsLower.transform, false);
        series.lowerLimitLabel.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        series.labelLabel = new GameObject(name).AddComponent<Text>();
        series.labelLabel.rectTransform.SetParent(labels.transform, false);
        series.labelLabel.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        this.series.Add(name, series);
        return series;
    }

    private RawImage imageComponent;
    public HorizontalOrVerticalLayoutGroup scaleLabelsUpper;
    public HorizontalOrVerticalLayoutGroup scaleLabelsLower;
    public HorizontalOrVerticalLayoutGroup labels;

    public float y_scale = 1f;

    public int samplesOnScreen = 20; 

	public Color32 backgroundColour = Color.white;
	public Color32 guidesColour = Color.blue;
	public Color32 zeroColour = Color.black;

    public int stepsBack;

	Color32[] m_PixelsBg;
	Color32[] m_Pixels;
	Texture2D m_Texture;	
	int m_WidthPixels;
	int m_HeightPixels;

    public void SetLabel(string label, string content)
    {
        GetComponentsInChildren<Text>().Where(c => c.gameObject.name == label).First().text = content;
    }

    private void Awake()
    {
        imageComponent = GetComponentInChildren<RawImage>();
    }

    void Start()
	{
        // Set up our texture.
        m_WidthPixels = (int)imageComponent.rectTransform.rect.width;
        m_HeightPixels = (int)imageComponent.rectTransform.rect.height;
        m_Texture = new Texture2D(m_WidthPixels, m_HeightPixels);
		imageComponent.texture = m_Texture;

		m_Pixels = new Color32[m_WidthPixels * m_HeightPixels];
		m_PixelsBg = new Color32[m_WidthPixels * m_HeightPixels];

	    for (int i = 0; i < m_Pixels.Length; ++i)
	    {
	        m_PixelsBg[i] = backgroundColour;
	    }
	}

	void Update()
	{
		// Clear.
		Array.Copy(m_PixelsBg, m_Pixels, m_Pixels.Length);

		// Draw guides.
        DrawLine(new Vector2(0f, m_HeightPixels * 0.5f), new Vector2(m_WidthPixels, m_HeightPixels * 0.5f), zeroColour);

        /*
		float guide = 0.5f * m_HeightPixels;
        float upperGuide = m_HeightPixels * 0.5f - guide;
        float lowerGuide = m_HeightPixels * 0.5f + guide;
		DrawLine(new Vector2(0f, upperGuide), new Vector2(m_WidthPixels, upperGuide), guidesColour);
		DrawLine(new Vector2(0f, lowerGuide), new Vector2(m_WidthPixels, lowerGuide), guidesColour);
        */

        foreach (var series in this.series.Values)
        {
            int sampleStart = Mathf.Max(series.values.Count - samplesOnScreen - stepsBack, 0);
            int sampleEnd = Mathf.Min(sampleStart + samplesOnScreen, series.values.Count);

            for (int i = sampleStart; i < sampleEnd - 1; i++)
            {
                DrawLine(PlotSpace(sampleStart, i, series.values[i] * series.scale * y_scale), PlotSpace(sampleStart, i + 1, series.values[i + 1] * series.scale * y_scale), series.color);
            }

            series.upperLimitLabel.color = series.color;
            series.upperLimitLabel.text = string.Format("{0}", (series.scale * y_scale));
            
            series.lowerLimitLabel.color = series.color;
            series.lowerLimitLabel.text = string.Format("-{0}", (series.scale * y_scale));

            series.labelLabel.color = series.color;
            series.labelLabel.text = series.name;
        }

        m_Texture.SetPixels32(m_Pixels);
		m_Texture.Apply();
	}

	// Convert time-value to the pixel plot space.
	Vector2 PlotSpace(int cursor, int sample, float value)
	{
		float x = ((sample - cursor) / (float)(samplesOnScreen - 1)) * m_WidthPixels;
		float y = (value + 0.5f) * m_HeightPixels;

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