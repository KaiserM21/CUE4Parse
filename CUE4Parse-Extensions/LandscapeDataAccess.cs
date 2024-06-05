﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse_Extensions;

public class FLandscapeComponentDataInterface
{
	// offset of this component's data into heightmap texture
	private readonly ULandscapeComponent Component;
	private readonly bool bWorkOnEditingLayer;
	private readonly int HeightmapStride;
	private readonly int HeightmapComponentOffsetX;
	private readonly int HeightmapComponentOffsetY;
	public readonly int HeightmapSubsectionOffset;
	private readonly int MipLevel = 0;

    private readonly FColor[] HeightMipData;
    private readonly unsafe FColor* XYOffsetMipData;
    
    private readonly int ComponentSizeVerts;
    private readonly int SubsectionSizeVerts;
    private readonly int ComponentNumSubsections;
    
    private readonly ConcurrentDictionary<string, byte[]> _cache;

    public unsafe FLandscapeComponentDataInterface(ULandscapeComponent InComponent, int InMipLevel, bool InWorkOnEditingLayer)
    {
	    Component = InComponent;
		bWorkOnEditingLayer = InWorkOnEditingLayer;
		HeightMipData = null;
		XYOffsetMipData = null;
		MipLevel = InMipLevel;

		_cache = new ConcurrentDictionary<string, byte[]>();

		UTexture2D heightMapTexture = Component.GetHeightmap(bWorkOnEditingLayer)!;

		var format = heightMapTexture.Format;
		Debug.Assert(heightMapTexture.Format == EPixelFormat.PF_B8G8R8A8);

		if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) format) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0) throw new NotImplementedException($"The supplied pixel format {format} is not supported!");
		
		HeightmapStride = heightMapTexture.PlatformData.SizeX >> MipLevel;
		HeightmapComponentOffsetX = (int)((heightMapTexture.PlatformData.SizeX >> MipLevel) * Component.HeightmapScaleBias.Z);
		HeightmapComponentOffsetY = (int)((heightMapTexture.PlatformData.SizeY >> MipLevel) * Component.HeightmapScaleBias.W);
		HeightmapSubsectionOffset = (Component.SubsectionSizeQuads + 1) >> MipLevel;

		ComponentSizeVerts = (Component.ComponentSizeQuads + 1) >> MipLevel;
		SubsectionSizeVerts = (Component.SubsectionSizeQuads + 1) >> MipLevel;
		ComponentNumSubsections = Component.NumSubsections;
		if (MipLevel < heightMapTexture.PlatformData.Mips.Length)
		{
			Trace.Assert(heightMapTexture.Owner != null, "heightMapTexture.Owner != null");
			var platform = heightMapTexture.Owner!.Provider!.Versions.Platform;
			var mip = heightMapTexture.PlatformData.Mips[MipLevel];
			var bulkData = mip.BulkData.Data;
			
			if (platform == ETexturePlatform.Playstation)
				bulkData =  PlatformDeswizzlers.DeswizzlePS4(bulkData!, mip, formatInfo);
			else if (platform == ETexturePlatform.NintendoSwitch)
				bulkData = PlatformDeswizzlers.GetDeswizzledData(bulkData!, mip, formatInfo);

			var ar = new FStreamArchive("HeightMap",
				new MemoryStream(bulkData ?? throw new InvalidOperationException("height map bulk data is null")));
			HeightMipData = ar.ReadArray<FColor>(bulkData.Length / sizeof(FColor));
			Debug.Assert(ar.Position == bulkData.Length);
			Debug.Assert(HeightMipData.Length == bulkData.Length / sizeof(FColor));
			
		}
		else {
			throw new Exception("MipLevel >= heightMapTexture.PlatformData.Mips.Length");
		}
    }
    
	private bool GetWeightMapIndex(FPackageIndex LayerInfo, bool InUseEditingWeightmap, out int LayerIdx)
	{
		LayerIdx = -1;
		FWeightmapLayerAllocationInfo[] ComponentWeightmapLayerAllocations = Component.GetWeightmapLayerAllocations(InUseEditingWeightmap);
		UTexture2D[] ComponentWeightmapTextures = Component.GetWeightmapTextures(InUseEditingWeightmap);

		for (int Idx = 0; Idx < ComponentWeightmapLayerAllocations.Length; Idx++)
		{
			if (ComponentWeightmapLayerAllocations[Idx].LayerInfo.Equals(LayerInfo))
			{
				LayerIdx = Idx;
				break;
			}
		}
		if (LayerIdx < 0)
		{
			return false;
		}
		if (ComponentWeightmapLayerAllocations[LayerIdx].WeightmapTextureIndex >= ComponentWeightmapTextures.Length)
		{
			return false;
		}
		if (ComponentWeightmapLayerAllocations[LayerIdx].WeightmapTextureChannel >= 4)
		{
			return false;
		}
		return true;
	}

	private bool GetWeightmapTextureData(FPackageIndex /*ULandscapeLayerInfoObject*/ LayerInfo, out byte[] OutData, bool InUseEditingWeightmap)
	{
		if (_cache.TryGetValue(LayerInfo.Name, out var cached))
		{
			OutData = cached;
			return true;
		}
		
		OutData = Array.Empty<byte>();
		FWeightmapLayerAllocationInfo[] ComponentWeightmapLayerAllocations = Component.GetWeightmapLayerAllocations(InUseEditingWeightmap);
		UTexture2D[] ComponentWeightmapTextures = Component.GetWeightmapTextures(InUseEditingWeightmap);

		if (!GetWeightMapIndex(LayerInfo, InUseEditingWeightmap, out var LayerIdx))
		{
			return false;
		}

		int weightmapSize = ((Component.SubsectionSizeQuads + 1) * Component.NumSubsections) >> MipLevel;
		OutData = new byte[weightmapSize*weightmapSize]; // not *4 because we only want one channel

		// BGRA
		var weightTexture =
			ComponentWeightmapTextures[ComponentWeightmapLayerAllocations[LayerIdx].WeightmapTextureIndex];
		var format = weightTexture.Format;
		if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) format) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0) throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

		var platform = weightTexture.Owner!.Provider!.Versions.Platform;

		var mip = weightTexture.PlatformData.Mips[MipLevel];
		var bulkData = mip.BulkData.Data;
		if (platform == ETexturePlatform.Playstation)
			bulkData =  PlatformDeswizzlers.DeswizzlePS4(bulkData!, mip, formatInfo);
		else if (platform == ETexturePlatform.NintendoSwitch)
			bulkData = PlatformDeswizzlers.GetDeswizzledData(bulkData!, mip, formatInfo);

		FByteArchive ar = new FByteArchive("WeightMapData",  bulkData);

		// remember: FColor is BGRA
		// Channel remapping
		int[] channelOffsets = new int[4] { (int)Marshal.OffsetOf(typeof(FColor), "R"),
			(int)Marshal.OffsetOf<FColor>("G"), (int)Marshal.OffsetOf<FColor>("B"), (int)Marshal.OffsetOf<FColor>("A") };
	
		var offset = channelOffsets[ComponentWeightmapLayerAllocations[LayerIdx].WeightmapTextureChannel];

		// separate the channel
		for (int i = 0; i < weightmapSize*weightmapSize; i++)
		{
			ar.Seek(i * 4 + offset, SeekOrigin.Begin);
			OutData[i] = ar.Read<byte>();
		}
		ar.Dispose();

		_cache.TryAdd(LayerInfo.Name, OutData);
		return true;
	}
	
	public FColor GetHeightData(int localX, int localY)
	{
		VertexXYToTexelXY(localX, localY, out var texelX, out var texelY);
		return HeightMipData[texelX + HeightmapComponentOffsetX + (texelY + HeightmapComponentOffsetY) * HeightmapStride];
	}

	public byte GetLayerWeight(int localX, int localY, FPackageIndex /*ULandscapeLayerInfoObject*/ layerInfo)
	{
		VertexXYToTexelXY(localX, localY, out var texelX, out var texelY);

		var weightData = GetWeightmapTextureData(layerInfo, out var outData, false);

		if (!GetWeightMapIndex(layerInfo, false, out var LayerIdx)) {
			return 0;
		}

		FWeightmapLayerAllocationInfo[] componentWeightmapLayerAllocations = Component.GetWeightmapLayerAllocations(false);
		var weightmapTexture = Component.GetWeightmapTextures(false)[componentWeightmapLayerAllocations[LayerIdx].WeightmapTextureIndex];

		var weightmapStride = weightmapTexture.PlatformData.SizeX >> MipLevel;
		var weightmapComponentOffsetX = (int)((weightmapTexture.PlatformData.SizeX >> MipLevel) * Component.WeightmapScaleBias.Z);
		var weightmapComponentOffsetY = (int)((weightmapTexture.PlatformData.SizeY >> MipLevel) * Component.WeightmapScaleBias.W);

		if (weightData)
		{
			return outData[texelX + weightmapComponentOffsetX + (texelY + weightmapComponentOffsetY) * weightmapStride];
		}

		return 0;
	}
	
	void VertexXYToTexelXY(int VertX, int VertY, out int OutX, out int OutY)
	{
		ComponentXYToSubsectionXY(VertX, VertY, out var SubNumX, out var SubNumY, out var SubX, out var SubY);

		OutX = SubNumX * SubsectionSizeVerts + SubX;
		OutY = SubNumY * SubsectionSizeVerts + SubY;
	}

	void ComponentXYToSubsectionXY(int compX, int compY, out int subNumX, out int subNumY, out int subX, out int subY)
	{
		// We do the calculation as if we're looking for the previous vertex.
		// This allows us to pick up the last shared vertex of every subsection correctly.
		subNumX = (compX-1) / (SubsectionSizeVerts - 1);
		subNumY = (compY-1) / (SubsectionSizeVerts - 1);
		subX = (compX-1) % (SubsectionSizeVerts - 1) + 1;
		subY = (compY-1) % (SubsectionSizeVerts - 1) + 1;

		// If we're asking for the first vertex, the calculation above will lead
		// to a negative SubNumX/Y, so we need to fix that case up.
		if( subNumX < 0 )
		{
			subNumX = 0;
			subX = 0;
		}

		if( subNumY < 0 )
		{
			subNumY = 0;
			subY = 0;
		}
	}

	public void VertexIndexToXY(int vertexIndex, out int outX, out int outY)
	{
#if true //LANDSCAPE_VALIDATE_DATA_ACCESS
		Debug.Assert(MipLevel == 0);
#endif
		outX = vertexIndex % ComponentSizeVerts;
		outY = vertexIndex / ComponentSizeVerts;
	}
	
    ushort GetHeight(int localX, int localY )
	{
		FColor texel = GetHeightData(localX, localY);
		return (ushort)((texel.R << 8) + texel.G);
		// return (ushort)((texel->R << 8) + texel->G);
	}
    
	public FVector GetLocalVertex(int localX, int localY)
	{
		var scaleFactor = (float)Component.ComponentSizeQuads / (float)(ComponentSizeVerts - 1);
		return new FVector(localX * scaleFactor, localY * scaleFactor, GetLocalHeight(GetHeight(localX, localY)));
	}

	float GetLocalHeight(ushort height)
	{
		const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
		const int MaxValue = 65535;
		const float MidValue = 32768f;
		// Reserved 2 bits for other purpose
		// Most significant bit - Visibility, 0 is visible(default), 1 is invisible
		// 2nd significant bit - Triangle flip, not implemented yet
		return (height - MidValue) * LANDSCAPE_ZSCALE;
	}
	
	void GetLocalTangentVectors(int localX, int localY, out FVector localTangentX, out FVector localTangentY, out FVector localTangentZ)
	{
		FColor data = GetHeightData(localX, localY);
    
		// the normal vector components
		localTangentZ.X = 2.0f * data.B / 255.0f - 1.0f;
		localTangentZ.Y = 2.0f * data.A / 255.0f - 1.0f;
		
		float zSquared = 1.0f - (localTangentZ.X * localTangentZ.X + localTangentZ.Y * localTangentZ.Y);
		localTangentZ.Z = (zSquared > 0.0f) ? (float)Math.Sqrt(zSquared) : 0.0f;
    
		// tangent vectors based on Z normal
		localTangentX = new FVector(-localTangentZ.Z, 0.0f, localTangentZ.X).GetSafeNormal();
		localTangentY = new FVector(0.0f, localTangentZ.Z, -localTangentZ.Y).GetSafeNormal();

		localTangentZ = localTangentZ.GetSafeNormal();
	}


	public void GetLocalTangentVectors(int vertexIndex, out FVector localTangentX, out FVector localTangentY, out FVector localTangentZ )
	{
		VertexIndexToXY( vertexIndex, out var x, out var y);
		GetLocalTangentVectors( x, y, out localTangentX, out localTangentY, out localTangentZ );
	}
}