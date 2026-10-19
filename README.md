# GTA V YDD Optimizer & LOD Generator

GTA V `.ydd` (drawable dictionary) model dosyaları için otomatik mesh küçültme, LOD (High/Medium/Low/VeryLow) üretme, vertex color düzenleme ve 3D önizleme aracı.

---

## 🚀 Çalıştırma

Programın derlenmiş çalıştırılabilir sürümü:
`bin\Release\YddOptimizer.exe`

Çift tıklayarak doğrudan çalıştırabilirsiniz.

---

## ✨ Özellikler

1. **Tek Dosya ve Toplu (Klasör) Modu:**
   - **Tek Dosya:** Seçtiğiniz `.ydd` dosyasını optimize eder ve sonuna `_resized.ydd` ekleyerek yeni dosya olarak kaydeder.
   - **Toplu Klasör:** Seçtiğiniz klasördeki tüm alt dizinler dahil tüm `.ydd` dosyalarını bulup `_resized` klasörüne aynı klasör hiyerarşisiyle kaydeder.
   - **Sürükle & Bırak:** Dosyayı veya klasörü doğrudan pencereye bırakabilirsiniz.

2. **Çift Yönlü Bağlantılı Yüzde ↔ Üçgen Sayısı:**
   - Model yüklendiğinde High LOD orijinal üçgen sayısı okunur.
   - Yüzdeyi değiştirdiğinizde hedef üçgen sayısı anlık güncellenir.
   - Hedef üçgen sayısını manuel girdiğinizde yüzde anlık güncellenir.

3. **LOD Seviyeleri (High, Medium, Low, Very Low):**
   - Her LOD seviyesi bağımsız olarak aktif/pasif edilebilir.
   - Eksik LOD seviyeleri CodeWalker'a uygun şekilde otomatik oluşturulur, mesafe ve render maskeleri hi modelden aktarılır.

4. **Vertex Color (Colour0 / Colour1) Düzenleme:**
   - Colour0 ve Colour1 kanalları için bağımsız düzenleme:
     - Beyaz Yap (`#FFFFFFFF`)
     - Siyah Yap (`#000000FF`)
     - Alpha'yı 255 Yap (Katı)
     - Özel Renk Seçimi (Renk paleti)

5. **3D İnteraktif Önizleme Paneli:**
   - Performansı korumak için varsayılan olarak kapalıdır.
   - "3D Önizleme Panelini Aç" seçeneği işaretlendiğinde pencere genişler ve sağ tarafta 3D görüntü açılır.
   - Fare Kontrolleri:
     - **Sol Tık + Sürükle:** Modeli 360° serbest döndürür (Orbit).
     - **Sağ Tık + Sürükle:** Modeli sahnede kaydırır (Pan).
     - **Tekerlek:** Yakınlaştırır / Uzaklaştırır (Zoom).
     - **Çift Tık:** Kamerayı sıfırlar.
   - Görünüm Seçenekleri:
     - LOD Seviyesi seçimi (High, Medium, Low, VeryLow).
     - Görüntüleme Modu: Solid + Tel Kafes, Dolu (Solid), Tel Kafes (Wireframe).
