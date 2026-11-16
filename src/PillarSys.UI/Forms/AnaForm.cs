using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FuzzySharp;
using PillarSys.Application.DTO;
using PillarSys.Application.Services;
using PillarSys.Application.Validation;
using PillarSys.AnalyticsRunner;
using PillarSys.UI.Config;
using Serilog;

namespace PillarSys.UI.Forms;

public sealed class AnaForm : Form
{
    private readonly Ayarlar _ayarlar;
    private readonly IlacService _ilacService;
    private readonly PythonAnalizCalistirici _pythonAnaliz;

    // Sol panel kontrolleri
    private readonly ComboBox _cmbIlacAdi = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 260 };
    private readonly TextBox _txtBarkod = new() { Width = 260 };
    private readonly TextBox _txtKategori = new() { Width = 260 };
    private readonly TextBox _txtBirim = new() { Width = 260 };
    private readonly NumericUpDown _numStok = new() { Width = 260, Minimum = 0, Maximum = 1_000_000, DecimalPlaces = 0 };
    private readonly NumericUpDown _numKritik = new() { Width = 260, Minimum = 0, Maximum = 1_000_000, DecimalPlaces = 0 };
    private readonly DateTimePicker _dtSonKullanma = new() { Width = 260, Format = DateTimePickerFormat.Short, ShowCheckBox = true };

    private readonly Button _btnEkle = new() { Text = "Ekle", Width = 120, Height = 36 };
    private readonly Button _btnCikart = new() { Text = "Çıkart (Stok Düş)", Width = 120, Height = 36 };
    private readonly Button _btnGuncelle = new() { Text = "Güncelle", Width = 120, Height = 36 };
    private readonly Button _btnTemizle = new() { Text = "Temizle", Width = 120, Height = 36 };

    private readonly Label _lblDurum = new() { AutoSize = false, Height = 36, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };

    // Sağ taraf
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly PictureBox _grafik = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BorderStyle = BorderStyle.FixedSingle
    };

    private int? _seciliIlacId = null;
    private List<IlacDto> _ilaclar = new();

    public AnaForm(Ayarlar ayarlar, IlacService ilacService, PythonAnalizCalistirici pythonAnaliz)
    {
        _ayarlar = ayarlar;
        _ilacService = ilacService;
        _pythonAnaliz = pythonAnaliz;

        Text = "PillarSys - İlaç Stok Takibi & Analiz";
        Width = 1200;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        WireEvents();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        try
        {
            await YukleVeYenileAsync();
            await AnalizYenileAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnShown hata");
            Mesaj("Başlangıç hatası: " + ex.Message, hata: true);
        }
    }

    private void BuildLayout()
    {
        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 380,
            FixedPanel = FixedPanel.Panel1
        };

        // Sol panel form düzeni
        var sol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var solLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2
        };
        solLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        solLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void Row(string label, Control control)
        {
            solLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            solLayout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, solLayout.RowCount);
            control.Dock = DockStyle.Fill;
            solLayout.Controls.Add(control, 1, solLayout.RowCount);
            solLayout.RowCount++;
        }

        Row("İlaç Adı", _cmbIlacAdi);
        Row("Barkod", _txtBarkod);
        Row("Kategori", _txtKategori);
        Row("Birim", _txtBirim);
        Row("Stok", _numStok);
        Row("Kritik", _numKritik);
        Row("Son Kul.", _dtSonKullanma);

        var butonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 10, 0, 0)
        };
        butonPanel.Controls.AddRange(new Control[] { _btnEkle, _btnCikart, _btnGuncelle, _btnTemizle });

        sol.Controls.Add(butonPanel);
        sol.Controls.Add(solLayout);
        sol.Controls.Add(_lblDurum);

        root.Panel1.Controls.Add(sol);

        // Sağ panel: üst grid + alt grafik
        var sagSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360,
            FixedPanel = FixedPanel.Panel2
        };

        var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        gridPanel.Controls.Add(_grid);

        var grafikPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        grafikPanel.Controls.Add(_grafik);

        sagSplit.Panel1.Controls.Add(gridPanel);
        sagSplit.Panel2.Controls.Add(grafikPanel);

        root.Panel2.Controls.Add(sagSplit);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _btnEkle.Click += async (_, __) => await TryEkleAsync();
        _btnCikart.Click += async (_, __) => await TryCikartAsync();
        _btnGuncelle.Click += async (_, __) => await TryGuncelleAsync();
        _btnTemizle.Click += (_, __) => Temizle();

        _grid.SelectionChanged += (_, __) => GridSecimdenDoldur();
        _cmbIlacAdi.Leave += (_, __) => IlacAdiFuzzyDuzelt();
    }

    private async Task YukleVeYenileAsync()
    {
        _ilaclar = await _ilacService.TumIlaclariGetirAsync(CancellationToken.None);

        // Combo
        _cmbIlacAdi.BeginUpdate();
        _cmbIlacAdi.Items.Clear();
        foreach (var x in _ilaclar) _cmbIlacAdi.Items.Add(x.IlacAdi);
        _cmbIlacAdi.EndUpdate();

        _cmbIlacAdi.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbIlacAdi.AutoCompleteSource = AutoCompleteSource.ListItems;

        // Grid
        _grid.DataSource = _ilaclar.Select(x => new
        {
            x.IlacId,
            x.IlacAdi,
            x.Barkod,
            x.Kategori,
            x.Birim,
            x.StokAdedi,
            x.KritikSeviye,
            SonKullanma = x.SonKullanma?.ToString("yyyy-MM-dd"),
            Guncelleme = x.GuncellemeTarihi.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        if (_grid.Columns["IlacId"] is not null)
            _grid.Columns["IlacId"].Visible = false;
    }

    private void GridSecimdenDoldur()
    {
        if (_grid.CurrentRow?.Cells is null) return;

        var ilacIdObj = _grid.CurrentRow.Cells["IlacId"]?.Value;
        if (ilacIdObj is null) return;
        if (!int.TryParse(ilacIdObj.ToString(), out var ilacId)) return;

        var secili = _ilaclar.FirstOrDefault(x => x.IlacId == ilacId);
        if (secili is null) return;

        _seciliIlacId = secili.IlacId;
        _cmbIlacAdi.Text = secili.IlacAdi;
        _txtBarkod.Text = secili.Barkod ?? "";
        _txtKategori.Text = secili.Kategori ?? "";
        _txtBirim.Text = secili.Birim ?? "";
        _numStok.Value = secili.StokAdedi;
        _numKritik.Value = secili.KritikSeviye;

        if (secili.SonKullanma is not null)
        {
            _dtSonKullanma.Checked = true;
            _dtSonKullanma.Value = new DateTime(secili.SonKullanma.Value.Year, secili.SonKullanma.Value.Month, secili.SonKullanma.Value.Day);
        }
        else
        {
            _dtSonKullanma.Checked = false;
        }
    }

    private void IlacAdiFuzzyDuzelt()
    {
        var text = (_cmbIlacAdi.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (_ilaclar.Any(x => x.IlacAdi.Equals(text, StringComparison.Ordinal))) return;

        var adaylar = _ilaclar.Select(x => x.IlacAdi).ToList();
        if (adaylar.Count == 0) return;

        var best = Process.ExtractOne(text, adaylar);
        if (best is null) return;

        if (best.Score >= 85)
        {
            _cmbIlacAdi.Text = best.Value;
            Mesaj($"Otomatik düzeltildi: {best.Value}", hata: false);
        }
    }

    private IlacGirdi ReadGirdi()
    {
        return new IlacGirdi
        {
            IlacAdi = (_cmbIlacAdi.Text ?? "").Trim(),
            Barkod = string.IsNullOrWhiteSpace(_txtBarkod.Text) ? null : _txtBarkod.Text.Trim(),
            Kategori = string.IsNullOrWhiteSpace(_txtKategori.Text) ? null : _txtKategori.Text.Trim(),
            Birim = string.IsNullOrWhiteSpace(_txtBirim.Text) ? null : _txtBirim.Text.Trim(),
            StokAdedi = (int)_numStok.Value,
            KritikSeviye = (int)_numKritik.Value,
            SonKullanma = _dtSonKullanma.Checked ? DateOnly.FromDateTime(_dtSonKullanma.Value.Date) : null
        };
    }

    private async Task TryEkleAsync()
    {
        DisableButtons(true);
        try
        {
            var girdi = ReadGirdi();
            var (ok, mesaj) = await _ilacService.IlacEkleAsync(girdi, CancellationToken.None);
            Mesaj(mesaj, hata: !ok);

            if (ok)
            {
                Temizle();
                await YukleVeYenileAsync();
                await AnalizYenileAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ekle hata");
            Mesaj("Ekleme hatası: " + ex.Message, hata: true);
        }
        finally
        {
            DisableButtons(false);
        }
    }

    private async Task TryCikartAsync()
    {
        DisableButtons(true);
        try
        {
            var ad = (_cmbIlacAdi.Text ?? "").Trim();
            var miktar = (int)_numStok.Value;

            if (miktar <= 0)
            {
                Mesaj("Çıkart için Stok alanına düşülecek miktarı yazın (0'dan büyük).", hata: true);
                return;
            }

            var (ok, mesaj) = await _ilacService.StokCikartAsync(ad, miktar, CancellationToken.None);
            Mesaj(mesaj, hata: !ok);

            if (ok)
            {
                Temizle();
                await YukleVeYenileAsync();
                await AnalizYenileAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Çıkart hata");
            Mesaj("Stok düşme hatası: " + ex.Message, hata: true);
        }
        finally
        {
            DisableButtons(false);
        }
    }

    private async Task TryGuncelleAsync()
    {
        DisableButtons(true);
        try
        {
            if (_seciliIlacId is null)
            {
                Mesaj("Güncellemek için tablodan bir ilaç seçin.", hata: true);
                return;
            }

            var girdi = ReadGirdi();
            var (ok, mesaj) = await _ilacService.IlacGuncelleAsync(_seciliIlacId.Value, girdi, CancellationToken.None);
            Mesaj(mesaj, hata: !ok);

            if (ok)
            {
                await YukleVeYenileAsync();
                await AnalizYenileAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Güncelle hata");
            Mesaj("Güncelleme hatası: " + ex.Message, hata: true);
        }
        finally
        {
            DisableButtons(false);
        }
    }

    private void Temizle()
    {
        _seciliIlacId = null;
        _cmbIlacAdi.Text = "";
        _txtBarkod.Text = "";
        _txtKategori.Text = "";
        _txtBirim.Text = "";
        _numStok.Value = 0;
        _numKritik.Value = 0;
        _dtSonKullanma.Checked = false;
        Mesaj("Temizlendi.", hata: false);
    }

    private async Task AnalizYenileAsync()
    {
        // Python çalıştırma ve grafik yenileme
        var baseDir = AppContext.BaseDirectory;
        var outDir = Path.Combine(baseDir, _ayarlar.Python.OutputRelativeFolder);
        Directory.CreateDirectory(outDir);

        // Scriptin ürettiği grafik dosyası adı (bizim python script standardı)
        var grafikDosyasi = Path.Combine(outDir, "analysis.png");

        try
        {
            // Python çalıştırma (UI donmasın)
            var (ok, hata) = await Task.Run(() => _pythonAnaliz.Calistir());

            if (!ok)
            {
                Mesaj("Analiz hatası: " + hata, hata: true);
                return;
            }

            // Grafik dosyası oluştu mu?
            if (!File.Exists(grafikDosyasi))
            {
                Mesaj("Analiz grafiği oluşmadı: " + grafikDosyasi, hata: true);
                return;
            }

            // Image dosyası kilitlenmesin diye FileShare ile açıp clone’layalım
            using var fs = new FileStream(grafikDosyasi, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var img = Image.FromStream(fs);
            var clone = new Bitmap(img);

            var old = _grafik.Image;
            _grafik.Image = clone;
            old?.Dispose();

            Mesaj("Analiz güncellendi.", hata: false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Analiz yenile hata");
            Mesaj("Analiz hatası: " + ex.Message, hata: true);
        }
    }

    private void DisableButtons(bool disabled)
    {
        _btnEkle.Enabled = !disabled;
        _btnCikart.Enabled = !disabled;
        _btnGuncelle.Enabled = !disabled;
        _btnTemizle.Enabled = !disabled;
        _cmbIlacAdi.Enabled = !disabled;
    }

    private void Mesaj(string text, bool hata)
    {
        _lblDurum.Text = text;
        _lblDurum.ForeColor = hata ? Color.Maroon : Color.DarkGreen;
    }
}
