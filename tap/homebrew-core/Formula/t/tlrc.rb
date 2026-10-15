class Tlrc < Formula
  desc "Official tldr client written in Rust"
  homepage "https://tldr.sh/tlrc/"
  url "https://github.com/tldr-pages/tlrc/releases/download/v1.13.1/tlrc-v1.13.1-x86_64-pc-windows-msvc.zip"
  sha256 "8b85477c935365008214be7a5fd319c56855175ff0c4e8cc2d01fa94accdd210"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tldr-pages/tlrc/archive/refs/tags/v1.13.1.tar.gz"
  end

  conflicts_with "tealdeer", because: "both install `tldr` binaries"

  def install
    bin.install "tldr.exe"
  end

  test do
    assert_match "brew", shell_output("#{bin}/tldr brew")
  end
end
