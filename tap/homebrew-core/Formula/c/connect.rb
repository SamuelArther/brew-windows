class Connect < Formula
  desc "Provides SOCKS and HTTPS proxy support to SSH"
  homepage "https://github.com/gotoh/ssh-connect"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-connect-1.105-5-any.pkg.tar.zst"
  sha256 "e3ee5d2a383c2fca3cc608baea39c57844a8525ceeae0b0f415d7ea441f6aad5"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/gotoh/ssh-connect/archive/refs/tags/1.106.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/connect.exe", "connect"
  end

  test do
    system bin/"connect"
  end
end
