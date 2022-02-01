using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using UnityEditor;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Roxy.Editor
{
		[UnityEditor.AssetImporters.ScriptedImporter(1, "vxm")]
		public class VxmImporter : UnityEditor.AssetImporters.ScriptedImporter
		{
			[SerializeField]
			public Texture2D paletteTexture;
			[SerializeField]
			Color32[] colourArray;
			[SerializeField]
			List<int> _voxels;
			public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
			{
				_voxels = new List<int>();
				colourArray = new Color32[257];
				var tempTexture = new Texture2D(256,1, TextureFormat.RGB24, false); // texture to pass to voxconvert
				if(paletteTexture != null){ 
					if(paletteTexture.height == 2){ // VXM palette format
						for(int x = 0; x < paletteTexture.width; x++){
							tempTexture.SetPixel(x,0,paletteTexture.GetPixel(x, 1));
						}
					}
					for(int x = 0; x < tempTexture.width; x++){
						colourArray[x] = tempTexture.GetPixel(x,0);
					}
					tempTexture.Apply();
				}

				var srcPath = ctx.assetPath;
				var dstPath = ctx.assetPath.Substring(0,ctx.assetPath.Length-3) + "vox";
				var projectPath = Directory.GetCurrentDirectory();
				Debug.Log(Environment.CurrentDirectory);
				var voxConvertPath = $"\"{projectPath}\\Assets\\voxconvert\\vengi-voxconvert.exe\"";
          
				//then Save To Disk as PNG
				var tempPath = $"{projectPath}\\{Guid.NewGuid().ToString("N")}.png";
				File.WriteAllBytes(tempPath, tempTexture.EncodeToPNG());

				var paletteArgs = paletteTexture == null ? "--src-palette" : $"-set palette \"{tempPath}\"";

				var args =  $"{paletteArgs} -i \"{projectPath}\\{srcPath}\" -o \"{projectPath}\\{dstPath}\"";
				var process = new System.Diagnostics.Process();
				process.StartInfo.FileName = voxConvertPath;
				process.StartInfo.Arguments = args;
				Debug.Log(args);
				process.Start();
				process.WaitForExit(); //wait indefinitely for the associated process to exit.

				var data = VoxFileImport.Load(dstPath);

				File.Delete(dstPath);
				File.Delete(tempPath);
				
				var boundsCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				var bounds = data.chunkChild
					.Select(chunk => new Vector3(chunk.size.x, chunk.size.z, chunk.size.y)).Aggregate(
						Vector3.zero,
						(result, chunk) => new Vector3(
							Mathf.Max(result.x, chunk.x),
							Mathf.Max(result.z, chunk.z),
							Mathf.Max(result.y, chunk.y)
						));

				boundsCube.transform.localScale = bounds;
				

				for (var chunkIndex	 = 0; chunkIndex < data.chunkChild.Length; chunkIndex++)
				{
					var chunk = data.chunkChild[chunkIndex];
					var volumeTexture = new Texture3D(chunk.size.x, chunk.size.z, chunk.size.y, TextureFormat.RGBA32, false);
					volumeTexture.filterMode = FilterMode.Point;
					var volumePixels = new Color32[chunk.size.x * chunk.size.z * chunk.size.y];
					
					for (int z = 0; z < chunk.size.y; z++)
					{
						var zSliceTexture = new Texture2D(chunk.size.x, chunk.size.z, TextureFormat.RGBA32, false);
						var pixels = new Color32[chunk.size.x * chunk.size.z];

						for (int x = 0; x < chunk.size.x; x++)
						{
							for (int y = 0; y < chunk.size.z; y++)
							{
								var voxel = chunk.xyzi.voxels.voxels[x, y, z];
								var colour = new Color32(0, 0, 0, 0);
								if(voxel != Int32.MaxValue ){
									_voxels.Add(voxel);
									colour = colourArray[voxel];
								}
								volumePixels[z * chunk.size.x * chunk.size.z + y * chunk.size.x + x] =
								pixels[y * chunk.size.x + x] = colour;
							}
						}

						zSliceTexture.SetPixels32(pixels);
						zSliceTexture.Apply();
						zSliceTexture.name = $"slice-{chunkIndex}-{z}";
						ctx.AddObjectToAsset(zSliceTexture.name, zSliceTexture);
					}
					
					volumeTexture.SetPixels32(volumePixels);
					volumeTexture.Apply();
					volumeTexture.name = $"volume-{chunkIndex}";
					ctx.AddObjectToAsset(volumeTexture.name, volumeTexture);
					if (chunkIndex == 0)
					{
						ctx.SetMainObject(volumeTexture);
					}
				}

				boundsCube.name = "bounds";
				ctx.AddObjectToAsset(boundsCube.name, boundsCube);
				if(paletteTexture != null){
					ctx.AddObjectToAsset("Palette", paletteTexture);
				}
				
			}
		}
	}