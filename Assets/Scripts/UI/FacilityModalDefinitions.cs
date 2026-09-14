using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Environment;
using TanamSawit.Managers;
using TanamSawit.Buildings;

namespace TanamSawit.UI
{
    /// <summary>
    /// Definisi tombol untuk modal fasilitas. Setiap tombol punya label, cek visibilitas,
    /// cek enabled, dan action onClick. Dipakai oleh <see cref="FacilityModalUI"/>.
    /// </summary>
    [Serializable]
    public class FacilityButtonSpec
    {
        public string label;
        public Func<bool> isVisible;    // jika false, tombol disembunyikan
        public Func<bool> isEnabled;    // jika false, tombol disabled (greyed)
        public Action onClick;
        public Color buttonColor = TanamSawit.UI.UIRoot.BgButton;

        public static FacilityButtonSpec Create(string label, Action onClick,
            Func<bool> isVisible = null, Func<bool> isEnabled = null,
            Color? color = null)
        {
            return new FacilityButtonSpec
            {
                label = label,
                onClick = onClick,
                isVisible = isVisible,
                isEnabled = isEnabled,
                buttonColor = color ?? TanamSawit.UI.UIRoot.BgButton
            };
        }
    }

    /// <summary>
    /// Definisi lengkap sebuah modal fasilitas: judul, deskripsi dinamis, dan daftar tombol.
    /// </summary>
    public class FacilityDefinition
    {
        public string title;
        public Func<string> getDescription;
        public List<FacilityButtonSpec> buttons;
    }

    /// <summary>
    /// Tabel definisi untuk semua 12 FacilityType. Memetakan setiap jenis fasilitas ke
    /// judul, deskripsi dinamis, dan tombol aksi. Menggantikan 10 method DrawModalXXX
    /// di ModernTycoonHUD dengan satu tabel deklaratif.
    /// </summary>
    public static class FacilityModalDefinitions
    {
        public static FacilityDefinition Get(FacilityType type)
        {
            switch (type)
            {
                case FacilityType.PapanLahan:
                case FacilityType.MandorKebun:
                    return GetKebun();

                case FacilityType.GudangTBS:
                    return GetGudang();

                case FacilityType.MessPekerja:
                    return GetMess();

                case FacilityType.KosKosan:
                    return GetKos();

                case FacilityType.Bank:
                    return GetBank();

                case FacilityType.Pinjol:
                    return GetPinjol();

                case FacilityType.Rentenir:
                    return GetRentenir();

                case FacilityType.PabrikCPO:
                    return GetPabrik();

                case FacilityType.YayasanCSR:
                    return GetYayasan();

                case FacilityType.BillboardSepupu:
                    return GetSepupu();

                default:
                    return new FacilityDefinition
                    {
                        title = "Fasilitas",
                        getDescription = () => "Tidak ada aksi tersedia untuk fasilitas ini.",
                        buttons = new List<FacilityButtonSpec>()
                    };
            }
        }

        // ── Kebun: beli lahan + panen ────────────────────────────────────

        private static FacilityDefinition GetKebun()
        {
            var econ = EconomyManager.Instance;
            var workers = WorkerManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDF31 Kelola Lahan & Panen",
                getDescription = () =>
                {
                    var sb = new System.Text.StringBuilder();
                    if (econ != null)
                    {
                        sb.AppendLine($"Lahan Dikuasai: {econ.CurrentLandPercentage:F1}% / 100%");
                        sb.AppendLine($"Kas Tersedia: {EconomyManager.FormatCurrency(econ.CurrentMoney)}");
                    }
                    return sb.ToString();
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("+2.5% Lahan (Rp 30 Jt)",
                        () => econ?.PurchaseLand(2.5f, 30_000_000),
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("+10% Lahan (Rp 120 Jt)",
                        () => econ?.PurchaseLand(10f, 120_000_000),
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("\uD83C\uDF3E Panen TBS Sekarang",
                        () => workers?.HarvestTBS()),
                }
            };
        }

        // ── Gudang TBS ───────────────────────────────────────────────────

        private static FacilityDefinition GetGudang()
        {
            var wm = WorkerManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83D\uDCE6 Gudang TBS",
                getDescription = () =>
                {
                    if (wm == null) return "";
                    return $"Stok TBS: {wm.TbsStockTon:F1} Ton\nStok CPO: {wm.CpoStockTon:F1} Ton\nPabrik: {(wm.HasFactory ? "Aktif" : "Belum ada")}";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83D\uDCB0 Jual TBS Mentah",
                        () => wm?.SellTBS()),
                    FacilityButtonSpec.Create("\u2699\uFE0F Olah TBS \u2192 CPO",
                        () => wm?.ProcessTbsToCpo(),
                        isVisible: () => wm != null && wm.HasFactory),
                    FacilityButtonSpec.Create("\uD83D\uDEA2 Ekspor CPO",
                        () => wm?.SellCPO(),
                        isVisible: () => wm != null && wm.HasFactory,
                        color: UIRoot.BgButtonSuccess),
                }
            };
        }

        // ── Mess Pekerja (display only) ──────────────────────────────────

        private static FacilityDefinition GetMess()
        {
            var wm = WorkerManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDFE8 Mess Pekerja",
                getDescription = () =>
                {
                    if (wm == null) return "";
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Pekerja Aktif: {wm.ActiveWorkersCount} orang");
                    sb.AppendLine();
                    foreach (var w in wm.Workers)
                    {
                        string status = w.IsExhausted ? "Kelelahan!" : $"Stamina: {w.stamina:F0}%";
                        sb.AppendLine($"  \u2022 {w.workerName}  {status}  Exp:{w.experience}");
                    }
                    return sb.ToString();
                },
                buttons = new List<FacilityButtonSpec>()
            };
        }

        // ── Kos-kosan ─────────────────────────────────────────────────────

        private static FacilityDefinition GetKos()
        {
            var loan = LoanManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDFDA Kos-kosan",
                getDescription = () =>
                {
                    if (loan == null) return "";
                    return $"Kos Dimiliki: {loan.OwnedBoardingHouses} unit\nPassive Income: Rp 1.500.000 / unit / bulan";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83C\uDFDA\uFE0F Bangun 1 Unit Kos-kosan (Rp 75 Jt)",
                        () => loan?.BuyBoardingHouse(),
                        color: UIRoot.BgButtonSuccess),
                }
            };
        }

        // ── Bank ──────────────────────────────────────────────────────────

        private static FacilityDefinition GetBank()
        {
            var loan = LoanManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDFE6 Bank Konvensional",
                getDescription = () => loan != null
                    ? $"Hutang Bank Aktif: {EconomyManager.FormatCurrency(loan.BankDebt)}"
                    : "",
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83D\uDCCB Pinjam Rp 50 Jt",
                        () => loan?.BorrowBank(50_000_000),
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("\uD83D\uDCCB Pinjam Rp 100 Jt",
                        () => loan?.BorrowBank(100_000_000),
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("\uD83D\uDCB5 Bayar Cicilan Rp 25 Jt",
                        () => loan?.RepayDebt(LoanType.BankKonvensional, 25_000_000),
                        isVisible: () => loan != null && loan.BankDebt > 0),
                }
            };
        }

        // ── Pinjol ────────────────────────────────────────────────────────

        private static FacilityDefinition GetPinjol()
        {
            var loan = LoanManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83D\uDCF1 Kantor Pinjol",
                getDescription = () =>
                {
                    if (loan == null) return "";
                    return $"Hutang Pinjol: {EconomyManager.FormatCurrency(loan.PinjolDebt)}\n\u26A0\uFE0F Bunga harian 2% berlipat ganda! Teror debt collector jika gagal bayar!";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83D\uDCF1 Cairkan Pinjol Rp 20 Jt",
                        () => loan?.BorrowPinjol(20_000_000),
                        color: UIRoot.BgButtonDanger),
                    FacilityButtonSpec.Create("\uD83D\uDCB5 Bayar Pinjol Rp 10 Jt",
                        () => loan?.RepayDebt(LoanType.Pinjol, 10_000_000),
                        isVisible: () => loan != null && loan.PinjolDebt > 0),
                }
            };
        }

        // ── Rentenir ──────────────────────────────────────────────────────

        private static FacilityDefinition GetRentenir()
        {
            var loan = LoanManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83D\uDCB0 Warung Rentenir Madura",
                getDescription = () =>
                {
                    if (loan == null) return "";
                    return $"Hutang Rentenir: {EconomyManager.FormatCurrency(loan.RentenirDebt)}\nBunga 1% per hari. Jangan sampai gagal bayar!";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83D\uDCB1 Pinjam Tunai Rp 30 Jt",
                        () => loan?.BorrowRentenirMadura(30_000_000),
                        color: UIRoot.BgButtonDanger),
                    FacilityButtonSpec.Create("\uD83D\uDCB5 Bayar Rp 15 Jt",
                        () => loan?.RepayDebt(LoanType.RentenirMadura, 15_000_000),
                        isVisible: () => loan != null && loan.RentenirDebt > 0),
                }
            };
        }

        // ── Pabrik CPO ────────────────────────────────────────────────────

        private static FacilityDefinition GetPabrik()
        {
            var wm = WorkerManager.Instance;
            var bm = BuildingManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDFED Pabrik Pengolahan CPO",
                getDescription = () =>
                {
                    if (wm == null) return "";
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Status Pabrik: {(wm.HasFactory ? "Beroperasi" : "Belum Dibangun")}");
                    sb.AppendLine($"Stok TBS: {wm.TbsStockTon:F1} Ton | CPO: {wm.CpoStockTon:F1} Ton");
                    if (bm != null)
                        sb.AppendLine($"Kerusakan: {(bm.IsFactoryDamaged ? "RUSAK (perlu perbaikan)" : "Normal")}");
                    return sb.ToString();
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83C\uDFED Bangun Pabrik CPO (Rp 150 Jt)",
                        () => bm?.BuildFactory(),
                        isVisible: () => wm != null && !wm.HasFactory,
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("\u2699\uFE0F Olah TBS \u2192 CPO (5:1)",
                        () => wm?.ProcessTbsToCpo(),
                        isVisible: () => wm != null && wm.HasFactory),
                    FacilityButtonSpec.Create("\uD83D\uDEA2 Ekspor CPO",
                        () => wm?.SellCPO(),
                        isVisible: () => wm != null && wm.HasFactory,
                        color: UIRoot.BgButtonSuccess),
                    FacilityButtonSpec.Create("\uD83D\uDD27 Perbaiki Pabrik (Rp 25 Jt)",
                        () => bm?.RepairFactory(),
                        isVisible: () => bm != null && bm.IsFactoryDamaged,
                        color: UIRoot.BgButtonDanger),
                }
            };
        }

        // ── Yayasan CSR ──────────────────────────────────────────────────

        private static FacilityDefinition GetYayasan()
        {
            var bm = BuildingManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83C\uDF93 Yayasan Pendidikan CSR",
                getDescription = () =>
                {
                    if (bm == null) return "";
                    return $"Status Yayasan: {(bm.HasFoundation ? "Sudah Berdiri" : "Belum Dibangun")}\nBonus: Semua pekerja baru mendapat +10 Intelligence.";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\uD83C\uDF93 Danai Yayasan CSR (Rp 80 Jt)",
                        () => bm?.BuildFoundation(),
                        isVisible: () => bm != null && !bm.HasFoundation,
                        color: UIRoot.BgButtonSuccess),
                }
            };
        }

        // ── Billboard Sepupu ──────────────────────────────────────────────

        private static FacilityDefinition GetSepupu()
        {
            var rival = RivalManager.Instance;
            var econ = EconomyManager.Instance;
            return new FacilityDefinition
            {
                title = "\uD83D\uDCCA Billboard Saingan Sepupu",
                getDescription = () =>
                {
                    if (rival == null || econ == null) return "";
                    double playerNW = econ.GetNetWorth();
                    double cousinNW = rival.CousinCurrentNetWorth;
                    bool isLeading = playerNW >= cousinNW;
                    return $"Net Worth Anda: {EconomyManager.FormatCurrency(playerNW)}\nNet Worth Sepupu: {EconomyManager.FormatCurrency(cousinNW)}\nStatus: {(isLeading ? "Anda sedang MEMIMPIN!" : "Anda TERTINGGAL dari sepupu!")}\nBatas Evaluasi: Tahun {rival.TargetEvaluationYear}";
                },
                buttons = new List<FacilityButtonSpec>
                {
                    FacilityButtonSpec.Create("\u23E9 Test Evaluasi Ending Sekarang",
                        () => rival?.EvaluateTenYearDeadline()),
                }
            };
        }
    }
}
