# HypoXia: Pulau Halusinasi

Game 3D co-op puzzle-adventure *first-person* untuk dua pemain. Dua peneliti menyelidiki
rumor hantu di bekas fasilitas tambang pulau vulkanik, tanpa sadar sistem ventilasi rusak
dan gas karbon monoksida (CO) perlahan merusak persepsi mereka. Satu objek fisik yang sama
bisa tampak berbeda di layar masing-masing pemain — hanya komunikasi yang bisa memisahkan
yang nyata dari halusinasi.

Tidak ada monster dan tidak ada pertempuran. Ketegangan lahir dari ketidakpercayaan pada
indra sendiri.

## Persyaratan

| | |
|---|---|
| Engine | Unity **6000.6.6f1** |
| Render pipeline | **URP** 17.6.0 |
| Networking | Netcode for GameObjects **2.13.3** |
| Input | Input System 1.20.0 |
| Platform target | PC standalone (Windows/Linux), 2 pemain via LAN |

> Pipeline yang dipakai proyek ini **URP**, bukan HDRP. Lihat *Deviasi dari GDD* di bawah.

## Menjalankan

1. Buka folder ini lewat Unity Hub dengan editor 6000.6.6f1.
2. Buka scene `Assets/Scenes/SampleScene.unity`.
3. Tekan **Play**. Menu LAN muncul di layar.
4. Pemain pertama menekan **Host Sesi (LAN)**.
5. Pemain kedua menjalankan instance lain, mengisi IP host, lalu menekan **Gabung Sesi (LAN)**.

Untuk menguji dua pemain di satu PC gunakan **Multiplayer Play Mode** (MPPM) bawaan Unity 6,
atau jalankan instance kedua dari sebuah build.

Port default: **7777/UDP**, listen di semua adapter LAN.

## Kontrol

| Aksi | Input |
|---|---|
| Bergerak | `W` `A` `S` `D` |
| Melihat | Mouse |
| Berlari | `Shift` (tahan) |
| Memakai tabung oksigen | `Q` |
| Senter | `F` |
| Debug band CO (build demo saja) | `F1` `F2` `F3` |
| Buka/tutup kunci kursor | `Esc` |

`E` (interaksi) belum aktif — masuk Sprint 2 bersama story B2.

## Mekanik Paparan CO

Meteran paparan berskala 0–100 (skala mekanik game, bukan persentase COHb).

| Rentang | Status |
|---|---|
| 0–49 | Normal |
| 50–74 | Terdistorsi |
| 75–99 | Halusinasi berat |
| 100 | Pingsan — sesi gagal, mulai ulang dari checkpoint |

Laju paparan:

| Kondisi | Laju |
|---|---|
| Ruang tertutup (pasif) | 0,12 %/detik |
| Berlari | 2× laju jalan |
| Dekat pipa bocor | +0,30 %/detik |
| Ruang terbuka | −0,10 %/detik (pulih) |
| Tabung oksigen | −20 % per tabung, maks. 3 |

**Rian** (host) terkena paparan 20% lebih lambat (pengali 0,8); **Maya** (client) laju standar.
Keduanya berbagi satu model dan dibedakan warna pakaian.

HUD sengaja tidak menampilkan angka: bilah paparan hanya berubah warna pada ambang **50%**
dan **75%**, supaya kepanikan terasa organik.

## Struktur Proyek

```
Assets/
├── Prefabs/Player.prefab          Player prefab jaringan (NetworkObject + NetworkTransform)
├── Scenes/SampleScene.unity       Scene gameplay; root Hypoxia_Network + Hypoxia_Greybox
├── Scripts/
│   ├── Exposure/                  ExposureCalculator, PlayerExposure, ExposureZone, tes
│   ├── UI/HypoxiaHUD.cs           HUD IMGUI: menu LAN, bilah paparan, game over
│   └── OwnerPlayerMovement.cs     Gerak owner-authoritative
└── DefaultNetworkPrefabs.asset    Daftar prefab jaringan

Packages/manifest.json             Dependensi UPM
ProjectSettings/                   Pengaturan Unity
```

Semua objek dunia memakai primitif greybox. Aset final masuk pada Sprint 5 (story G2).

## Status Sprint

| Sprint | Milestone | Story | Poin | Status |
|---|---|---|---|---|
| 1 | — | A1, A2, A3, A4, D1 | 22 | ✅ Selesai |
| 2 | Alpha | G1, B1, B2, E3 | 23 | Berikutnya |
| 3 | — | C1, C2, E4, E5, E6 | 23 | — |
| 4 | Beta | C3, E1, E2, E7 | 24 | — |
| 5 | — | F1, F2, G2 | 15 | — |
| 6 | Release | G3 | 5 | — |

**Sprint 1** — paparan CO naik-turun sesuai konfigurasi (A1), bilah HUD berubah pada ambang
50 dan 75 (A2), tabung oksigen memulihkan 20% (A3), sesi gagal di 100% dan bisa diulang dari
checkpoint (A4), serta host/join LAN dua pemain dengan posisi dan nilai CO tersinkron (D1).

**Sprint 2** — greybox tiga area berurutan (Dermaga, Laboratorium, Poros & Stasiun) plus
gerbang sekat (G1), objek yang sama tampil berbeda di dua layar mengikuti band CO (B1),
interaksi dikirim ke host dan diverifikasi di objek asli (B2), occlusion culling pada dinding
statis (E3).

## Tes

`Assets/Scripts/Exposure/ExposureCalculatorTests.cs` berisi pemeriksaan berbasis `Debug.Assert`
yang dijalankan otomatis saat Editor dimuat (`[InitializeOnLoadMethod]`). Kegagalan tampil di
Console. Tidak memakai framework tes tambahan.

## Deviasi dari GDD

| GDD | Kenyataan | Alasan |
|---|---|---|
| HDRP | URP 17.6.0 | Sudah menjadi pipeline proyek. Efek Volume per band (E1) dan ray tracing refleksi (F1) akan diadaptasi ke URP. |
| DirectX 12 | Dikembangkan di Linux | Klausa DX12 pada kriteria D1 tidak berlaku; sinkronisasi LAN tetap diverifikasi. |
| Voice chat in-game | Discord | D2 berstatus opsional dan dipotong sesuai strategi *Buffer & Scope Pruning* GDD Bab 18. |

## Aturan Desain yang Tidak Boleh Dilanggar

Pemetaan ulang wujud objek (halusinasi) **hanya menyentuh lapisan visual dan audio**. Tidak
pernah collider, *interaction hitbox*, atau state logika game. Semua verifikasi logika teka-teki
dilakukan di host. Jika collider ikut berubah, teka-teki menjadi mustahil diselesaikan.

## Kontributor

Johan Firmansyah (202310370311488) — Informatika, Universitas Muhammadiyah Malang.
Mata kuliah Pemrograman Game Lanjut dan Rekayasa Game.