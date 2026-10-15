class Sad < Formula
  desc "CLI search and replace | Space Age seD"
  homepage "https://github.com/ms-jpq/sad"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sad-0.4.32-1-any.pkg.tar.zst"
  sha256 "030f1bd86e1e8fe80c2fb8b02dae9028211701db95430ae535f949a799135e4d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/ms-jpq/sad/archive/refs/tags/v0.4.32.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/sad.exe", "sad"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sad --version 2>&1")
  end
end
