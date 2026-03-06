using Microsoft.EntityFrameworkCore;
using StadiumTicketBooking.Data.Entity;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StadiumTicketBooking.Forms
{
    public partial class frmSanVanDong : Form
    {
        StadiumDbContext context = new StadiumDbContext();
        bool xuLyThem = false;
        int currentId;
        string imagesFolder;
        string stadiumImagesFolder;

        public frmSanVanDong()
        {
            InitializeComponent();

            imagesFolder = GetImagesFolder();
            stadiumImagesFolder = Path.Combine(imagesFolder, "AnhSanVanDong");

            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            if (!Directory.Exists(stadiumImagesFolder))
                Directory.CreateDirectory(stadiumImagesFolder);

            dgvSanVanDong.DataError += (s, e) => { e.ThrowException = false; };
            dgvSanVanDong.CellFormatting += dgvSanVanDong_CellFormatting;
        }

        private string GetImagesFolder()
        {
            string baseDir = Application.StartupPath;
            string candidate = Path.Combine(baseDir, "Images");
            if (Directory.Exists(candidate))
                return candidate;

            DirectoryInfo dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                candidate = Path.Combine(dir.FullName, "Images");
                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return Path.Combine(baseDir, "Images");
        }

        private string FindImagePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            string directPath = Path.Combine(imagesFolder, fileName);
            if (File.Exists(directPath))
                return directPath;

            string stadiumPath = Path.Combine(stadiumImagesFolder, fileName);
            if (File.Exists(stadiumPath))
                return stadiumPath;

            try
            {
                var files = Directory.GetFiles(imagesFolder, fileName, SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
            catch
            {
            }

            return null;
        }

        private void BatTatChucNang(bool dangSua)
        {
            btnLuu.Enabled = dangSua;
            btnHuy.Enabled = dangSua;
            txtTenSan.Enabled = dangSua;
            txtDiaChi.Enabled = dangSua;
            btnDoiAnh.Enabled = dangSua;

            btnThem.Enabled = !dangSua;
            btnSua.Enabled = !dangSua;
            btnXoa.Enabled = !dangSua;
            dgvSanVanDong.Enabled = !dangSua;

            txtID.ReadOnly = true;
        }

        private void dgvSanVanDong_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (dgvSanVanDong.Columns[e.ColumnIndex].Name == "colHinhAnh" ||
                dgvSanVanDong.Columns[e.ColumnIndex].HeaderText.Contains("Ảnh"))
            {
                if (e.Value != null && !string.IsNullOrWhiteSpace(e.Value.ToString()))
                {
                    string fileName = e.Value.ToString();
                    string path = FindImagePath(fileName);

                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(path);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            using (Image img = Image.FromStream(ms))
                            {
                                e.Value = new Bitmap(img, 60, 40);
                                e.FormattingApplied = true;
                            }
                        }
                        catch
                        {
                            e.Value = null;
                            e.FormattingApplied = true;
                        }
                    }
                    else
                    {
                        e.Value = null;
                        e.FormattingApplied = true;
                    }
                }
                else
                {
                    e.Value = null;
                    e.FormattingApplied = true;
                }
            }
        }

        private void frmSanVanDong_Load(object sender, EventArgs e)
        {
            BatTatChucNang(false);
            dgvSanVanDong.AutoGenerateColumns = false;
            LoadDataGrid();
        }

        private void LoadDataGrid()
        {
            var listSVD = context.SanVanDong.ToList();
            BindingSource bs = new BindingSource();
            bs.DataSource = listSVD;

            txtID.DataBindings.Clear();
            txtID.DataBindings.Add("Text", bs, "ID", true, DataSourceUpdateMode.Never);

            txtTenSan.DataBindings.Clear();
            txtTenSan.DataBindings.Add("Text", bs, "TenSan", true, DataSourceUpdateMode.Never);

            txtDiaChi.DataBindings.Clear();
            txtDiaChi.DataBindings.Add("Text", bs, "DiaChi", true, DataSourceUpdateMode.Never);

            picHinhAnh.DataBindings.Clear();
            Binding bImg = new Binding("ImageLocation", bs, "HinhAnh", true);
            bImg.Format += (s, ev) =>
            {
                if (ev.Value != null && !string.IsNullOrWhiteSpace(ev.Value.ToString()))
                {
                    string fullPath = FindImagePath(ev.Value.ToString());
                    ev.Value = !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath) ? fullPath : null;
                }
                else
                {
                    ev.Value = null;
                }
            };
            picHinhAnh.DataBindings.Add(bImg);

            dgvSanVanDong.DataSource = bs;
        }

        private void btnDoiAnh_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Images|*.jpg;*.png;*.jpeg";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string fileName = Path.GetFileName(ofd.FileName);
                    string destPath = Path.Combine(stadiumImagesFolder, fileName);

                    File.Copy(ofd.FileName, destPath, true);
                    picHinhAnh.ImageLocation = destPath;
                }
            }
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenSan.Text))
            {
                MessageBox.Show("Tên sân không được để trống!");
                return;
            }

            try
            {
                string fileNameOnly = string.IsNullOrEmpty(picHinhAnh.ImageLocation)
                    ? ""
                    : Path.GetFileName(picHinhAnh.ImageLocation);

                if (xuLyThem)
                {
                    SanVanDong svd = new SanVanDong
                    {
                        TenSan = txtTenSan.Text.Trim(),
                        DiaChi = txtDiaChi.Text.Trim(),
                        HinhAnh = fileNameOnly
                    };
                    context.SanVanDong.Add(svd);
                }
                else
                {
                    var svd = context.SanVanDong.Find(currentId);
                    if (svd != null)
                    {
                        svd.TenSan = txtTenSan.Text.Trim();
                        svd.DiaChi = txtDiaChi.Text.Trim();

                        if (!string.IsNullOrEmpty(fileNameOnly))
                            svd.HinhAnh = fileNameOnly;
                    }
                }

                context.SaveChanges();
                MessageBox.Show("Lưu thành công!");
                LoadDataGrid();
                BatTatChucNang(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            xuLyThem = true;
            BatTatChucNang(true);

            txtID.Text = "Tự động";
            txtTenSan.Clear();
            txtDiaChi.Clear();
            picHinhAnh.ImageLocation = null;
            picHinhAnh.Image = null;

            txtTenSan.Focus();
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (dgvSanVanDong.CurrentRow == null) return;

            xuLyThem = false;
            BatTatChucNang(true);
            currentId = Convert.ToInt32(dgvSanVanDong.CurrentRow.Cells["colID"].Value);
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (dgvSanVanDong.CurrentRow == null) return;

            int idXoa = Convert.ToInt32(dgvSanVanDong.CurrentRow.Cells["colID"].Value);
            string tenSan = dgvSanVanDong.CurrentRow.Cells["colTenSan"].Value?.ToString() ?? "";

            if (MessageBox.Show($"Xác nhận xóa sân: {tenSan}?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                var svd = context.SanVanDong.Find(idXoa);
                if (svd != null)
                {
                    context.SanVanDong.Remove(svd);
                    context.SaveChanges();
                    LoadDataGrid();
                }
            }
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            LoadDataGrid();
            BatTatChucNang(false);
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}