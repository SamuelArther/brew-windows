class Snzip < Formula
  desc "Compression/decompression tool based on snappy"
  homepage "https://github.com/kubo/snzip"
  url "https://github.com/kubo/snzip/releases/download/v1.0.5/snzip-1.0.5-win64.zip"
  sha256 "e467e70e0589c87f07dc427ec544d4be1a7211ee62b134d8d66c89fb64968274"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kubo/snzip/releases/download/v1.0.5/snzip-1.0.5.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/snunzip.exe", "snunzip"
  end

  test do
    system bin/"snunzip", "-h"
  end
end
