using UnityEngine;
using System.Text;
using System.IO;
using System;

namespace Crosstales.Common.Audio
{
   /// <summary>
   /// WAV utility for recording and audio playback functions in Unity.
   ///
   /// - Use "ToAudioClip" method for loading wav file / bytes.
   /// Loads .wav (PCM uncompressed) files at 8,16,24 and 32 bits and converts data to Unity's AudioClip.
   ///
   /// - Use "FromAudioClip" method for saving wav file / bytes.
   /// Converts an AudioClip's float data into wav byte array at 16 bit.
   /// </summary>
   /// <remarks>
   /// Partially based on: https://github.com/deadlyfingers/UnityWav
   /// </remarks>
   public abstract class WavMaster
   {
      #region Variables

      // Force save as 16-bit .wav
      private const int blockSize_16Bit = 2;

      #endregion


      #region Static methods

      /// <summary>Load PCM format *.wav audio file and convert to AudioClip.</summary>
      /// <param name="filePath">Local file path to .wav file</param>
      /// <param name="name">Name of the AudioClip (default: wav, optional)</param>
      /// <returns>AudioClip from the byte-array.</returns>
      public static AudioClip ToAudioClip(string filePath, string name = "wav")
      {
         /*
         if (!filePath.StartsWith(Application.persistentDataPath) && !filePath.StartsWith(Application.dataPath))
         {
            Debug.LogWarning("This only supports files that are stored using Unity's Application data path. \nTo load bundled resources use 'Resources.Load(\"filename\") typeof(AudioClip)' method. \nhttps://docs.unity3d.com/ScriptReference/Resources.Load.html");
            return null;
         }
*/
         try
         {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            return ToAudioClip(fileBytes, name);
         }
         catch (Exception ex)
         {
            Debug.LogError("Could not read audio file: " + ex);
         }

         return null;
      }

      /// <summary>Load PCM format *.wav audio stream and convert to AudioClip.</summary>
      /// <param name="stream">Local file path to .wav file</param>
      /// <param name="name">Name of the AudioClip (default: wav, optional)</param>
      /// <returns>AudioClip from the byte-array.</returns>
      public static AudioClip ToAudioClip(Stream stream, string name = "wav")
      {
         try
         {
            return ToAudioClip(stream.CTReadFully(), name);
         }
         catch (Exception ex)
         {
            Debug.LogError("Could not read audio stream: " + ex);
         }

         return null;
      }

      /// <summary>Load PCM format byte-array and convert to AudioClip.</summary>
      /// <param name="fileBytes">Byte array with the PCM data</param>
      /// <param name="name">Name of the AudioClip (default: wav, optional)</param>
      /// <returns>AudioClip from the byte-array.</returns>
      public static AudioClip ToAudioClip(byte[] fileBytes, string name = "wav")
      {
         //string riff = Encoding.ASCII.GetString (fileBytes, 0, 4);
         //string wave = Encoding.ASCII.GetString (fileBytes, 8, 4);
         int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
         ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);

         // NB: Only uncompressed PCM wav files are supported.
         string formatCode = WavMaster.formatCode(audioFormat);
         Debug.AssertFormat(audioFormat == 1 || audioFormat == 65534, "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", audioFormat, formatCode);

         ushort channels = BitConverter.ToUInt16(fileBytes, 22);

         int sampleRate = BitConverter.ToInt32(fileBytes, 24);
         //int byteRate = BitConverter.ToInt32 (fileBytes, 28);
         //ushort blockAlign = BitConverter.ToUInt16 (fileBytes, 32);
         ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

         int headerOffset = 16 + 4 + subchunk1 + 4;
         int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);

         //Debug.Log($"{channels} - {sampleRate} - {bitDepth}");

         float[] data;
         switch (bitDepth)
         {
            case 8:
               data = convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
               break;
            case 16:
               data = convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
               break;
            case 24:
               data = convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
               break;
            case 32:
               data = convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
               break;
            default:
               throw new Exception(bitDepth + " bit depth is not supported.");
         }

         AudioClip audioClip = AudioClip.Create(name, data.Length / channels, channels, sampleRate, false);
         audioClip.SetData(data, 0);
         return audioClip;
      }

      /// <summary>Convert an AudioClip to a byte-array.</summary>
      /// <param name="audioClip">AudioClip to convert</param>
      /// <returns>AudioClip as byte-array.</returns>
      public static byte[] FromAudioClip(AudioClip audioClip)
      {
         return FromAudioClip(audioClip, null, false);
      }

      /// <summary>Convert an AudioClip to a byte-array and save it to a file.</summary>
      /// <param name="audioClip">AudioClip to save</param>
      /// <param name="filepath">File path</param>
      /// <param name="saveAsFile">Save the file (default: true, optional)</param>
      /// <returns>AudioClip as byte-array.</returns>
      public static byte[] FromAudioClip(AudioClip audioClip, string filepath, bool saveAsFile = true)
      {
         byte[] bytes = null;

         using (MemoryStream stream = new MemoryStream())
         {
            const int headerSize = 44;

            // get bit depth
            ushort bitDepth = 16; //BitDepth (audioClip);

            // NB: Only supports 16 bit

            // total file size = 44 bytes for header format and audioClip.samples * factor due to float to Int16 / sbyte conversion
            int fileSize = audioClip.samples * blockSize_16Bit + headerSize; // BlockSize (bitDepth)

            // chunk descriptor (riff)
            writeFileHeader(stream, fileSize);
            // file header (fmt)
            writeFileFormat(stream, audioClip.channels, audioClip.frequency, bitDepth);
            // data chunks (data)
            writeFileData(stream, audioClip);

            bytes = stream.ToArray();

            // Validate total bytes
            Debug.AssertFormat(bytes.Length == fileSize, "Unexpected AudioClip to wav format byte count: {0} == {1}", bytes.Length, fileSize);

            // Save file to persistant storage location
            if (saveAsFile)
            {
               try
               {
                  File.WriteAllBytes(filepath, bytes);
               }
               catch (Exception ex)
               {
                  Debug.LogError("Could not save audio file: " + ex);
                  //Debug.Log ("Auto-saved .wav file: " + filepath);
               }
            }
         }

         return bytes;
      }

      /// <summary>Calculates the bit depth of an AudioClip.</summary>
      /// <param name="audioClip">Audio clip.</param>
      /// <returns>The bit depth. Should be 8 or 16 or 32 bit.</returns>
      public static ushort BitDepth(AudioClip audioClip)
      {
         ushort bitDepth = Convert.ToUInt16(audioClip.samples * audioClip.channels * audioClip.length / audioClip.frequency);
         Debug.AssertFormat(bitDepth == 8 || bitDepth == 16 || bitDepth == 32, "Unexpected AudioClip bit depth: {0}. Expected 8 or 16 or 32 bit.", bitDepth);
         return bitDepth;
      }

      #endregion


      #region Private methods

      private static float[] convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
      {
         int wavSize = BitConverter.ToInt32(source, headerOffset);
         headerOffset += sizeof(int);
         Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 8-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

         float[] data = new float[wavSize];

         sbyte maxValue = sbyte.MaxValue;

         for (int ii = 0; ii < wavSize; ii++)
         {
            data[ii] = (float)source[ii] / maxValue;
         }

         return data;
      }

      private static float[] convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
      {
         int wavSize = BitConverter.ToInt32(source, headerOffset);
         headerOffset += sizeof(int);
         Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 16-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

         int x = sizeof(Int16); // block size = 2
         int convertedSize = wavSize / x;

         float[] data = new float[convertedSize];

         Int16 maxValue = Int16.MaxValue;

         for (int ii = 0; ii < convertedSize; ii++)
         {
            int offset = ii * x + headerOffset;
            data[ii] = (float)BitConverter.ToInt16(source, offset) / maxValue;
         }

         Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

         return data;
      }

      private static float[] convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
      {
         int wavSize = BitConverter.ToInt32(source, headerOffset);
         headerOffset += sizeof(int);
         Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 24-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

         int x = 3; // block size = 3
         int convertedSize = wavSize / x;

         int maxValue = Int32.MaxValue;

         float[] data = new float[convertedSize];

         byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

         for (int ii = 0; ii < convertedSize; ii++)
         {
            int offset = ii * x + headerOffset;
            Buffer.BlockCopy(source, offset, block, 1, x);
            data[ii] = (float)BitConverter.ToInt32(block, 0) / maxValue;
         }

         Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

         return data;
      }

      private static float[] convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
      {
         int wavSize = BitConverter.ToInt32(source, headerOffset);
         headerOffset += sizeof(int);
         Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 32-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

         int x = sizeof(float); //  block size = 4
         int convertedSize = wavSize / x;

         Int32 maxValue = Int32.MaxValue;

         float[] data = new float[convertedSize];

         for (int ii = 0; ii < convertedSize; ii++)
         {
            int offset = ii * x + headerOffset;
            data[ii] = (float)BitConverter.ToInt32(source, offset) / maxValue;
         }

         Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

         return data;
      }

      private static int writeFileHeader(MemoryStream stream, int fileSize)
      {
         int count = 0;
         int total = 12;

         // riff chunk id
         byte[] riff = Encoding.ASCII.GetBytes("RIFF");
         count += writeBytesToMemoryStream(stream, riff);

         // riff chunk size
         int chunkSize = fileSize - 8; // total size - 8 for the other two fields in the header
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(chunkSize));

         byte[] wave = Encoding.ASCII.GetBytes("WAVE");
         count += writeBytesToMemoryStream(stream, wave);

         // Validate header
         Debug.AssertFormat(count == total, "Unexpected wav descriptor byte count: {0} == {1}", count, total);

         return count;
      }

      private static int writeFileFormat(MemoryStream stream, int channels, int sampleRate, ushort bitDepth)
      {
         int count = 0;
         int total = 24;

         byte[] id = Encoding.ASCII.GetBytes("fmt ");
         count += writeBytesToMemoryStream(stream, id);

         int subchunk1Size = 16; // 24 - 8
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(subchunk1Size));

         ushort audioFormat = 1;
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(audioFormat));

         ushort numChannels = Convert.ToUInt16(channels);
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(numChannels));

         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(sampleRate));

         int byteRate = sampleRate * channels * bytesPerSample(bitDepth);
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(byteRate));

         ushort blockAlign = Convert.ToUInt16(channels * bytesPerSample(bitDepth));
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(blockAlign));

         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(bitDepth));

         // Validate format
         Debug.AssertFormat(count == total, "Unexpected wav fmt byte count: {0} == {1}", count, total);

         return count;
      }

      private static int writeFileData(MemoryStream stream, AudioClip audioClip)
      {
         int count = 0;
         int total = 8;

         // Copy float[] data from AudioClip
         float[] data = new float[audioClip.samples * audioClip.channels];
         audioClip.GetData(data, 0);

         byte[] bytes = convertAudioClipDataToInt16ByteArray(data);

         byte[] id = Encoding.ASCII.GetBytes("data");
         count += writeBytesToMemoryStream(stream, id);

         int subchunk2Size = Convert.ToInt32(audioClip.samples * blockSize_16Bit); // BlockSize (bitDepth)
         count += writeBytesToMemoryStream(stream, BitConverter.GetBytes(subchunk2Size));

         // Validate header
         Debug.AssertFormat(count == total, "Unexpected wav data id byte count: {0} == {1}", count, total);

         // Write bytes to stream
         count += writeBytesToMemoryStream(stream, bytes);

         // Validate audio data
         Debug.AssertFormat(bytes.Length == subchunk2Size, "Unexpected AudioClip to wav subchunk2 size: {0} == {1}", bytes.Length, subchunk2Size);

         return count;
      }

      private static byte[] convertAudioClipDataToInt16ByteArray(float[] data)
      {
         byte[] bytes = null;

         using (MemoryStream dataStream = new MemoryStream())
         {
            int x = sizeof(Int16);

            Int16 maxValue = Int16.MaxValue;

            foreach (float d in data)
            {
               dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(d * maxValue)), 0, x);
            }

            bytes = dataStream.ToArray();

            // Validate converted bytes
            Debug.AssertFormat(data.Length * x == bytes.Length, "Unexpected float[] to Int16 to byte[] size: {0} == {1}", data.Length * x, bytes.Length);
         }

         return bytes;
      }

      private static int writeBytesToMemoryStream(MemoryStream stream, byte[] bytes)
      {
         int count = bytes.Length;
         stream.Write(bytes, 0, count);
         //Debug.LogFormat ("WAV:{0} wrote {1} bytes.", tag, count);
         return count;
      }

      private static int bytesPerSample(ushort bitDepth)
      {
         return bitDepth / 8;
      }

      private static int BlockSize(ushort bitDepth)
      {
         switch (bitDepth)
         {
            case 32:
               return sizeof(Int32); // 32-bit -> 4 bytes (Int32)
            case 16:
               return sizeof(Int16); // 16-bit -> 2 bytes (Int16)
            case 8:
               return sizeof(sbyte); // 8-bit -> 1 byte (sbyte)
            default:
               throw new Exception(bitDepth + " bit depth is not supported.");
         }
      }

      private static string formatCode(ushort code)
      {
         switch (code)
         {
            case 1:
               return "PCM";
            case 2:
               return "ADPCM";
            case 3:
               return "IEEE";
            case 7:
               return "μ-law";
            case 65534:
               return "WaveFormatExtendable";
            default:
               Debug.LogWarning("Unknown wav code format:" + code);
               return string.Empty;
         }
      }

      #endregion
   }
}
// © 2018-2021 crosstales LLC (https://www.crosstales.com)