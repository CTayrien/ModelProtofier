using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
//using System.Collections;

namespace ModelProcessor
{
    public partial class Form1 : Form
    {
        struct vec2
        {
            public float x, y;
        }
        struct vec3
        {
            public float x, y, z;
        }
        struct Vertex {
            public vec3 loc;
            public vec2 uv;
            public vec3 norm;
        }
        struct VertInd
        {
            public uint locInd, uvInd, normInd;
        }

        List<Vertex> vertBufData = new List<Vertex>();
        float maxx = 0, maxy = 0, maxz = 0;
        uint nverts = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // disable the buttons
            button1.Enabled = false;
            button2.Enabled = false;

            // Open file dialog
            OpenFileDialog open = new OpenFileDialog();
            open.ShowDialog();
            try
            {
                // assume texture-mapped, normal mapped and triangulated faces
                // try to import obj file data into buffer

                // PROCESS
                // Get unique vertex locs, uvs and norms
                List<vec3> locs = new List<vec3>();
                List<vec2> uvs = new List<vec2>();
                List<vec3> norms = new List<vec3>();

                // Get indices of locations, uvs and normals for stitching
                List<VertInd> vertInds = new List<VertInd>();

                // The following will be parsed from the file...
                //Vertex vertex;
                //VertInd vertInd;

                // Get all lines from the file, one at a time.
                StreamReader modelstream = new StreamReader(open.FileName);
                string lineString;
                while ((lineString = modelstream.ReadLine()) != null)
                {
                    char[] delims = { ' ', '/' };
                    string[] words = lineString.Split(delims);

                    // Parse label
                    string label = words[0];

                    // Parse and add a loc
                    if (label == "v")
                    {
                        vec3 loc = new vec3();
                        float.TryParse(words[1], out loc.x);
                        float.TryParse(words[2], out loc.y);
                        float.TryParse(words[3], out loc.z);
                        locs.Add(loc);
                    }
                    // Parse and add a uv
                    if (label == "vt")
                    {
                        vec2 uv = new vec2();
                        float.TryParse(words[1], out uv.x);
                        float.TryParse(words[2], out uv.y);
                        uvs.Add(uv);
                    }
                    // Parse and add a norm
                    if (label == "vn")
                    {
                        vec3 norm = new vec3();
                        float.TryParse(words[1], out norm.x);
                        float.TryParse(words[2], out norm.y);
                        float.TryParse(words[3], out norm.z);
                        norms.Add(norm);
                    }
                    // Parse and add 3 complex vertInds
                    if (label == "f")
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            VertInd vertInd = new VertInd();
                            UInt32.TryParse(words[1 + i * 3], out vertInd.locInd); vertInd.locInd--;
                            UInt32.TryParse(words[2 + i * 3], out vertInd.uvInd); vertInd.uvInd--;
                            UInt32.TryParse(words[3 + i * 3], out vertInd.normInd); vertInd.normInd--;
                            vertInds.Add(vertInd);
                        }
                    }
                }
                
                modelstream.Close();

                // Recenter model
                vec3 mina = new vec3();
                vec3 maxa = new vec3();
                vec3 c = new vec3();

                mina.x = maxa.x = locs[0].x;
                mina.y = maxa.y = locs[0].y;
                mina.z = maxa.z = locs[0].z;

                for (int i = 1; i < locs.Count; i++)
                {
                    if (locs[i].x < mina.x) mina.x = locs[i].x;
                    if (locs[i].y < mina.y) mina.y = locs[i].y;
                    if (locs[i].z < mina.z) mina.z = locs[i].z;

                    if (locs[i].x > maxa.x) maxa.x = locs[i].x;
                    if (locs[i].y > maxa.y) maxa.y = locs[i].y;
                    if (locs[i].z > maxa.z) maxa.z = locs[i].z;
                }

                c.x = (mina.x + maxa.x) / 2;
                c.y = (mina.y + maxa.y) / 2;
                c.z = (mina.z + maxa.z) / 2;

                List<vec3> locsNew = new List<vec3>();
                for (int i = 0; i < locs.Count; i++)
                {
                    vec3 loc = new vec3();
                    loc.x = locs[i].x - c.x;
                    loc.y = locs[i].y - c.y;
                    loc.z = locs[i].z - c.z;
                    locsNew.Add(loc);
                }
                locs = locsNew;

                // Get max extremeties (needed for collision checks)
                for (int i = 0; i < locs.Count; i++)
                {
                    vec3 abs = new vec3();
                    abs.x = Math.Abs(locs[i].x);
                    abs.y = Math.Abs(locs[i].y);
                    abs.z = Math.Abs(locs[i].z);

                    //if (r > maxr) maxr = r;
                    if (abs.x > maxx) maxx = abs.x;
                    if (abs.y > maxy) maxy = abs.y;
                    if (abs.z > maxz) maxz = abs.z;
                }

                // Stitch together data for vertex buffer
                nverts = (uint)vertInds.Count;
                //vertBufData = new List<Vertex>((int)nverts);
                for (int i = 0; i < nverts; i++)
                {
                    Vertex vertex = new Vertex();

                    vertex.loc = locs[(int)vertInds[i].locInd];
                    vertex.uv = uvs[(int)vertInds[i].uvInd];
                    vertex.norm = norms[(int)vertInds[i].normInd];

                    vertBufData.Add(vertex);
                }

                textBox1.Text = maxx.ToString();
                textBox2.Text = maxy.ToString();
                textBox3.Text = maxz.ToString();
                textBox4.Text = nverts.ToString();

                // popup with success or failure
                MessageBox.Show("Importing completed");
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                MessageBox.Show("Error reading file");
            }
            
            // enable the buttons
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // disable the buttons
            button1.Enabled = false;
            button2.Enabled = false;

            // Save file dialog
            SaveFileDialog save = new SaveFileDialog();
            save.DefaultExt = ".dat";
            save.ShowDialog();
            try
            {
                //assemble byte array
                int nbytes = 16 + 32 * (int)nverts;
                byte[] data = new byte[nbytes];
                data[0] = (byte)nbytes;

                System.IO.Stream modelstream = save.OpenFile();
                BinaryWriter bw = new BinaryWriter(modelstream);

                // try to write buffer data into file
                // write 4 byte unsigned int (long long) number of bytes in buffer
                bw.Write(maxx);
                bw.Write(maxy);
                bw.Write(maxz);
                bw.Write(nverts);

                for (int i=0; i < nverts; i++)
                {
                    bw.Write(vertBufData[i].loc.x);
                    bw.Write(vertBufData[i].loc.y);
                    bw.Write(vertBufData[i].loc.z);

                    bw.Write(vertBufData[i].uv.x);
                    bw.Write(vertBufData[i].uv.y);

                    bw.Write(vertBufData[i].norm.x);
                    bw.Write(vertBufData[i].norm.y);
                    bw.Write(vertBufData[i].norm.z);
                }

                modelstream.Close();
                
                // popup with success or failure
                MessageBox.Show("Exporting completed");
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                MessageBox.Show("Error writing file");
            }

            vertBufData = new List<Vertex>();
            maxx = maxy = maxz = 0;
            nverts = 0;

            textBox1.Text = maxx.ToString();
            textBox2.Text = maxy.ToString();
            textBox3.Text = maxz.ToString();
            textBox4.Text = nverts.ToString();

            // enable the buttons
            button1.Enabled = true;
            button2.Enabled = true;
        }
    }
}
