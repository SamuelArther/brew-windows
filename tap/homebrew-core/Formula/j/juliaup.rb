class Juliaup < Formula
  desc "Julia installer and version multiplexer"
  homepage "https://github.com/JuliaLang/juliaup"
  url "https://github.com/JuliaLang/juliaup/releases/download/v1.22.7/juliaup-1.22.7-x86_64-pc-windows-gnu-portable.tar.gz"
  sha256 "040b688fc39e2f48a7c7c7ee83cbda88131fa6230ed68db24b2a19a40eb39849"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/JuliaLang/juliaup/archive/refs/tags/v1.22.7.tar.gz"
    strategy :github_latest
  end

  conflicts_with "julia", because: "both install `julia` binaries"

  def install
    bin.install "julia.exe"
    bin.install "juliaup.exe"
    bin.install "juliaupgui.exe"
  end

  test do
    assert_path_exists bin/"julia.exe"
  end
end
