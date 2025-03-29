using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lexer
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            InitializeDataGridViewColumns(dataGridView1);
        }

        private void InitializeDataGridViewColumns(DataGridView dataGridView)
        {
            if (dataGridView1 != null)
            {
                // Очищаем старые столбцы перед добавлением новых
                dataGridView1.Columns.Clear();

                // Код лексемы
                this.dataGridView1.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name = "TokenCode",
                    HeaderText = "Код лексемы",
                    Width = 100
                });

                // Тип лексемы (ключевое слово, идентификатор, оператор и т. д.)
                this.dataGridView1.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name = "TokenType",
                    HeaderText = "Тип лексемы",
                    Width = 150
                });

                // Сама лексема
                this.dataGridView1.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name = "TokenValue",
                    HeaderText = "Лексема",
                    AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
                });

                // Номер строки
                this.dataGridView1.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name = "LineNumber",
                    HeaderText = "Номер строки",
                    Width = 110
                });

                // Позиция в строке
                this.dataGridView1.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name = "Position",
                    HeaderText = "Позиция",
                    Width = 110
                });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Перебираем все вкладки в TabControl
            for (int i = tabControl1.TabCount - 1; i >= 0; i--)
            {
                TabPage tabPage = tabControl1.TabPages[i];
                SplitContainer splitContainer = tabPage.Controls[0] as SplitContainer;
                RichTextBox richTextBox1 = splitContainer.Panel1.Controls[0] as RichTextBox; // Верхний RichTextBox

                // Проверяем, есть ли изменения
                if (richTextBox1.Modified)
                {
                    // Спрашиваем пользователя, хочет ли он сохранить изменения
                    DialogResult result = MessageBox.Show("Сохранить изменения в " + tabPage.Text + "?",
                                                          "Предупреждение", MessageBoxButtons.YesNoCancel,
                                                          MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        // Сохранение файла
                        try
                        {
                            сохранитьToolStripMenuItem_Click(sender, e);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Ошибка при сохранении файла: " + ex.Message, "Ошибка",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            e.Cancel = true; // Отменяем закрытие при ошибке
                            return;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true; // Отменяем закрытие формы
                        return;
                    }
                }
            }
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewTab(null, "Новый документ", "");
        }

        private void OpenFileInNewTab(string filePath)
        {
            string fileContent = File.ReadAllText(filePath);
            string fileName = Path.GetFileName(filePath);

            CreateNewTab(filePath, fileName, fileContent);
        }

        private void CreateNewTab(string filePath, string tabTitle, string fileContent)
        {
            // Создаём новую вкладку
            TabPage newTab = new TabPage(tabTitle)
            {
                Padding = new Padding(3),
                UseVisualStyleBackColor = true,
                BackColor = Color.FloralWhite
            };

            // Создаём SplitContainer для редактора с нумерацией строк
            SplitContainer editorSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                IsSplitterFixed = true,
                SplitterWidth = 1,
                FixedPanel = FixedPanel.Panel1
            };

            // Определяем ширину панели нумерации
            using (RichTextBox tempRichTextBox = new RichTextBox())
            {
                int requiredWidth = TextRenderer.MeasureText("999", tempRichTextBox.Font).Width + 10;
                editorSplitContainer.SplitterDistance = requiredWidth;
            }

            // Панель для нумерации строк
            Panel lineNumberPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Linen
            };

            // RichTextBox для редактирования кода
            RichTextBox editorRichTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Linen,
                Text = fileContent,
                Tag = filePath,
                Name = "editorRichTextBox",
                BorderStyle = BorderStyle.None
            };

            // Инициализация нумерации строк
            InitializeLineNumbering(lineNumberPanel, editorRichTextBox);

            // Добавляем элементы в контейнеры
            editorSplitContainer.Panel1.Controls.Add(lineNumberPanel);
            editorSplitContainer.Panel2.Controls.Add(editorRichTextBox);

            // Добавляем редактор во вкладку
            newTab.Controls.Add(editorSplitContainer);

            // Добавляем вкладку в tabControl1, который находится в splitcontainer1.Panel1
            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectedTab = newTab;
        }

        // Методы для нумерации строк остаются без изменений
        private void InitializeLineNumbering(Panel panel, RichTextBox richTextBox)
        {
            panel.Paint += (sender, e) => LineNumberPanel_Paint(sender, e, richTextBox);
            richTextBox.TextChanged += (s, e) => panel.Invalidate();
            richTextBox.VScroll += (s, e) => panel.Invalidate();
            richTextBox.SelectionChanged += (s, e) => panel.Invalidate();
            richTextBox.FontChanged += (s, e) => panel.Invalidate();
        }

        private void LineNumberPanel_Paint(object sender, PaintEventArgs e, RichTextBox richTextBox)
        {
            int firstIndex = richTextBox.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = richTextBox.GetLineFromCharIndex(firstIndex);

            int lineHeight = TextRenderer.MeasureText("0", richTextBox.Font).Height;
            int y = 0;

            for (int i = firstLine; y < richTextBox.Height; i++)
            {
                y = (i - firstLine) * lineHeight;
                e.Graphics.DrawString((i + 1).ToString(), richTextBox.Font, Brushes.Black, new PointF(5, y));
            }
        }


        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            openFileDialog.Title = "Открыть файл";
            openFileDialog.Multiselect = true; // Разрешаем выбирать несколько файлов

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    OpenFileInNewTab(filePath);
                }
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }


        public void Save()
        {
            if (tabControl1.SelectedTab == null) return;

            // Получаем текущий редактор
            var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
            if (splitContainer == null) return;

            var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
            if (richTextBox == null) return;

            string filePath = richTextBox.Tag as string;

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    File.WriteAllText(filePath, richTextBox.Text);
                    richTextBox.Modified = false;
                    MessageBox.Show("Файл сохранён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении файла: " + ex.Message,
                                   "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                сохранитьКакToolStripMenuItem_Click(null, null);
            }
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == null) return;

            var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
            if (splitContainer == null) return;

            var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
            if (richTextBox == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл как";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, richTextBox.Text);
                    richTextBox.Tag = saveFileDialog.FileName;
                    tabControl1.SelectedTab.Text = Path.GetFileName(saveFileDialog.FileName);
                    richTextBox.Modified = false;
                    MessageBox.Show("Файл успешно сохранён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении файла: " + ex.Message,
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Перебираем все вкладки в TabControl
            for (int i = tabControl1.TabCount - 1; i >= 0; i--)
            {
                TabPage tabPage = tabControl1.TabPages[i];

                // Получаем SplitContainer из вкладки
                var splitContainer = tabPage.Controls.OfType<SplitContainer>().FirstOrDefault();
                if (splitContainer == null) continue;

                // Получаем RichTextBox из Panel2 SplitContainer
                var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
                if (richTextBox == null) continue;

                // Проверяем, есть ли изменения
                if (richTextBox.Modified)
                {
                    // Спрашиваем пользователя, хочет ли он сохранить изменения
                    DialogResult result = MessageBox.Show($"Сохранить изменения в {tabPage.Text}?",
                        "Предупреждение",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        // Сохранение файла
                        try
                        {
                            // Сохраняем текущую вкладку
                            tabControl1.SelectedTab = tabPage;
                            сохранитьToolStripMenuItem_Click(sender, e);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}",
                                "Ошибка",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return; // Выход из метода при ошибке
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return; // Прерываем выход
                    }
                }
            }

            // Закрываем приложение
            Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            создатьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            открытьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
                if (splitContainer != null)
                {
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
                    if (richTextBox != null && richTextBox.CanUndo)
                    {
                        richTextBox.Undo(); // Отменяем последнее действие
                        richTextBox.Focus(); // Возвращаем фокус для визуального отображения
                    }
                }
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            отменитьToolStripMenuItem_Click(sender, e);
        }

        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
                if (splitContainer != null)
                {
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
                    if (richTextBox != null && richTextBox.CanRedo)
                    {
                        richTextBox.Redo(); // Повторяем последнее действие
                    }
                }
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            повторитьToolStripMenuItem_Click(sender, e);
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                // Безопасное получение SplitContainer
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();

                if (splitContainer != null)
                {
                    // Получаем RichTextBox из Panel2
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (richTextBox != null && richTextBox.SelectionLength > 0)
                    {
                        richTextBox.Cut(); // Вырезаем выделенный текст
                    }
                }
            }
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            вырезатьToolStripMenuItem_Click(sender, e);
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                // Безопасное получение SplitContainer
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();

                if (splitContainer != null)
                {
                    // Получаем RichTextBox из Panel2
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (richTextBox != null && richTextBox.SelectionLength > 0)
                    {
                        richTextBox.Copy();
                    }
                }
            }
        }
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            копироватьToolStripMenuItem_Click(sender, e);
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                // Безопасное получение SplitContainer
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();

                if (splitContainer != null)
                {
                    // Получаем RichTextBox из Panel2
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (richTextBox != null)
                    {
                        // Проверяем, есть ли текст в буфере обмена
                        if (Clipboard.ContainsText())
                        {
                            richTextBox.Paste();
                        }
                    }
                }
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            вставитьToolStripMenuItem_Click(sender, e);
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                // Безопасное получение SplitContainer
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();

                if (splitContainer != null)
                {
                    // Получаем RichTextBox из Panel2
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (richTextBox != null && richTextBox.SelectionLength > 0)
                    {
                        // Удаляем выделенный текст
                        richTextBox.SelectedText = string.Empty;
                    }
                }
            }
        }

        private void выделитьВсёToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                // Получаем SplitContainer из выбранной вкладки
                var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();

                // Проверяем, что SplitContainer не равен null
                if (splitContainer != null)
                {
                    // Получаем RichTextBox из Panel2 SplitContainer
                    var richTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();

                    // Проверяем, что richTextBox не равен null
                    if (richTextBox != null)
                    {
                        // Выделяем весь текст в RichTextBox
                        richTextBox.SelectAll();
                        richTextBox.Focus(); // Даем фокус для визуального выделения
                    }
                }
            }
        }
        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Создаем и показываем форму справки
                Help helpForm = new Help();
                helpForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть справку: " + ex.Message,
                              "Ошибка",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Создаем и показываем форму "О программе"
                About aboutForm = new About();
                aboutForm.ShowDialog(); // Используем ShowDialog для модального окна
            }
            catch (Exception ex)
            {
                // Если возникла ошибка при создании формы, показываем MessageBox
                MessageBox.Show("Не удалось открыть информацию о программе: " + ex.Message,
                                "Ошибка",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            вызовСправкиToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            оПрограммеToolStripMenuItem_Click(sender, e);
        }

        private void AnalyzeRecordType()
        {
            if (tabControl1.SelectedTab == null) return;

            var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
            if (splitContainer == null) return;

            var editorRichTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
            if (editorRichTextBox == null) return;

            string inputText = editorRichTextBox.Text;
            RecordTypeParser parser = new RecordTypeParser(inputText);
            bool isValid = parser.Parse();

            dataGridView1.Rows.Clear();

            if (isValid)
            {
                dataGridView1.Rows.Add("0", "Успех", "Синтаксис правильный", "1", "1");
                MessageBox.Show("Анализ завершен успешно. Ошибок не найдено.", "Результат анализа",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var errors = parser.GetErrors();
                foreach (var error in errors)
                {
                    dataGridView1.Rows.Add("Ошибка", error.Message, error.Fragment,
                                         error.Line.ToString(), error.Position.ToString());
                }

                MessageBox.Show($"Найдено {errors.Count} ошибок.", "Результат анализа",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            AnalyzeRecordType();
        }

        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AnalyzeRecordType();
        }



        //private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    if (tabControl1.SelectedTab == null) return;

        //    // Получаем SplitContainer из текущей вкладки
        //    var splitContainer = tabControl1.SelectedTab.Controls.OfType<SplitContainer>().FirstOrDefault();
        //    if (splitContainer == null) return;

        //    // Получаем RichTextBox из Panel2 SplitContainer
        //    var editorRichTextBox = splitContainer.Panel2.Controls.OfType<RichTextBox>().FirstOrDefault();
        //    if (editorRichTextBox == null) return;

        //    // Используем существующий dataGridView1, который должен быть в splitcontainer1.Panel2
        //    if (dataGridView1 == null) return;

        //    // Анализируем текст
        //    Scanner scanner = new Scanner();
        //    dataGridView1.Rows.Clear();
        //    scanner.Analyze(editorRichTextBox.Text, dataGridView1, editorRichTextBox);
        //}

        //private void toolStripButton9_Click(object sender, EventArgs e)
        //{
        //    пускToolStripMenuItem_Click(sender, e);
        //}


    }
}
