using System;
using CoreUtilities;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using CoreUtilities.Links;
using System.Data;
using Layout;



namespace ADD_Checklist
{
	public class NoteDataXML_Checklist: NoteDataXML_Table
	{
		#region constants
		public const string tableID="Roll";
		public const string tableCheckLabel="Result";
		public const string tableTrueFalse = "Next Table";	
	//	public const string NotUsed = "Modifier";
		#endregion
		#region interface
		CheckedListBox checkers = null;
		TextBox preview = null;
		#endregion

		#region properties

		string notelink = Constants.BLANK;
		
		public string Notelink {
			get {
				return notelink;
			}
			set {
				notelink = value;
			}
		}
		#endregion


		public NoteDataXML_Checklist () : base()
		{
			CommonConstructorBehavior ();
		}
		public NoteDataXML_Checklist(int height, int width):base(height, width)
		{
			CommonConstructorBehavior ();
			ReadOnly = true;
		}
		public NoteDataXML_Checklist(NoteDataInterface Note) : base(Note)
		{
			this.Notelink = ((NoteDataXML_Checklist)Note).Notelink;
		}
		protected override void CommonConstructorBehavior ()
		{
			base.CommonConstructorBehavior ();
			Caption = Loc.Instance.GetString("Check List");

		}
	
		/// <summary>
		/// Registers the type.
		/// </summary>
		public override string RegisterType()
		{
			return Loc.Instance.GetString("Check List");
		}

		void UpdateCheckpage (CheckedListBox checkers)
		{
			checkers.Items.Clear();
			foreach (DataRow row in dataSource.Rows) {
				bool ischecked = false;
				if (row[tableTrueFalse].ToString() == "1")
				{
					ischecked = true;
				}
				checkers.Items.Add (row[tableCheckLabel].ToString(), ischecked);
				
			}
		}

		protected override void DoBuildChildren (LayoutPanelBase Layout)
		{
			base.DoBuildChildren (Layout);
			try {
				TabControl pages = new TabControl ();

				ParentNotePanel.Controls.Add (pages);
				pages.Dock = DockStyle.Fill;
				pages.BringToFront();

				TabPage GridPage = new TabPage (Loc.Instance.GetString ("Advanced"));
				TabPage CheckPage = new TabPage (Loc.Instance.GetString ("Checklist"));
				pages.TabPages.Add (CheckPage);
				pages.TabPages.Add (GridPage);


				ParentNotePanel.Controls.Remove (this.Table);
				GridPage.Controls.Add (this.Table);

				// Set up check list
				checkers = new CheckedListBox ();
				checkers.Dock = DockStyle.Fill;

			
				UpdateCheckpage (checkers);
				checkers.ItemCheck+= (object sender, ItemCheckEventArgs e) => SetSaveRequired(true);
				checkers.SelectedIndexChanged+= HandleSelectedIndexChanged;



				preview = new TextBox();
				preview.Dock = DockStyle.Bottom;
				preview.Height = 75;
				preview.Multiline = true;
				preview.ReadOnly = true;
				preview.ScrollBars = ScrollBars.Both;
					CheckPage.Controls.Add (checkers);
				CheckPage.Controls.Add (preview);


			} catch (Exception ex) {
				NewMessage.Show (ex.ToString ());
			}

			ToolStripMenuItem LinkedNote = 
				LayoutDetails.BuildMenuPropertyEdit (Loc.Instance.GetString("Linked Note: {0}"), 
				                                     Notelink,
				                                     Loc.Instance.GetString ("Give a valid note's name to populate check list."),HandleNoteLinkNameChange );


			ToolStripButton RefreshButton = new ToolStripButton();
			RefreshButton.Text = Loc.Instance.GetString("Refresh");
			RefreshButton.Click+= HandleRefreshButtonClick;


			properties.DropDownItems.Add (new ToolStripSeparator());
			properties.DropDownItems.Add (LinkedNote);
			properties.DropDownItems.Add (RefreshButton);

		}
		protected override void DoChildAppearance (AppearanceClass app)
		{
			base.DoChildAppearance (app);

			checkers.BackColor = app.mainBackground;
			checkers.ForeColor = app.secondaryForeground;

			preview.BackColor = app.mainBackground;
			preview.ForeColor = app.secondaryForeground;
		}
		void HandleSelectedIndexChanged (object sender, EventArgs e)
		{
			if (checkers.SelectedItem != null) {
				//NewMessage.Show (checkers.ToString ());
				preview.Text = checkers.SelectedItem.ToString ();
			}
		}

		bool IsTextAlreadyInTable (string item)
		{
			bool IsThere = false;

			foreach (DataRow row in dataSource.Rows) {
				if (row[tableCheckLabel].ToString() == item)
				{
					IsThere = true;
					break;
				}
			}

			return IsThere;
		}

		/// <summary>
		/// Parses the note into table -- will added each line
		/// </summary>
		/// <param name='note'>
		/// Note.
		/// </param>
		void ParseNoteIntoTable (NoteDataInterface note)
		{

			RichTextBox tmp = new RichTextBox();
			tmp.Rtf = note.Data1;
			string Source = tmp.Text;
			tmp.Dispose();

			// assuming is not null, already tested for this
			string[] ImportedItems = Source.Split (new string[2]{"\r\n","\n"}, StringSplitOptions.RemoveEmptyEntries);
			if (ImportedItems != null && ImportedItems.Length > 0) {
				//int count = 0;
				foreach (string item in ImportedItems) {
					if (item != Constants.BLANK)
					{
					if (IsTextAlreadyInTable(item) == false)
					{
						int count = RowCount();
						this.AddRow (new object[3]{count.ToString (),item, "0"});
						//count++;
					}
					}
					// if text IS IN table, we don't do anything during a parse.
				}
			}
		}

		void HandleRefreshButtonClick (object sender, EventArgs e)
		{
			NoteDataInterface note = this.Layout.FindNoteByName (Notelink);
			if (note == null || note.Data1 == Constants.BLANK) {
				NewMessage.Show (Loc.Instance.GetStringFmt ("The note [{0}] does not exist or has no text.", Notelink));
			} else {
				// we need to force a save in case the text on the note has changed and we want it reflected in the checklist
				Layout.SaveLayout ();
				// we can parse
				ParseNoteIntoTable(note);
				UpdateCheckpage (checkers);
			}

		}



		void HandleNoteLinkNameChange (object sender, KeyEventArgs e)
		{
			string tablecaption = Notelink;
			LayoutDetails.HandleMenuLabelEdit (sender, e, ref tablecaption, SetSaveRequired);
			Notelink = tablecaption;
		}
		/// <summary>
		/// Updates the database by looking for a string matching STR and then updating the result with result
		/// </summary>
		/// <param name='str'>
		/// String.
		/// </param>
		/// <param name='result'>
		/// Result.
		/// </param>
		void UpdateDatabase (string str, int result)
		{
			for (int i = 0; i < dataSource.Rows.Count; i++)
			{
				if (dataSource.Rows[i][tableCheckLabel].ToString() == str)
				{
					dataSource.Rows[i][tableTrueFalse] = result;
					break;
				}
			}

				


		}

		public override void Save ()
		{
			// iterate through all items and test if checked and write 0 or 1
			for (int i = 0; i < checkers.Items.Count; i++)
			{
				int result = 0;
				if (checkers.GetItemChecked(i) == true)
				{
					result = 1;
				}
				// now update the database based on the text
				UpdateDatabase(checkers.Items[i].ToString (), result);
			}



			base.Save ();



		}

	}
}

