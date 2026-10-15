class Nss < Formula
  desc "Libraries for security-enabled client and server applications"
  homepage "https://firefox-source-docs.mozilla.org/security/nss/index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nss-3.131-1-any.pkg.tar.zst"
  sha256 "01da502b6ccc9ce4dfbd251baa6532328a21aedbf22328f208e76327653047c4"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftp.mozilla.org/pub/security/nss/releases/"
    regex(%r{href=.*?NSS[._-]v?(\d+(?:[._]\d+)+)[._-]RTM/?["' >]}i)
    strategy :page_match do |page, regex|
      page.scan(regex).map { |match| match.first.tr("_", ".") }
    end
  end

  resource "mingw-w64-ucrt-x86_64-nspr" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nspr-4.40-1-any.pkg.tar.zst"
    sha256 "35f0f60025cb0d20039f3d2c59df744a9308fe264c4606d49685e87efce5984a"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-tcl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tcl-8.6.18-1-any.pkg.tar.zst"
    sha256 "0c3a50ea5be8c5190e58a1fdc46c09f8113ad1b855745875646b7a78a02215df"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/certutil.exe", "certutil"
    msys2_bin "ucrt64/bin/cmsutil.exe", "cmsutil"
    msys2_bin "ucrt64/bin/crlutil.exe", "crlutil"
    msys2_bin "ucrt64/bin/modutil.exe", "modutil"
    msys2_bin "ucrt64/bin/pk12util.exe", "pk12util"
    msys2_bin "ucrt64/bin/signtool.exe", "signtool"
    msys2_bin "ucrt64/bin/signver.exe", "signver"
    msys2_bin "ucrt64/bin/ssltap.exe", "ssltap"
  end

  test do
    assert_path_exists bin/"certutil.exe"
  end
end
