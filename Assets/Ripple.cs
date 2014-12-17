using UnityEngine;
using SimpleJSON;
using System;

public class Ripple : MonoBehaviour {

	[Range(10, 100)]
	public int resolution = 100;

	private int currentResolution;
	private ParticleSystem.Particle[] points;

	// Sensor data
	JSONArray json;
	int currentIndex = 0;

	// Hack to represent nulls
	static float nullFloat = -99999;

	float tempMax = nullFloat;
	float tempMin = nullFloat;
	float humMax = nullFloat;
	float humMin = nullFloat;
	float lightMax = nullFloat;
	float lightMin = nullFloat;
	float noiseMax = nullFloat;
	float noiseMin = nullFloat;

	DateTime currentDateTime;
	
	private void CreatePoints () {
		currentResolution = resolution;

		points = new ParticleSystem.Particle[resolution * resolution];

		float increment = 1f / (resolution - 1);

		int i = 0;

		for (int x = 0; x < resolution; x++) {
			for (int z = 0; z < resolution; z++) {
				Vector3 p = new Vector3(x * increment, 0f, z * increment);
				points[i].position = p;
				points[i].color = new Color(p.x, 0f, p.z);
				points[i++].size = 0.1f;
			}
		}

		Debug.Log (points.Length);

		// Load JSON file
		TextAsset dataAsset = Resources.Load("data") as TextAsset;
		string data = dataAsset.text;
		json = JSON.Parse (data).AsArray;

		// Calculate the mins and maxs
		for (int j = 0; j < json.Count; j++) {
			JSONClass sensors = json[j]["sensors"].AsObject;
			
			if (tempMax == nullFloat || tempMax < sensors["temp"]["average"].AsFloat) {
				tempMax = sensors["temp"]["average"].AsFloat;
			}
			
			if (tempMin == nullFloat || tempMin > sensors["temp"]["average"].AsFloat) {
				tempMin = sensors["temp"]["average"].AsFloat;
			}
			
			if (humMax == nullFloat || humMax < sensors["hum"]["average"].AsFloat) {
				humMax = sensors["hum"]["average"].AsFloat;
			}
			
			if (humMin == nullFloat || humMin > sensors["hum"]["average"].AsFloat) {
				humMin = sensors["hum"]["average"].AsFloat;
			}
			
			if (lightMax == nullFloat || lightMax < sensors["light"]["average"].AsFloat) {
				lightMax = sensors["light"]["average"].AsFloat;
			}
			
			if (lightMin == nullFloat || lightMin > sensors["light"]["average"].AsFloat) {
				lightMin = sensors["light"]["average"].AsFloat;
			}
			
			if (noiseMax == nullFloat || noiseMax < sensors["noise"]["average"].AsFloat) {
				noiseMax = sensors["noise"]["average"].AsFloat;
			}
			
			if (noiseMin == nullFloat || noiseMin > sensors["noise"]["average"].AsFloat) {
				noiseMin = sensors["noise"]["average"].AsFloat;
			}
		}
		
		Debug.Log (tempMin + " " + tempMax + " " + humMin + " " + humMax + " " + lightMin + " " + lightMax + " " + noiseMin + " " + noiseMax);

	}

	void Update () {

		if (currentResolution != resolution || points == null) {
			CreatePoints();
		}

		// Time since level loaded
		float time = Time.timeSinceLevelLoad;

		if (currentIndex < json.Count - 1) {
			currentIndex++;
		} else {
			currentIndex = 0;
		}

		// Get the data
		JSONClass data = json [currentIndex].AsObject;

		float temp = data["sensors"]["temp"]["average"].AsFloat;
		float humidity = data["sensors"]["hum"]["average"].AsFloat;
		float light = data["sensors"]["light"]["average"].AsFloat;
		float noise = data["sensors"]["noise"]["average"].AsFloat;

		currentDateTime = UnixTimeStampToDateTime (data ["timestamp"].AsDouble);

//		Debug.Log (temp + " " + humidity + " " + light + " " + noise);

		float middle = resolution / 2;

		// Loop through the points
		for (int i = 0; i < points.Length; i++) {
			float x = i % resolution;
			float y = Mathf.Floor(i / resolution);

			// Get the position from the point
			Vector3 p = points[i].position;

			// Use the function delegate to calculate the y co-ordinate
			p.y = Calculate(p, time);

			// Get the colour from the point
			Color c = Color.white;

			float xMiddle = Mathf.Abs (middle - x) / middle;
			float yMiddle = Mathf.Abs (middle - y) / middle;

//			Debug.Log(xMiddle);

			// Top - humidity
			if (x <= resolution / 2 && y <= resolution / 2) {
				float humidityY = CalculateHumidity(humidity);

				c.r = 0.1f;
				c.g = Mathf.Max (0.3f, (humidityY + 1) / 2);
				c.b = 0.1f;
			}

			// Bottom - temp
			if (x >= resolution / 2 && y >= resolution / 2) {
				float tempY = CalculateTemp(temp);
				c.r = 0.1f;
				c.g = 0.1f;
				c.b = 0.1f;
				
				c.r = Mathf.Max(0.3f, (tempY + 1) / 2);
			}

			// Left - noise
			if (x <= resolution / 2 && y >= resolution / 2) {
				float noiseY = CalculateNoise(noise);
				c.r = 0.1f;
				c.g = 0.1f;
				c.b = Mathf.Max(0.3f, (noiseY + 1) / 2);
			}

			// Right - light
			if (x >= resolution / 2 && y <= resolution / 2) {

				float lightY = CalculateLight(light);

				c.r = Mathf.Max(0.3f, (lightY + 1) / 2);
				c.g = Mathf.Max(0.3f, (lightY + 1) / 2);
				c.b = Mathf.Max(0.3f, (lightY + 1) / 2);
			}

			c = c * (1 - ((xMiddle + yMiddle) / 2));
			
			// Reassign the position
			points[i].position = p;

			// Reassign the colour
			points[i].color = c;
		}

		// Add the particles to the particle system
		particleSystem.SetParticles(points, points.Length);
	}

	private static float Calculate (Vector3 position, float time) {
		position.x -= 0.5f;
		position.z -= 0.5f;
		float squareRadius = position.x * position.x + position.z * position.z;

		return 0.5f + Mathf.Sin(15f * Mathf.PI * squareRadius - 2f * time) / (2f + 100f * squareRadius);
	}
	
	private float CalculateHumidity(float value) {
		return CalculateValue (value, humMin, humMax);
	}
	
	private float CalculateLight(float value) {
		return CalculateValue (value, lightMin, lightMax);
	}
	
	private float CalculateNoise(float value) {
		return CalculateValue (value, noiseMin, noiseMax);
	}
	
	private float CalculateTemp(float value) {
		return CalculateValue (value, tempMin, tempMax);
	}

	private float CalculateValue(float value, float min, float max) {
		float range = max - min;
		float percentage = ((value - min) / range);

		return (percentage * 2) - 1;
	}

	public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
	{
		// Unix timestamp is seconds past epoch
		System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

		dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();

		return dateTime;
	}

	void OnGUI () {
		int width = Screen.width;
		int height = Screen.height;

		var centeredStyle = GUI.skin.GetStyle("Label");
		centeredStyle.alignment = TextAnchor.UpperCenter;
		
		if (currentDateTime != null) {
			GUI.Label (new Rect (0, 0, width, height), currentDateTime.ToShortDateString() + " " + currentDateTime.ToShortTimeString());
		}

		int labelWidth = 100;
		int labelHeight = 40;

		// Top
		GUI.Label (new Rect ((width / 2) - (labelWidth / 2) + 10, (height / 2) - 120, labelWidth, labelHeight), "Humidity", centeredStyle);

		// Left
		GUI.Label (new Rect ((width / 2) - labelWidth - 180, (height / 2) + 40, labelWidth, labelHeight), "Noise", centeredStyle);

		// Right
		GUI.Label (new Rect ((width / 2) + labelWidth + 100, (height / 2) + 40, labelWidth, labelHeight), "Light", centeredStyle);

		// Bottom
		GUI.Label (new Rect ((width / 2) - (labelWidth / 2) + 10, (height / 2) + 240, labelWidth, labelHeight), "Temperature", centeredStyle);

	
	}
}