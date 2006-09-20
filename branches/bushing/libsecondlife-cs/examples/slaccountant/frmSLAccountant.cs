/*
 * Copyright (c) 2006, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using libsecondlife;

namespace SLAccountant
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class frmSLAccountant : System.Windows.Forms.Form
	{
		private System.Windows.Forms.GroupBox grpLogin;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.TextBox txtLastName;
		private System.Windows.Forms.Button cmdConnect;
		private System.Windows.Forms.TextBox txtFirstName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label lblName;
		private System.Windows.Forms.Label lblBalance;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtFind;
		private System.Windows.Forms.Button cmdFind;
		private System.Windows.Forms.TextBox txtTransfer;
		private System.Windows.Forms.Button cmdTransfer;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ListView lstFind;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colOnline;
		private System.Windows.Forms.ColumnHeader colUuid;

		// libsecondlife instance
		private SecondLife client;
		// Mutex for locking the listview
		Mutex lstFindMutex;

		public frmSLAccountant()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			client.Network.Logout();
			
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.grpLogin = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtLastName = new System.Windows.Forms.TextBox();
			this.cmdConnect = new System.Windows.Forms.Button();
			this.txtFirstName = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.lblName = new System.Windows.Forms.Label();
			this.lblBalance = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.txtFind = new System.Windows.Forms.TextBox();
			this.cmdFind = new System.Windows.Forms.Button();
			this.txtTransfer = new System.Windows.Forms.TextBox();
			this.cmdTransfer = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.lstFind = new System.Windows.Forms.ListView();
			this.colName = new System.Windows.Forms.ColumnHeader();
			this.colOnline = new System.Windows.Forms.ColumnHeader();
			this.colUuid = new System.Windows.Forms.ColumnHeader();
			this.grpLogin.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpLogin
			// 
			this.grpLogin.Controls.Add(this.label3);
			this.grpLogin.Controls.Add(this.label2);
			this.grpLogin.Controls.Add(this.label1);
			this.grpLogin.Controls.Add(this.txtPassword);
			this.grpLogin.Controls.Add(this.txtLastName);
			this.grpLogin.Controls.Add(this.cmdConnect);
			this.grpLogin.Controls.Add(this.txtFirstName);
			this.grpLogin.Enabled = false;
			this.grpLogin.Location = new System.Drawing.Point(16, 344);
			this.grpLogin.Name = "grpLogin";
			this.grpLogin.Size = new System.Drawing.Size(560, 80);
			this.grpLogin.TabIndex = 50;
			this.grpLogin.TabStop = false;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(280, 24);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(120, 16);
			this.label3.TabIndex = 50;
			this.label3.Text = "Password";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(152, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(120, 16);
			this.label2.TabIndex = 50;
			this.label2.Text = "Last Name";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 16);
			this.label1.TabIndex = 50;
			this.label1.Text = "First Name";
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(280, 40);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(120, 20);
			this.txtPassword.TabIndex = 2;
			this.txtPassword.Text = "";
			// 
			// txtLastName
			// 
			this.txtLastName.Location = new System.Drawing.Point(152, 40);
			this.txtLastName.Name = "txtLastName";
			this.txtLastName.Size = new System.Drawing.Size(112, 20);
			this.txtLastName.TabIndex = 1;
			this.txtLastName.Text = "";
			// 
			// cmdConnect
			// 
			this.cmdConnect.Location = new System.Drawing.Point(424, 40);
			this.cmdConnect.Name = "cmdConnect";
			this.cmdConnect.Size = new System.Drawing.Size(120, 24);
			this.cmdConnect.TabIndex = 3;
			this.cmdConnect.Text = "Connect";
			this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
			// 
			// txtFirstName
			// 
			this.txtFirstName.Location = new System.Drawing.Point(16, 40);
			this.txtFirstName.Name = "txtFirstName";
			this.txtFirstName.Size = new System.Drawing.Size(120, 20);
			this.txtFirstName.TabIndex = 0;
			this.txtFirstName.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 8);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(48, 16);
			this.label4.TabIndex = 50;
			this.label4.Text = "Name:";
			// 
			// lblName
			// 
			this.lblName.Location = new System.Drawing.Point(64, 8);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(184, 16);
			this.lblName.TabIndex = 50;
			// 
			// lblBalance
			// 
			this.lblBalance.Location = new System.Drawing.Point(512, 8);
			this.lblBalance.Name = "lblBalance";
			this.lblBalance.Size = new System.Drawing.Size(64, 16);
			this.lblBalance.TabIndex = 50;
			this.lblBalance.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(456, 8);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(56, 16);
			this.label6.TabIndex = 50;
			this.label6.Text = "Balance:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(16, 40);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(88, 16);
			this.label5.TabIndex = 50;
			this.label5.Text = "People Search";
			// 
			// txtFind
			// 
			this.txtFind.Enabled = false;
			this.txtFind.Location = new System.Drawing.Point(16, 56);
			this.txtFind.Name = "txtFind";
			this.txtFind.Size = new System.Drawing.Size(184, 20);
			this.txtFind.TabIndex = 4;
			this.txtFind.Text = "";
			// 
			// cmdFind
			// 
			this.cmdFind.Enabled = false;
			this.cmdFind.Location = new System.Drawing.Point(208, 56);
			this.cmdFind.Name = "cmdFind";
			this.cmdFind.Size = new System.Drawing.Size(48, 24);
			this.cmdFind.TabIndex = 5;
			this.cmdFind.Text = "Find";
			this.cmdFind.Click += new System.EventHandler(this.cmdFind_Click);
			// 
			// txtTransfer
			// 
			this.txtTransfer.Enabled = false;
			this.txtTransfer.Location = new System.Drawing.Point(360, 192);
			this.txtTransfer.MaxLength = 7;
			this.txtTransfer.Name = "txtTransfer";
			this.txtTransfer.Size = new System.Drawing.Size(104, 20);
			this.txtTransfer.TabIndex = 7;
			this.txtTransfer.Text = "";
			// 
			// cmdTransfer
			// 
			this.cmdTransfer.Enabled = false;
			this.cmdTransfer.Location = new System.Drawing.Point(472, 192);
			this.cmdTransfer.Name = "cmdTransfer";
			this.cmdTransfer.Size = new System.Drawing.Size(104, 24);
			this.cmdTransfer.TabIndex = 8;
			this.cmdTransfer.Text = "Transfer Lindens";
			this.cmdTransfer.Click += new System.EventHandler(this.cmdTransfer_Click);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(360, 176);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(88, 16);
			this.label7.TabIndex = 17;
			this.label7.Text = "Amount:";
			// 
			// lstFind
			// 
			this.lstFind.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.lstFind.AllowColumnReorder = true;
			this.lstFind.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																					  this.colName,
																					  this.colOnline,
																					  this.colUuid});
			this.lstFind.FullRowSelect = true;
			this.lstFind.HideSelection = false;
			this.lstFind.Location = new System.Drawing.Point(16, 88);
			this.lstFind.Name = "lstFind";
			this.lstFind.Size = new System.Drawing.Size(336, 248);
			this.lstFind.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lstFind.TabIndex = 6;
			this.lstFind.View = System.Windows.Forms.View.Details;
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 120;
			// 
			// colOnline
			// 
			this.colOnline.Text = "Online";
			this.colOnline.Width = 50;
			// 
			// colUuid
			// 
			this.colUuid.Text = "UUID";
			this.colUuid.Width = 150;
			// 
			// frmSLAccountant
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(592, 437);
			this.Controls.Add(this.lstFind);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.cmdTransfer);
			this.Controls.Add(this.txtTransfer);
			this.Controls.Add(this.txtFind);
			this.Controls.Add(this.cmdFind);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.lblBalance);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.grpLogin);
			this.Name = "frmSLAccountant";
			this.Text = "SL Accountant";
			this.Load += new System.EventHandler(this.frmSLAccountant_Load);
			this.grpLogin.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new frmSLAccountant());
		}

		private void BalanceHandler(Packet packet, Simulator simulator)
		{
			if (packet.Layout.Name == "MoneyBalanceReply")
			{
				int balance = 0;
				int squareMetersCredit = 0;
				string description = "";
				LLUUID transactionID = null;
				bool transactionSuccess = false;

				foreach (Block block in packet.Blocks())
				{
					foreach (Field field in block.Fields)
					{
						if (field.Layout.Name == "MoneyBalance")
						{
							balance = (int)field.Data;
						}
						else if (field.Layout.Name == "SquareMetersCredit")
						{
							squareMetersCredit = (int)field.Data;
						}
						else if (field.Layout.Name == "Description")
						{
							byte[] byteArray = (byte[])field.Data;
							description = System.Text.Encoding.ASCII.GetString(byteArray).Replace("\0", "");
						}
						else if (field.Layout.Name == "TransactionID")
						{
							transactionID = (LLUUID)field.Data;
						}
						else if (field.Layout.Name == "TransactionSuccess")
						{
							transactionSuccess = (bool)field.Data;
						}
					}
				}

				lblBalance.Text = balance.ToString();
			}
		}

		private void DirPeopleHandler(Packet packet, Simulator simulator)
		{
			lstFindMutex.WaitOne();

			foreach (Block block in packet.Blocks())
			{
				if (block.Layout.Name == "QueryReplies")
				{
					LLUUID id = null;
					string firstName = "";
					string lastName = "";
					bool online = false;
					
					foreach (Field field in block.Fields)
					{
						if (field.Layout.Name == "AgentID")
						{
							id = (LLUUID)field.Data;
						}
						else if (field.Layout.Name == "LastName")
						{
							lastName = System.Text.Encoding.UTF8.GetString((byte[])field.Data).Replace("\0", "");
						}
						else if (field.Layout.Name == "FirstName")
						{
							firstName = System.Text.Encoding.UTF8.GetString((byte[])field.Data).Replace("\0", "");
						}
						else if (field.Layout.Name == "Online")
						{
							online = (bool)field.Data;
						}
					}

					if (id != null)
					{
						ListViewItem listItem = new ListViewItem(new string[] 
						{ firstName + " " + lastName, (online ? "Yes" : "No"), id.ToString() });
						lstFind.Items.Add(listItem);
					}
				}
			}

			lstFindMutex.ReleaseMutex();
		}

		private void AvatarAppearanceHandler(Packet packet, Simulator simulator)
		{
			LLUUID id = null;
			bool trial = false;

			foreach (Block block in packet.Blocks())
			{
				foreach (Field field in block.Fields)
				{
					if (field.Layout.Name == "ID")
					{
						id = (LLUUID)field.Data;
					}
					else if (field.Layout.Name == "IsTrial")
					{
						trial = (bool)field.Data;
					}
				}
			}

			//txtLog.AppendText("AvatarAppearance: " + id.ToString() + " (Trial: " + ((trial) ? "Yes" : "No") + ")\n");
		}

		private void frmSLAccountant_Load(object sender, System.EventArgs e)
		{
			lstFindMutex = new Mutex(false, "lstFindMutex");

			try
			{
				client = new SecondLife("keywords.txt", "protocol.txt");

				// Install our packet handlers
				client.Network.RegisterCallback("AvatarAppearance", new PacketCallback(AvatarAppearanceHandler));
				client.Network.RegisterCallback("MoneyBalanceReply", new PacketCallback(BalanceHandler));
				client.Network.RegisterCallback("DirPeopleReply", new PacketCallback(DirPeopleHandler));

				grpLogin.Enabled = true;
			}
			catch (Exception error)
			{
				MessageBox.Show(this, error.ToString());
			}
		}

		private void cmdConnect_Click(object sender, System.EventArgs e)
		{
			if (cmdConnect.Text == "Connect")
			{
				cmdConnect.Text = "Disconnect";
				txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = false;

				Hashtable loginParams = NetworkManager.DefaultLoginValues(txtFirstName.Text, 
					txtLastName.Text, txtPassword.Text, "00:00:00:00:00:00", "last", 1, 50, 50, 50, 
					"Win", "0", "accountant", "jhurliman@wsu.edu");

				if (client.Network.Login(loginParams))
				{
					Random rand = new Random();
					
					lblName.Text = client.Network.LoginValues["first_name"] + " " + 
						client.Network.LoginValues["last_name"];

					// AgentHeightWidth
					Hashtable blocks = new Hashtable();
					Hashtable fields = new Hashtable();
					fields["ID"] = client.Network.AgentID;
					fields["GenCounter"] = (uint)0;
					fields["CircuitCode"] = client.Network.CurrentSim.CircuitCode;
					blocks[fields] = "Sender";
					fields = new Hashtable();
					fields["Height"] = (ushort)rand.Next(0, 65535);
					fields["Width"] = (ushort)rand.Next(0, 65535);
					blocks[fields] = "HeightWidthBlock";
					Packet packet = PacketBuilder.BuildPacket("AgentHeightWidth", client.Protocol, blocks,
						Helpers.MSG_RELIABLE + Helpers.MSG_ZEROCODED);

					client.Network.SendPacket(packet);

					// ConnectAgentToUserserver
					blocks = new Hashtable();
					fields = new Hashtable();
					fields["AgentID"] = client.Network.AgentID;
					fields["SessionID"] = client.Network.SessionID;
					blocks[fields] = "AgentData";
					packet = PacketBuilder.BuildPacket("ConnectAgentToUserserver", client.Protocol, blocks,
						Helpers.MSG_RELIABLE + Helpers.MSG_ZEROCODED);

					client.Network.SendPacket(packet);

					// MoneyBalanceRequest
					blocks = new Hashtable();
					fields = new Hashtable();
					fields["AgentID"] = client.Network.AgentID;
					fields["TransactionID"] = LLUUID.GenerateUUID();
					blocks[fields] = "MoneyData";
					packet = PacketBuilder.BuildPacket("MoneyBalanceRequest", client.Protocol, blocks,
						Helpers.MSG_RELIABLE);

					client.Network.SendPacket(packet);

					// AgentSetAppearance
					blocks = new Hashtable();
					// Setup some random appearance values
					for (int i = 0; i < 218; ++i)
					{
						fields = new Hashtable();
						fields["ParamValue"] = (byte)rand.Next(255);
						blocks[fields] = "VisualParam";
					}
					fields = new Hashtable();
					byte[] byteArray = new byte[400];
					fields["TextureEntry"] = byteArray;
					blocks[fields] = "ObjectData";
					fields = new Hashtable();
					fields["SerialNum"] = (uint)1;
					fields["ID"] = client.Network.AgentID;
					// Setup a random avatar size
					LLVector3 sizeVector = new LLVector3(0.45F, 0.6F, 1.831094F);
					fields["Size"] = sizeVector;
					blocks[fields] = "Sender";
					packet = PacketBuilder.BuildPacket("AgentSetAppearance", client.Protocol, blocks,
						Helpers.MSG_RELIABLE);

					client.Network.SendPacket(packet);

					txtFind.Enabled = cmdFind.Enabled = true;
					txtTransfer.Enabled = cmdTransfer.Enabled = true;
				}
				else
				{
					MessageBox.Show(this, "Error logging in: " + client.Network.LoginError);
					cmdConnect.Text = "Connect";
					txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = true;
					txtFind.Enabled = cmdFind.Enabled = false;
					lblName.Text = lblBalance.Text = "";
					txtTransfer.Enabled = cmdTransfer.Enabled = false;
				}
			}
			else
			{
				client.Network.Logout();
				cmdConnect.Text = "Connect";
				txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = true;
				txtFind.Enabled = cmdFind.Enabled = false;
				lblName.Text = lblBalance.Text = "";
				txtTransfer.Enabled = cmdTransfer.Enabled = false;
			}
		}

		private void cmdFind_Click(object sender, System.EventArgs e)
		{
			lstFind.Items.Clear();

			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();
			fields["QueryID"] = LLUUID.GenerateUUID();
			fields["QueryFlags"] = (uint)1;
			fields["QueryStart"] = (int)0;
			fields["QueryText"] = txtFind.Text;
			blocks[fields] = "QueryData";

			fields = new Hashtable();
			fields["AgentID"] = client.Network.AgentID;
			fields["SessionID"] = client.Network.SessionID;
			blocks[fields] = "AgentData";

			Packet packet = PacketBuilder.BuildPacket("DirFindQuery", client.Protocol, blocks,
				Helpers.MSG_RELIABLE);

			client.Network.SendPacket(packet);
		}

		private void cmdTransfer_Click(object sender, System.EventArgs e)
		{
			int amount = 0;

			try
			{
				amount = System.Convert.ToInt32(txtTransfer.Text);
			}
			catch (Exception)
			{
				MessageBox.Show(txtTransfer.Text + " is not a valid amount");
				return;
			}

			if (lstFind.SelectedItems.Count != 1)
			{
				MessageBox.Show("Find an avatar using the directory search and select " + 
					"their name to transfer money");
				return;
			}
			
			client.Avatar.GiveMoney(new LLUUID(lstFind.SelectedItems[0].SubItems[2].Text),
			                        amount, "SLAccountant payment");
		}
	}
}