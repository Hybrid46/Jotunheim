using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class TextureMaker : MonoBehaviour
{
    public enum Channel { Skip, sourceRed, sourceGreen, sourceBlue, sourceAlpha };

    [Space(30)]
    [Header("Sources")]
    [Space(10)]
    public Texture2D[] source;

    [Space(30)]
    [Header("Channels")]
    [Space(10)]
    public Channel targetRed;
    public Channel targetGreen;
    public Channel targetBlue;
    public Channel targetAlpha;

    [Space(20)]
    public bool generate;

    [Space(10)]
    [Header("Targets")]
    [Space(10)]
    public List<Texture2D> target;

    void Update()
    {
        if (generate == true)
        {
            generate = false;

            for (int i = 0; i < source.Length; i++)
            {
                if (target == null || target.Count == 0)
                {
                    target = new List<Texture2D>(source.Length);

                    Texture2D tempTex = new Texture2D(source[i].width, source[i].height, source[i].format, source[i].mipmapCount > 0);
                    tempTex.filterMode = source[i].filterMode;
                    tempTex.alphaIsTransparency = source[i].alphaIsTransparency;
                    tempTex.anisoLevel = source[i].anisoLevel;
                    tempTex.wrapMode = source[i].wrapMode;

                    tempTex.Apply();

                    target.Add(tempTex);
                }

                Color32[] sourceColors = source[i].GetPixels32();
                Color32[] targetColors = target[i].GetPixels32();

                for (int c = 0; c < sourceColors.Length; c++)
                {
                    switch (targetRed)
                    {
                        case Channel.Skip:
                            break;
                        case Channel.sourceRed:
                            targetColors[c].r = sourceColors[c].r;
                            break;
                        case Channel.sourceGreen:
                            targetColors[c].r = sourceColors[c].g;
                            break;
                        case Channel.sourceBlue:
                            targetColors[c].r = sourceColors[c].b;
                            break;
                        case Channel.sourceAlpha:
                            targetColors[c].r = sourceColors[c].a;
                            break;
                        default:
                            break;
                    }

                    switch (targetGreen)
                    {
                        case Channel.Skip:
                            break;
                        case Channel.sourceRed:
                            targetColors[c].g = sourceColors[c].r;
                            break;
                        case Channel.sourceGreen:
                            targetColors[c].g = sourceColors[c].g;
                            break;
                        case Channel.sourceBlue:
                            targetColors[c].g = sourceColors[c].b;
                            break;
                        case Channel.sourceAlpha:
                            targetColors[c].g = sourceColors[c].a;
                            break;
                        default:
                            break;
                    }

                    switch (targetBlue)
                    {
                        case Channel.Skip:
                            break;
                        case Channel.sourceRed:
                            targetColors[c].b = sourceColors[c].r;
                            break;
                        case Channel.sourceGreen:
                            targetColors[c].b = sourceColors[c].g;
                            break;
                        case Channel.sourceBlue:
                            targetColors[c].b = sourceColors[c].b;
                            break;
                        case Channel.sourceAlpha:
                            targetColors[c].b = sourceColors[c].a;
                            break;
                        default:
                            break;
                    }

                    switch (targetAlpha)
                    {
                        case Channel.Skip:
                            break;
                        case Channel.sourceRed:
                            targetColors[c].a = sourceColors[c].r;
                            break;
                        case Channel.sourceGreen:
                            targetColors[c].a = sourceColors[c].g;
                            break;
                        case Channel.sourceBlue:
                            targetColors[c].a = sourceColors[c].b;
                            break;
                        case Channel.sourceAlpha:
                            targetColors[c].a = sourceColors[c].a;
                            break;
                        default:
                            break;
                    }
                }

                target[i].SetPixels32(targetColors);
                target[i].Apply();

                //Save texture
            }
        }
    }
}
