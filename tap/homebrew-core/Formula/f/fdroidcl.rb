class Fdroidcl < Formula
  desc "F-Droid desktop client"
  homepage "https://github.com/Hoverth/fdroidcl"
  url "https://github.com/Hoverth/fdroidcl/releases/download/v0.8.1/fdroidcl_v0.8.1_windows_amd64.zip"
  sha256 "eb874f350203e1bfd9def97cb823953a2aaad0f2cc4cc50201f1e48cecc83c94"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Hoverth/fdroidcl/archive/refs/tags/v0.8.1.tar.gz"
  end

  def install
    bin.install "fdroidcl_v0.8.1_windows_amd64.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fdroidcl_v0.8.1_windows_amd64 version 2>&1")
  end
end
