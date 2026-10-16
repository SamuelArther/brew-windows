class Zip < Formula
  desc "Compression and file packaging/archive utility"
  homepage "https://infozip.sourceforge.net/Zip.html"
  url "https://repo.msys2.org/msys/x86_64/zip-3.0-5-x86_64.pkg.tar.zst"
  sha256 "874e20bf625fbe577949444faf30ab9a725dbd4886ec9bff26459152da7f831c"
  license "Info-ZIP"
  revision 1

  # Info-ZIP Zip via MSYS2.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libbz2" do
    url "https://repo.msys2.org/msys/x86_64/libbz2-1.0.8-4-x86_64.pkg.tar.zst"
    sha256 "89fb0e1478b22b80effda55ab4ae7549388d01245d3841a096d8a2d63236c2a1"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/zip.exe", "zip"
    msys2_bin "usr/bin/zipcloak.exe", "zipcloak"
    msys2_bin "usr/bin/zipnote.exe", "zipnote"
    msys2_bin "usr/bin/zipsplit.exe", "zipsplit"
  end

  test do
    (testpath/"test1").write "Hello!"
    system bin/"zip", "test.zip", "test1"
    assert_path_exists testpath/"test.zip"
  end
end
