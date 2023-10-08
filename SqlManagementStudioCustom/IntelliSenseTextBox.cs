using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.Linq;

public class IntelliSenseRichTextBox : RichTextBox
{
    private ListBox listBox;
    private List<string> dictionary;

    public IntelliSenseRichTextBox()
    {
        listBox = new ListBox();
        listBox.Visible = false;
        dictionary = new List<string> { "create table", "take", "add", "where" };
        this.Controls.Add(listBox);
        this.TextChanged += IntelliSenseRichTextBox_TextChanged;
    }

    private void IntelliSenseRichTextBox_TextChanged(object sender, EventArgs e)
    {
        string word = this.Text.Split(' ').Last();

        if (string.IsNullOrEmpty(word))
        {
            listBox.Visible = false;
            return;
        }

        var matches = dictionary.Where(item => item.StartsWith(word, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matches.Count > 0)
        {
            listBox.DataSource = matches;
            listBox.Visible = true;
            listBox.BringToFront();
        }
        else
        {
            listBox.Visible = false;
        }
    }
}