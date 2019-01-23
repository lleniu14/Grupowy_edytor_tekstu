using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ResizingAdornerSDK;

namespace RealTimeRTBEditor_Client
{
    public partial class TextEditor : Window
    {
        public TextEditor(EditorData editorData, Client _client)
        {
            _editorData = editorData;
            InitializeComponent();
            Visibility = Visibility.Visible;
            Workspace.Visibility = Visibility.Hidden;
            client = _client;
            patchingLogic = new PatchingLogic(this);

            client.ReciveDocument += ReciveDocument;
            client.UpdateRequest += UpdateRequest;
            client.AckRequest += AckRequest;
            client.ReciveList += ReciveList;
            client.ReciveSharedList += ReciveSharedList;
            client.ReciveUsersList += ReciveUsersList;

            _fontFamily.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies;
            _fontSize.ItemsSource = FontSizes;

            _fontFamily.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies;
            _fontSize.ItemsSource = FontSizes;

            _fontFamily.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies;
            _fontSize.ItemsSource = FontSizes;
        }

        #region Properties

        public Client client;
        private bool _isUpdatingEditor = false;
        private DateTime _lastUpdate;
        private DateTime _delayedUpdate;
        public PatchingLogic patchingLogic;
        public Document document;
        int deley = 150;
        public String docId;
        public bool fclicl = false;
        EditorData _editorData;
        public double[] FontSizes
        {
            get
            {
                return new double[] {
                    3.0, 4.0, 5.0, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5,
                    10.0, 10.5, 11.0, 11.5, 12.0, 12.5, 13.0, 13.5, 14.0, 15.0,
                    16.0, 17.0, 18.0, 19.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0,
                    32.0, 34.0, 36.0, 38.0, 40.0, 44.0, 48.0, 52.0, 56.0, 60.0, 64.0, 68.0, 72.0, 76.0,
                    80.0, 88.0, 96.0, 104.0, 112.0, 120.0, 128.0, 136.0, 144.0
                    };
            }
        }

        #endregion

        #region Event Handlers

        private void Workspace_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateVisualState();
            SendMessage();
        }


        private void FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                FontFamily editValue = (FontFamily)e.AddedItems[0];
                ApplyPropertyValueToSelectedText(TextElement.FontFamilyProperty, editValue);
            }
            catch (Exception)
            { }
        }

        private void FontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ApplyPropertyValueToSelectedText(TextElement.FontSizeProperty, e.AddedItems[0]);
            }
            catch (Exception)
            { }
        }

        private void btn_importimg_Click(object sender, RoutedEventArgs e)
        {
            selectImg(Workspace);
        }

        private void btn_Table_Click(object sender, RoutedEventArgs e)
        {
            Tab tab = new Tab(this);
        }


        #endregion

        #region Methods

        private void UpdateSelectedFontSize()
        {
            object value = Workspace.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            _fontSize.SelectedValue = (value == DependencyProperty.UnsetValue) ? null : value;
        }
        private void UpdateVisualState()
        {
            UpdateToggleButtonState();
            UpdateSelectionListType();
            UpdateSelectedFontFamily();
            UpdateSelectedFontSize();
        }

        private void UpdateToggleButtonState()
        {
            UpdateItemCheckedState(_btnBold, TextElement.FontWeightProperty, FontWeights.Bold);
            UpdateItemCheckedState(_btnItalic, TextElement.FontStyleProperty, FontStyles.Italic);
            UpdateItemCheckedState(_btnUnderline, Inline.TextDecorationsProperty, TextDecorations.Underline);

            UpdateItemCheckedState(_btnAlignLeft, Paragraph.TextAlignmentProperty, TextAlignment.Left);
            UpdateItemCheckedState(_btnAlignCenter, Paragraph.TextAlignmentProperty, TextAlignment.Center);
            UpdateItemCheckedState(_btnAlignRight, Paragraph.TextAlignmentProperty, TextAlignment.Right);
            UpdateItemCheckedState(_btnAlignJustify, Paragraph.TextAlignmentProperty, TextAlignment.Right);
        }

        void UpdateItemCheckedState(ToggleButton button, DependencyProperty formattingProperty, object expectedValue)
        {
            object currentValue = Workspace.Selection.GetPropertyValue(formattingProperty);
            button.IsChecked = (currentValue == DependencyProperty.UnsetValue) ? false : currentValue != null && currentValue.Equals(expectedValue);

        }
        private void UpdateSelectionListType()
        {
            Paragraph startParagraph = Workspace.Selection.Start.Paragraph;
            Paragraph endParagraph = Workspace.Selection.End.Paragraph;
            if (startParagraph != null && endParagraph != null && (startParagraph.Parent is ListItem) && (endParagraph.Parent is ListItem) && object.ReferenceEquals(((ListItem)startParagraph.Parent).List, ((ListItem)endParagraph.Parent).List))
            {
                TextMarkerStyle markerStyle = ((ListItem)startParagraph.Parent).List.MarkerStyle;
                if (markerStyle == TextMarkerStyle.Disc) //bullets
                {
                    _btnBullets.IsChecked = true;
                }
                else if (markerStyle == TextMarkerStyle.Decimal) //numbers
                {
                    _btnNumbers.IsChecked = true;
                }
            }
            else
            {
                _btnBullets.IsChecked = false;
                _btnNumbers.IsChecked = false;
            }
        }

        private void UpdateSelectedFontFamily()
        {
            object value = Workspace.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            FontFamily currentFontFamily = (FontFamily)((value == DependencyProperty.UnsetValue) ? null : value);
            if (currentFontFamily != null)
            {
                _fontFamily.SelectedItem = currentFontFamily;
            }
        }

        private void UpdateSelectedFontSizej()
        {
            object value = Workspace.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            _fontSize.SelectedValue = (value == DependencyProperty.UnsetValue) ? null : value;
        }


        void ApplyPropertyValueToSelectedText(DependencyProperty formattingProperty, object value)
        {
            if (value == null)
                return;

            Workspace.Selection.ApplyPropertyValue(formattingProperty, value);
        }
        public static void selectImg(RichTextBox rc)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.jpg, *.jpeg,*.gif, *.png) | *.jpg; *.jpeg; *.gif; *.png";
            var result = dlg.ShowDialog();

            if (result.Value)
            {
                Uri uri = new Uri(dlg.FileName, UriKind.Relative);
                BitmapImage bitmapImg = new BitmapImage(uri);
                Image image = new Image();
                image.Stretch = Stretch.Fill;
                image.Width = 250;
                image.Height = 200;
                image.Source = bitmapImg;

                var tp = rc.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                new InlineUIContainer(image, tp);

                image.Loaded += delegate
                {
                    AdornerLayer al = AdornerLayer.GetAdornerLayer(image);
                    if (al != null)
                    {
                        al.Add(new ResizingAdorner(image));
                    }
                };

            }
        }
        public void CreateTable(int columns, int rows)
        {
            Table tab = new Table();
            tab.RowGroups.Add(new TableRowGroup());

            for (int i = 0; i < rows; i++)
            {
                tab.RowGroups[0].Rows.Add(new TableRow());
                var tabRow = tab.RowGroups[0].Rows[i];
                tab.RowGroups[0].Rows.Add(CreateNewRow(columns));
            }
            Paragraph p = new Paragraph();
            Workspace.Document.Blocks.Add(tab);
            Workspace.Document.Blocks.Add(p);
        }
        private static TableRow CreateNewRow(int columns)
        {
            TableRow newRow = new TableRow();
            for (int i = 0; i < columns; i++)
            {
                TableCell cell = new TableCell { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1, 1, 1, 1) };
                Paragraph paragraph = new Paragraph();
                Span span = new Span();
                paragraph.Inlines.Add(span);
                cell.Blocks.Add(paragraph);
                newRow.Cells.Add(cell);
            }

            return newRow;
        }



        private static void fontcolor(RichTextBox rc)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var wpfcolor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                TextRange range = new TextRange(rc.Selection.Start, rc.Selection.End);
                range.ApplyPropertyValue(FlowDocument.ForegroundProperty, new SolidColorBrush(wpfcolor));

            }
        }
        public void ReloadDocument(int id)
        {
            client.SendChooseDoc(id);
        }

        private delegate void UpdateTextDelegate(string content);
        public void UpdateText(string content)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new UpdateTextDelegate(UpdateText), new object[] { content });
                return;
            }
            _isUpdatingEditor = true;
            Workspace.Document.Blocks.Clear();
            MemoryStream stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(content));
            TextRange range = new TextRange(Workspace.Document.ContentStart, Workspace.Document.ContentEnd);
            range.Load(stream, DataFormats.Rtf);
            _isUpdatingEditor = false;
        }

        private void SendMessage()
        {
            if (_lastUpdate <= DateTime.Now.AddMilliseconds(-deley))
            {
                SendMessageIfNotUpdating();
            }
            else if (_lastUpdate > _delayedUpdate)
            {
                _delayedUpdate = _lastUpdate.AddMilliseconds(deley);
                Task.Delay(TimeSpan.FromMilliseconds(deley)).ContinueWith(x =>
                {
                    if (_lastUpdate <= _delayedUpdate)
                    {
                        SendMessageIfNotUpdating();
                    }
                });
            }
        }

        private delegate void SendMessageIfNotUpdatingDelegate();
        private void SendMessageIfNotUpdating()
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new SendMessageIfNotUpdatingDelegate(SendMessageIfNotUpdating), new object[] { });
                return;
            }
            if (!_isUpdatingEditor && UpdateDocument != null)
            {
                string rtfFromRtb = string.Empty;
                using (MemoryStream ms = new MemoryStream())
                {
                    TextRange range2 = new TextRange(Workspace.Document.ContentStart, Workspace.Document.ContentEnd);
                    range2.Save(ms, DataFormats.Rtf);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        rtfFromRtb = sr.ReadToEnd();
                    }
                }
                _lastUpdate = DateTime.Now;
                UpdateDocument(this, new UpdateDocumentRequest
                {
                    DocumentId = docId,
                    NewContent = rtfFromRtb
                });
            }
        }
        private void WriteOnScreen(string text)
        {
            if(Workspace.Visibility == Visibility.Hidden) Workspace.Visibility = Visibility.Visible; 
            Workspace.Document.Blocks.Clear();
            MemoryStream stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(text));
            TextRange range = new TextRange(Workspace.Document.ContentStart, Workspace.Document.ContentEnd);
            range.Load(stream, DataFormats.Rtf);
        }

        private void ReciveDocument(object sender, Document doc)
        {
            CreateDocument?.Invoke(this, doc);
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new WriteOnScreenDelegate(WriteOnScreen), new object[] { doc.Content });
                return;
            }
        }
        private void Createlist(string list)
        {
            ListofDoc listofDoc = new ListofDoc(list, this);
        }
        private void CreatelistShared(string list)
        {
            ListofDDoc listofDoc = new ListofDDoc(list, this);
        }

        private void CreateUsersList(string list)
        {
            Editors editors = new Editors(this, list);
        }

        private void ReciveList(object sender, string list)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new CreatelistDelegate(Createlist), new object[] { list });
                return;
            }
        }
        private void ReciveSharedList(object sender, string list)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new CreatelistSharedDelegate(CreatelistShared), new object[] { list });
                return;
            }
        }
        private void ReciveUsersList(object sender, string list)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new CreateUserslistDelegate(CreateUsersList), new object[] { list });
                return;
            }
        }

        private void AckRequest(object sender, AcknowledgeDto acknowledgeDto)
        {
            patchingLogic.AckRequest(acknowledgeDto);
        }

        private void UpdateRequest(object sender, UpdateDto dto)
        {
            patchingLogic.UpdateRequest(dto);
        }
        private string Gt()
        {
            string rtfFromRtb = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                TextRange range2 = new TextRange(Workspace.Document.ContentStart, Workspace.Document.ContentEnd);
                range2.Save(ms, DataFormats.Rtf);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    rtfFromRtb = sr.ReadToEnd();
                }
            }
            System.Diagnostics.Debug.Write(rtfFromRtb);
            return rtfFromRtb;
        }
        public string GetText()
        {
            string text = (string)Dispatcher.Invoke(new GetTextDelegate(Gt), new object[] { });
            return text;
        }
        #endregion

        #region Click
        private void btn_Font_Click(object sender, RoutedEventArgs e)
        {
            fontcolor(Workspace);
        }

        private void btn_SaveDoc_Click(object sender, RoutedEventArgs e)
        {
            client.SendRequest("04");
        }

        private void btn_OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            client.SendRequest("03");
        }

        private void btn_SavDoc_Click(object sender, RoutedEventArgs e)
        {
            if (Workspace.Visibility == Visibility.Visible)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*";
                if (dlg.ShowDialog() == true)
                {
                    FileStream fileStream = new FileStream(dlg.FileName, FileMode.Create);
                    TextRange range = new TextRange(Workspace.Document.ContentStart, Workspace.Document.ContentEnd);
                    range.Save(fileStream, DataFormats.Rtf);
                }
            }
            else
            {
                MessageBox.Show("Najpierw stwórz lub otwórz dokument");
            }
        }
        #endregion

        #region Delegate
        private delegate string GetTextDelegate();

    

        private delegate void WriteOnScreenDelegate(string list);

       

        private delegate void CreatelistDelegate(string list);


        private delegate void CreatelistSharedDelegate(string list);

        private delegate void CreateUserslistDelegate(string list);

        #endregion

        public event EventHandler<Document> CreateDocument;
        public event EventHandler<UpdateDocumentRequest> UpdateDocument;
    }
    public class UpdateDocumentRequest
    {
        public string DocumentId { get; set; }
        public string NewContent { get; set; }
    }
}

