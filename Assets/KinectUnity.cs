using UnityEngine;
using UnityEngine.UI;

using System.Collections;

using Windows.Kinect;

public class KinectUnity : MonoBehaviour
{
    public RawImage rawImage;

    private KinectSensor sensor;
    private MultiSourceFrameReader multiReader;

    private CoordinateMapper mapper;

    private FrameDescription colorDescription;
    private FrameDescription bodyIndexDescription;
    private FrameDescription depthDescription;

    private byte[] colorData;
    private byte[] bodyIndexData;
    private ushort[] depthData;

    private Texture2D texture;
    private byte[] textureData;
    

    // Use this for initialization
    void Start ()
    {
        sensor = KinectSensor.GetDefault();

        multiReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
        mapper = sensor.CoordinateMapper;

        colorDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        bodyIndexDescription = sensor.BodyIndexFrameSource.FrameDescription;
        depthDescription = sensor.DepthFrameSource.FrameDescription;

        colorData = new byte[colorDescription.Width * colorDescription.Height * colorDescription.BytesPerPixel];
        bodyIndexData = new byte[bodyIndexDescription.Width * bodyIndexDescription.Height];
        depthData = new ushort[depthDescription.Width * depthDescription.Height];

        texture = new Texture2D(bodyIndexDescription.Width, bodyIndexDescription.Height, TextureFormat.RGBA32, false);
        textureData = new byte[bodyIndexDescription.Width * bodyIndexDescription.Height * 4];

        rawImage.texture = texture;

        if (!sensor.IsOpen)
        {
            sensor.Open();
        }
	}

    void OnApplicationQuit()
    {
        if(multiReader != null)
        {
            multiReader.Dispose();
            multiReader = null;
        }

        if(sensor != null)
        {
            sensor.Close();
            sensor = null;
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        if(multiReader != null)
        {
            var multiFrame = multiReader.AcquireLatestFrame();
            if(multiFrame != null)
            {
                using (var bodyIndexFrame = multiFrame.BodyIndexFrameReference.AcquireFrame())
                using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
                using(var depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
                {
                    if (bodyIndexFrame != null && colorFrame != null && depthFrame != null)
                    {
                        colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Rgba);
                        bodyIndexFrame.CopyFrameDataToArray(bodyIndexData);
                        depthFrame.CopyFrameDataToArray(depthData);

                        ColorSpacePoint[] colorSpacePoints = new ColorSpacePoint[bodyIndexData.Length];
                        mapper.MapDepthFrameToColorSpace(depthData, colorSpacePoints);

                        for (int i = 0; i < bodyIndexData.Length; ++i)
                        {
                            ColorSpacePoint point = colorSpacePoints[i];

                            int colorX = (int)point.X;
                            int colorY = (int)point.Y;

                            // カラー座標にマッピングできたらその色で塗る
                            if (0 <= colorX && colorX < colorDescription.Width
                                && 0 <= colorY && colorY < colorDescription.Height)
                            {
                                int colorIndex = colorY * colorDescription.Width + colorX;
                                if(colorIndex < colorDescription.Width * colorDescription.Height)
                                {
                                    textureData[i * 4 + 0] = colorData[colorIndex * 4 + 0];
                                    textureData[i * 4 + 1] = colorData[colorIndex * 4 + 1];
                                    textureData[i * 4 + 2] = colorData[colorIndex * 4 + 2];
                                }
                            }

                            if (bodyIndexData[i] != 255)
                            {
                                textureData[i * 4 + 3] = 255;
                            }
                            else
                            {
                                // 人以外は透明に
                                textureData[i * 4 + 3] = 0;
                            }
                        }

                        texture.LoadRawTextureData(textureData);
                        texture.Apply();
                    }
                }
            }
        }
	}
}
