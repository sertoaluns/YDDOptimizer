using System;
using System.Collections.Generic;

namespace YddOptimizer.Core
{
    public static class Localization
    {
        public static string CurrentLang = "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new Dictionary<string, Dictionary<string, string>>
        {
            ["tr"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "GTA V YDD Optimizer & LOD Generator",
                ["TabOptimization"] = "📁 Optimizasyon",
                ["TabSettings"] = "⚙️ Ayarlar",
                ["TabGuide"] = "ℹ️ Kullanım Kılavuzu",

                ["GrpMode"] = " Çalışma Modu ",
                ["ModeSingle"] = "Tek Dosya Modu (.ydd)",
                ["ModeBatch"] = "Toplu Klasör Modu (Batch)",

                ["GrpPaths"] = " Dosya ve Çıktı Yolu ",
                ["LblInputFile"] = "Girdi Dosyası:",
                ["LblInputFolder"] = "Girdi Klasörü:",
                ["LblOutputFile"] = "Çıktı Dosyası:",
                ["LblOutputFolder"] = "Çıktı Klasörü:",
                ["BtnBrowse"] = "Gözat...",

                ["GrpStats"] = " Model Bilgileri & Tahmini Bellek ",
                ["StatOrigTris"] = "Orijinal Hi Üçgen: {0:N0}",
                ["StatOrigMem"] = "Tahmini Bellek: {0:0.##} MiB",
                ["StatResultMem"] = "Sonuç Bellek: ~{0:0.##} MiB",
                ["StatBatchCount"] = "Toplu Mod: {0} YDD dosyası",

                ["GrpLod"] = " LOD ve Küçültme Ayarları (Yüzde ↔ Üçgen Sayısı Bağlantılı) ",
                ["LodHigh"] = "High LOD:",
                ["LodMed"] = "Medium LOD:",
                ["LodLow"] = "Low LOD:",
                ["LodVLow"] = "Very Low LOD:",
                ["LblTris"] = "Üçgen:",

                ["GrpColors"] = " Vertex Color Düzenleme ",
                ["Col0Edit"] = "Colour0 Düzenle:",
                ["Col1Edit"] = "Colour1 Düzenle:",
                ["ColWhite"] = "Beyaz Yap (255, 255, 255, 255)",
                ["ColBlack"] = "Siyah Yap (0, 0, 0, 255)",
                ["ColAlpha255"] = "Alpha'yı 255 Yap (Opak)",
                ["ColCustom"] = "Özel Renk Seç...",
                ["BtnColorPick"] = "Renk...",

                ["ChkPreviewToggle"] = " 3D Önizleme Panelini Aç",
                ["BtnOptimize"] = "⚡ Optimize Et ve Kaydet",
                ["StatusReady"] = "Hazır.",
                ["StatusProcessing"] = "İşleniyor: {0}/{1}...",
                ["StatusDone"] = "Tamamlandı! {0} başarılı, {1} hatalı.",

                ["GrpGeneralSettings"] = " Genel Ayarlar & Dosya Yolları ",
                ["LblLanguage"] = "Uygulama Dili (Language):",
                ["LblGta5Path"] = "GTA 5 Oyun Klasörü Yolu:",
                ["LblCodeWalkerPath"] = "CodeWalker Klasörü / DLL Yolu:",
                ["BtnAutoDetect"] = "Otomatik Bul",
                ["BtnSaveSettings"] = "💾 Ayarları Kaydet",
                ["MsgSettingsSaved"] = "Ayarlar başarıyla kaydedildi.",

                ["PreviewLod"] = "LOD:",
                ["PreviewMode"] = "Görünüm:",
                ["PreviewReset"] = "Kamerayı Sıfırla",
                ["PreviewModeSolidWire"] = "Dolu + Tel Kafes",
                ["PreviewModeSolid"] = "Dolu (Solid)",
                ["PreviewModeWire"] = "Tel Kafes (Wireframe)",

                ["GuideText"] = @"# GTA V YDD Optimizer & LOD Generator – Kullanım Kılavuzu

Bu araç, GTA 5 oyunundaki `.ydd` (drawable dictionary) 3D model dosyalarının boyutunu düşürmek, oyun içi performansını artırmak ve FiveM/Singleplayer ortamlarında streaming bellek uyarılarını engellemek için tasarlanmıştır.

---

### 1. Temel İşlevler
- **Tek Dosya Modu:** Tek bir `.ydd` dosyasını seçip küçülterek çıktıyı `[dosyaadı]_resized.ydd` olarak kaydeder.
- **Toplu Klasör Modu:** Seçilen klasörün altındaki tüm `.ydd` dosyalarını otomatik bulur ve `_resized` klasörüne hiyerarşiyi bozmadan optimize ederek aktarır.
- **Sürükle & Bırak:** Dosyayı veya klasörü doğrudan pencerenin üzerine bırakabilirsiniz.

---

### 2. LOD ve Yüzde ↔ Üçgen Senkronizasyonu
GTA 5 modellerinde 4 LOD (Seviye) bulunur:
1. **High (Yakın):** Oyuncunun modele en yakın olduğu anki ana model.
2. **Medium (Orta):** Orta mesafede yüklenir (Önerilen: %20 - %30).
3. **Low (Uzak):** Uzak mesafede yüklenir (Önerilen: %8 - %15).
4. **Very Low (Çok Uzak):** Çok uzak mesafede yüklenir (Önerilen: %3 - %5).

> **İpucu:** Yüzde kutusuna değer girdiğinizde hedef üçgen sayısı anında hesaplanır. Üçgen sayısı kutusuna manuel bir sayı yazdığınızda ise yüzde otomatik olarak güncellenir.

---

### 3. Vertex Color (Colour0 / Colour1) Düzenleme
GTA 5 kıyafet, saç ve prop modellerinde vertex color kanalları gölgelendirme (AO), renk tonlama ve şeffaflık maskeleri için kullanılır:
- **Beyaz Yap:** Tüm vertex renklerini saf beyaz (#FFFFFFFF) yaparak kararmış veya yanlış tonlanmış modelleri düzeltir.
- **Siyah Yap:** Vertexleri siyaha çekerek koyu efektler sağlar.
- **Alpha'yı 255 Yap:** Modelin RGB rengini korurken alpha kanalını tamamen opak hale getirir.
- **Özel Renk:** Paletten seçeceğiniz herhangi bir rengi tüm vertexlere uygular.

---

### 4. 3D Önizleme Kontrolleri
- **Açma/Kapatma:** '3D Önizleme Panelini Aç' kutusunu işaretlediğinizde sağ tarafta 3D pencere açılır.
- **Sol Fare Tuşu + Sürükle:** Modeli serbestçe 360 derece döndürür.
- **Sağ Fare Tuşu + Sürükle:** Modeli ekranda sağa-sola veya yukarı-aşağı kaydırır (Pan).
- **Fare Tekerleği:** Modele yakınlaşır ve uzaklaşır (Zoom).
- **Çift Tıklama:** Kamerayı başlangıç konumuna sıfırlar.
- **LOD Seçici:** Farklı LOD seviyelerindeki üçgen yoğunluğunu canlı inceleyebilirsiniz."
            },

            ["en"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "GTA V YDD Optimizer & LOD Generator",
                ["TabOptimization"] = "📁 Optimization",
                ["TabSettings"] = "⚙️ Settings",
                ["TabGuide"] = "ℹ️ User Guide",

                ["GrpMode"] = " Operating Mode ",
                ["ModeSingle"] = "Single File Mode (.ydd)",
                ["ModeBatch"] = "Batch Folder Mode",

                ["GrpPaths"] = " File and Output Paths ",
                ["LblInputFile"] = "Input File:",
                ["LblInputFolder"] = "Input Folder:",
                ["LblOutputFile"] = "Output File:",
                ["LblOutputFolder"] = "Output Folder:",
                ["BtnBrowse"] = "Browse...",

                ["GrpStats"] = " Model Info & Memory Estimate ",
                ["StatOrigTris"] = "Original Hi Tris: {0:N0}",
                ["StatOrigMem"] = "Estimated Memory: {0:0.##} MiB",
                ["StatResultMem"] = "Result Memory: ~{0:0.##} MiB",
                ["StatBatchCount"] = "Batch Mode: {0} YDD files",

                ["GrpLod"] = " LOD & Decimation Settings (Bidirectional Percent ↔ Tris) ",
                ["LodHigh"] = "High LOD:",
                ["LodMed"] = "Medium LOD:",
                ["LodLow"] = "Low LOD:",
                ["LodVLow"] = "Very Low LOD:",
                ["LblTris"] = "Tris:",

                ["GrpColors"] = " Vertex Color Editing ",
                ["Col0Edit"] = "Edit Colour0:",
                ["Col1Edit"] = "Edit Colour1:",
                ["ColWhite"] = "Set White (255, 255, 255, 255)",
                ["ColBlack"] = "Set Black (0, 0, 0, 255)",
                ["ColAlpha255"] = "Set Alpha 255 (Opaque)",
                ["ColCustom"] = "Custom Color...",
                ["BtnColorPick"] = "Color...",

                ["ChkPreviewToggle"] = " Open 3D Preview Panel",
                ["BtnOptimize"] = "⚡ Optimize & Save",
                ["StatusReady"] = "Ready.",
                ["StatusProcessing"] = "Processing: {0}/{1}...",
                ["StatusDone"] = "Completed! {0} succeeded, {1} failed.",

                ["GrpGeneralSettings"] = " General Settings & Directories ",
                ["LblLanguage"] = "Application Language:",
                ["LblGta5Path"] = "GTA V Game Directory:",
                ["LblCodeWalkerPath"] = "CodeWalker Directory / DLL Path:",
                ["BtnAutoDetect"] = "Auto Detect",
                ["BtnSaveSettings"] = "💾 Save Settings",
                ["MsgSettingsSaved"] = "Settings saved successfully.",

                ["PreviewLod"] = "LOD:",
                ["PreviewMode"] = "View:",
                ["PreviewReset"] = "Reset Camera",
                ["PreviewModeSolidWire"] = "Solid + Wireframe",
                ["PreviewModeSolid"] = "Solid",
                ["PreviewModeWire"] = "Wireframe",

                ["GuideText"] = @"# GTA V YDD Optimizer & LOD Generator – User Guide

This tool is designed to optimize GTA V `.ydd` (drawable dictionary) 3D model files, drastically reduce memory usage, and eliminate streaming memory warnings in FiveM and Singleplayer.

---

### 1. Core Features
- **Single File Mode:** Select a single `.ydd` file, reduce its complexity and save it with `_resized.ydd` suffix.
- **Batch Folder Mode:** Recursively scans a selected directory for all `.ydd` files and saves the optimized models into a `_resized` subfolder while preserving folder hierarchy.
- **Drag & Drop:** Simply drag and drop any file or folder onto the window.

---

### 2. LODs and Percent ↔ Triangle Synchronization
GTA 5 models utilize 4 distinct LOD levels:
1. **High (Close):** Main model viewed up close.
2. **Medium:** Loaded at intermediate distances (Recommended: 20% - 30%).
3. **Low:** Loaded far away (Recommended: 8% - 15%).
4. **Very Low:** Loaded at extreme distances (Recommended: 3% - 5%).

> **Tip:** Entering a percentage immediately calculates the target triangle count. Conversely, typing a specific triangle count automatically updates the percentage.

---

### 3. Vertex Color (Colour0 / Colour1) Editing
GTA 5 clothing, props, and hair models utilize vertex color channels for ambient occlusion, tinting, and transparency masks:
- **Set White:** Sets all vertex colors to pure white (#FFFFFFFF), fixing darkened or incorrectly shaded models.
- **Set Black:** Darkens vertices for shadow effects.
- **Set Alpha 255:** Retains existing RGB colors while making the alpha channel completely opaque.
- **Custom Color:** Assigns any custom RGBA color to all vertices.

---

### 4. 3D Interactive Preview Controls
- **Toggle:** Check 'Open 3D Preview Panel' to expand the viewport on the right.
- **Left Mouse Button + Drag:** Orbit camera 360° around the model.
- **Right Mouse Button + Drag:** Pan camera horizontally and vertically.
- **Mouse Wheel:** Zoom in and out.
- **Double Click:** Reset camera position.
- **LOD Selector:** Switch between LOD levels to inspect mesh decimation live."
            },

            ["de"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "GTA V YDD Optimizer & LOD Generator",
                ["TabOptimization"] = "📁 Optimierung",
                ["TabSettings"] = "⚙️ Einstellungen",
                ["TabGuide"] = "ℹ️ Benutzerhandbuch",

                ["GrpMode"] = " Betriebsmodus ",
                ["ModeSingle"] = "Einzeldazei-Modus (.ydd)",
                ["ModeBatch"] = "Batch-Ordner-Modus",

                ["GrpPaths"] = " Datei- und Ausgabepfade ",
                ["LblInputFile"] = "Eingabedatei:",
                ["LblInputFolder"] = "Eingabeordner:",
                ["LblOutputFile"] = "Ausgabedatei:",
                ["LblOutputFolder"] = "Ausgabeordner:",
                ["BtnBrowse"] = "Durchsuchen...",

                ["GrpStats"] = " Modellinfo & Geschätzter Speicher ",
                ["StatOrigTris"] = "Original Hi Dreiecke: {0:N0}",
                ["StatOrigMem"] = "Geschätzter Speicher: {0:0.##} MiB",
                ["StatResultMem"] = "Ergebnis-Speicher: ~{0:0.##} MiB",
                ["StatBatchCount"] = "Batch-Modus: {0} YDD-Dateien",

                ["GrpLod"] = " LOD & Dezimierungs-Einstellungen (Prozent ↔ Dreiecke) ",
                ["LodHigh"] = "High LOD:",
                ["LodMed"] = "Medium LOD:",
                ["LodLow"] = "Low LOD:",
                ["LodVLow"] = "Very Low LOD:",
                ["LblTris"] = "Dreiecke:",

                ["GrpColors"] = " Vertex-Farbbearbeitung ",
                ["Col0Edit"] = "Colour0 bearbeiten:",
                ["Col1Edit"] = "Colour1 bearbeiten:",
                ["ColWhite"] = "Weiß setzen (255, 255, 255, 255)",
                ["ColBlack"] = "Schwarz setzen (0, 0, 0, 255)",
                ["ColAlpha255"] = "Alpha auf 255 setzen (Opak)",
                ["ColCustom"] = "Benutzerdefinierte Farbe...",
                ["BtnColorPick"] = "Farbe...",

                ["ChkPreviewToggle"] = " 3D-Vorschaufenster öffnen",
                ["BtnOptimize"] = "⚡ Optimieren & Speichern",
                ["StatusReady"] = "Bereit.",
                ["StatusProcessing"] = "Verarbeitung: {0}/{1}...",
                ["StatusDone"] = "Abgeschlossen! {0} erfolgreich, {1} fehlgeschlagen.",

                ["GrpGeneralSettings"] = " Allgemeine Einstellungen & Pfade ",
                ["LblLanguage"] = "Anwendungssprache (Language):",
                ["LblGta5Path"] = "GTA V Spielverzeichnis:",
                ["LblCodeWalkerPath"] = "CodeWalker Verzeichnis / DLL-Pfad:",
                ["BtnAutoDetect"] = "Automatisch erkennen",
                ["BtnSaveSettings"] = "💾 Einstellungen speichern",
                ["MsgSettingsSaved"] = "Einstellungen erfolgreich gespeichert.",

                ["PreviewLod"] = "LOD:",
                ["PreviewMode"] = "Ansicht:",
                ["PreviewReset"] = "Kamera zurücksetzen",
                ["PreviewModeSolidWire"] = "Fläche + Drahtgitter",
                ["PreviewModeSolid"] = "Fläche (Solid)",
                ["PreviewModeWire"] = "Drahtgitter (Wireframe)",

                ["GuideText"] = @"# GTA V YDD Optimizer & LOD Generator – Benutzerhandbuch

Dieses Tool optimiert GTA V `.ydd` (Drawable Dictionary) 3D-Modelldateien, reduziert drastisch den Streaming-Speicherbedarf und verhindert Abstürze und Ruckler in FiveM und im Einzelspieler.

---

### 1. Grundfunktionen
- **Einzeldatei-Modus:** Wählt eine `.ydd`-Datei aus, optimiert die Geometrie und speichert das Ergebnis als `_resized.ydd`.
- **Batch-Ordner-Modus:** Durchsucht automatisch alle Unterordner nach `.ydd`-Dateien und optimiert sie in einen `_resized`-Ordner mit identischer Struktur.
- **Drag & Drop:** Dateien oder Ordner einfach direkt auf das Programmfenster ziehen.

---

### 2. LODs und Bidirektionale Prozent ↔ Dreieckssynchronisation
GTA 5 Modelle verwenden 4 LOD-Stufen:
1. **High (Nahbereich):** Hauptmodell in Spielernähe.
2. **Medium:** Mittlere Entfernung (Empfohlen: 20% - 30%).
3. **Low:** Weite Entfernung (Empfohlen: 8% - 15%).
4. **Very Low:** Sehr weite Entfernung (Empfohlen: 3% - 5%).

> **Tipp:** Wenn Sie einen Prozentsatz eingeben, wird die Dreiecksanzahl sofort berechnet. Die manuelle Eingabe einer Dreiecksanzahl aktualisiert den Prozentsatz automatisch.

---

### 3. Vertex-Farben (Colour0 / Colour1)
GTA 5 Kleidungs-, Requisiten- und Haarmodelle nutzen Vertex-Farben für Ambient Occlusion, Tönung und Alpha-Masken:
- **Weiß setzen:** Setzt alle Vertex-Farben auf reines Weiß (#FFFFFFFF) und repariert fehlerhaft abgedunkelte Modelle.
- **Schwarz setzen:** Dunkelt Vertices ab.
- **Alpha auf 255 setzen:** Erhält die RGB-Farben und setzt den Alpha-Kanal auf vollständig deckend.
- **Benutzerdefinierte Farbe:** Weist allen Vertices eine gewählte RGBA-Farbe zu.

---

### 4. 3D-Vorschau Steuerung
- **Umschalten:** Aktivieren Sie '3D-Vorschaufenster öffnen', um das 3D-Fenster rechts anzuzeigen.
- **Linke Maustaste + Ziehen:** Modell frei um 360° drehen.
- **Rechte Maustaste + Ziehen:** Modell verschieben (Pan).
- **Mausrad:** Zoom (Heran- / Wegzoomen).
- **Doppelklick:** Kamera zurücksetzen.
- **LOD-Auswahl:** Dreiecksdichte der einzelnen LOD-Stufen direkt live prüfen."
            }
        };

        public static string Get(string key)
        {
            if (Strings.TryGetValue(CurrentLang, out var dict) && dict.TryGetValue(key, out var val))
                return val;

            if (Strings["en"].TryGetValue(key, out var fallback))
                return fallback;

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string fmt = Get(key);
            try { return string.Format(fmt, args); }
            catch { return fmt; }
        }
    }
}
