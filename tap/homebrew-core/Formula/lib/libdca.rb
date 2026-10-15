class Libdca < Formula
  desc "Library for decoding DTS Coherent Acoustics streams"
  homepage "https://www.videolan.org/developers/libdca.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdca-0.0.7-3-any.pkg.tar.zst"
  sha256 "04cfa8da5c59c89e3c5fb4ba3e1f93d73b79c1273dc8bdf985958dc50c23aa73"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://download.videolan.org/pub/videolan/libdca/"
    regex(%r{href=.*?v?(\d+(?:\.\d+)+)[/"'>]}i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dcadec.exe", "dcadec"
    msys2_bin "ucrt64/bin/extract_dca.exe", "extract_dca"
  end

  test do
    assert_path_exists bin/"dcadec.exe"
  end
end
