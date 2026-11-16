# -*- coding: utf-8 -*-
import argparse
import json
from datetime import datetime, timedelta

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--connection", required=True)
    p.add_argument("--png", required=True)
    p.add_argument("--json", required=True)
    args = p.parse_args()

    import pyodbc
    import pandas as pd
    import matplotlib.pyplot as plt

    con = pyodbc.connect(args.connection, autocommit=True)

    since = datetime.utcnow() - timedelta(days=30)

    q_moves = '''
    SELECT
        h.Tarih,
        h.HareketTuru,
        h.Miktar,
        i.IlacAdi
    FROM StokHareketleri h
    INNER JOIN Ilaclar i ON i.IlacId = h.IlacId
    WHERE h.Tarih >= ?
    ORDER BY h.Tarih;
    '''
    df = pd.read_sql(q_moves, con, params=[since])

    q_kritik = '''
    SELECT COUNT(*) AS KritikAdet
    FROM Ilaclar
    WHERE StokAdedi < KritikSeviye;
    '''
    kritik = int(pd.read_sql(q_kritik, con).iloc[0]["KritikAdet"])

    q_top = '''
    SELECT TOP 10 i.IlacAdi, SUM(h.Miktar) AS CikisToplam
    FROM StokHareketleri h
    INNER JOIN Ilaclar i ON i.IlacId = h.IlacId
    WHERE h.Tarih >= ? AND h.HareketTuru = 2
    GROUP BY i.IlacAdi
    ORDER BY CikisToplam DESC;
    '''
    top = pd.read_sql(q_top, con, params=[since])

    if len(df) > 0:
        df["Gun"] = pd.to_datetime(df["Tarih"]).dt.date
        giris = df[df["HareketTuru"] == 1].groupby("Gun")["Miktar"].sum().rename("Giris")
        cikis = df[df["HareketTuru"] == 2].groupby("Gun")["Miktar"].sum().rename("Cikis")
        trend = pd.concat([giris, cikis], axis=1).fillna(0).reset_index()
    else:
        import pandas as pd
        trend = pd.DataFrame({"Gun": [], "Giris": [], "Cikis": []})

    plt.figure(figsize=(12, 6))

    ax1 = plt.subplot(2, 1, 1)
    if len(trend) > 0:
        ax1.plot(trend["Gun"], trend["Giris"], label="Giriş")
        ax1.plot(trend["Gun"], trend["Cikis"], label="Çıkış")
        ax1.set_title("Son 30 Gün Stok Hareketi (Günlük)")
        ax1.legend()
        ax1.grid(True, alpha=0.3)
        ax1.tick_params(axis='x', rotation=20)
    else:
        ax1.text(0.5, 0.5, "Son 30 günde hareket yok", ha="center", va="center")
        ax1.set_axis_off()

    ax2 = plt.subplot(2, 1, 2)
    if len(top) > 0:
        ax2.bar(top["IlacAdi"], top["CikisToplam"])
        ax2.set_title("En Çok Çıkan 10 İlaç (Son 30 Gün)")
        ax2.tick_params(axis='x', rotation=20)
        ax2.grid(True, axis="y", alpha=0.3)
    else:
        ax2.text(0.5, 0.5, "Çıkış verisi yok", ha="center", va="center")
        ax2.set_axis_off()

    plt.tight_layout()
    plt.savefig(args.png, dpi=150)
    plt.close()

    summary = {
        "kritik_stok_adet": kritik,
        "top_cikis": top.to_dict(orient="records"),
        "son_30_gun_hareket_sayisi": int(len(df)),
        "uretim_utc": datetime.utcnow().isoformat() + "Z"
    }
    with open(args.json, "w", encoding="utf-8") as f:
        json.dump(summary, f, ensure_ascii=False, indent=2)

if __name__ == "__main__":
    main()
