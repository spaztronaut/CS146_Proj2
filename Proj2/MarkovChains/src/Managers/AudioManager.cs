﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MarkovChains.src.Managers;
using MarkovChains.Audio;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.IO;
using System.Threading;
using Tao.Sdl;

namespace MarkovChains.Managers
{
    unsafe public class AudioManager : IManager
    {
        private static AudioManager _instance;

        //OpenAL audio members

        private AudioContext _actx;
        private XRamExtension _xram;

        public XRamExtension XRam
        {
            get { return _xram; }
        }

        /// <summary>
        /// Singleton instance of class.
        /// </summary>
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AudioManager();

                return _instance;
            }
        }

        private Dictionary<string, SoundEffect> _sounds;
        private Queue<string> _soundsToPlay;

        public AudioManager()
        {
            Initialize();
        }

        public void Initialize()
        {
            _sounds = new Dictionary<string, SoundEffect>();
            _soundsToPlay = new Queue<string>();

            //LoadOpenAL();

            LoadContent();
        }


        int sourceid;
        int bufferid;
        int state;

        private void LoadOpenAL()
        {
            _actx = new AudioContext();

            bufferid = AL.GenBuffer();
            sourceid = AL.GenSource();


            int channels, bps, sampleRate;

            byte[] data = loadWav("Content\\Audio\\MarkovSamples\\waves\\FancyPants", out channels, out sampleRate, out bps);

            AL.BufferData(bufferid, GetSoundFormat(channels, bps), data, data.Length, sampleRate);
            
            Console.WriteLine("Playing");

            Thread t = new Thread(() =>
            {
                do
                {

                    AL.Source(sourceid, ALSourcei.Buffer, bufferid);
                    AL.SourcePlay(sourceid);

                    Thread.Sleep(5000);

                    Console.Write(".");
                    AL.GetSource(sourceid, ALGetSourcei.SourceState, out state);
                } while ((ALSourceState)state == ALSourceState.Playing);

                AL.SourceStop(sourceid);
                AL.DeleteSource(sourceid);
                AL.DeleteBuffer(bufferid);
            });

            t.Start();
            
        }

        public void LoadContent()
        {
            SoundEffect sound1 = Game1.Instance.Content.Load<SoundEffect>(@"Audio\MarkovSamples\waves\bass_g");
            _sounds.Add("bass_g", sound1);
            SoundEffect sound2 = Game1.Instance.Content.Load<SoundEffect>(@"Audio\MarkovSamples\waves\bass_f");
            _sounds.Add("bass_f", sound2);
            SoundEffect sound3 = Game1.Instance.Content.Load<SoundEffect>(@"Audio\MarkovSamples\waves\bass_c");
            _sounds.Add("bass_c", sound3);

            SampleAudioFile("Content\\Audio\\MarkovSamples\\testsample.txt", 2);
        }

        public void UnloadContent()
        {
            //int buffers = 1;
            //AL.DeleteBuffers(0, ref buffers);
            //_ac.Dispose();
        }
        int count = 0;
        public void Update(GameTime gameTime)
        {
            while (_soundsToPlay.Count > 0)
            {
                _sounds[_soundsToPlay.Dequeue()].Play();
            }
        }

        public void PlaySound(string _name)
        {
            if (_sounds.ContainsKey(_name))
                _soundsToPlay.Enqueue(_name);
        }

        public void Draw(SpriteBatch spriteBatch)
        {

        }

        public void LoadFromXML(string path)
        {

        }

        public MarkovMatrix<string, char> SampleAudioFile(string path, int markovOrder)
        {
            List<string> noteChains = new List<string>();
            List<char> notes = new List<char>();

            Stream stream = File.Open(path, FileMode.Open);

            if (stream == null)
                throw new FileNotFoundException();

            char[] buffer = new char[markovOrder + 1];
            
            using (StreamReader reader = new StreamReader(stream))
            {
                int numToRead = buffer.Length - 1;
                //this reads in the "prev" notes of the chain
                reader.Read(buffer, 0, numToRead);
                string chain = null;

                while (!reader.EndOfStream)
                {
                    reader.Read(buffer, buffer.Length - 1, 1);

                    //get the note chain
                    chain = new string(buffer, 0, buffer.Length - 1);

                    //check to see if it is contained in our note chain list
                    if (!noteChains.Contains(chain))
                        noteChains.Add(chain);

                    //set previous notes. essentially shift the characters in the buffer by 1
                    for (int i = 1; i < buffer.Length; ++i)
                    {
                        if(!notes.Contains(buffer[i - 1]))
                            notes.Add(buffer[i - 1]);

                        buffer[i - 1] = buffer[i];
                    }

                    if (!notes.Contains(buffer[buffer.Length - 1]))
                        notes.Add(buffer[buffer.Length - 1]);
                }

                //grab the last chain and check to see if it is in the list
                chain = new string(buffer, 0, markovOrder);
                if (!noteChains.Contains(chain))
                    noteChains.Add(chain);

            }

            //create the markov matrix
            return new MarkovMatrix<string, char>(noteChains, notes, path, markovOrder);
        }

        public byte[] loadWav(string file, out int channels, out int sampleRate, out int bps/*, out int size*/)
        {
            Stream stream = File.Open(file + ".wav", FileMode.Open);

            if (stream == null)
                throw new ArgumentNullException();

            byte[] data = null;

            using (BinaryReader reader = new BinaryReader(stream))
            {
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                int riff_chunck_size = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                // WAVE header
                string format_signature = new string(reader.ReadChars(4));
                if (format_signature != "fmt ")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();
                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();
                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();

                //here we may encoutner additional subchunks. We only care about
                //the data subchunk for now.

                string data_signature = null;
                int data_chunk_size = 0;

                while (data_signature != "data")
                {
                    data_signature = new string(reader.ReadChars(4));
                    //if (data_signature != "data")
                    //    throw new NotSupportedException("Specified wave file is not supported.");

                }
                data_chunk_size = reader.ReadInt32() + 10000;

                channels = num_channels;
                bps = bits_per_sample;
                sampleRate = sample_rate;

                data = reader.ReadBytes((int)reader.BaseStream.Length);
            }

            stream.Close();

            return data;
        }

        public static ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }
    }
}
