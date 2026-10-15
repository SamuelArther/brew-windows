class Nbping < Formula
  desc "Ping Tool in Rust with Real-Time Data and Visualizations"
  homepage "https://github.com/hanshuaikang/NBping"
  url "https://github.com/hanshuaikang/NBping/releases/download/v0.7.1/nbping-x86_64-pc-windows-msvc.zip"
  sha256 "973fdf04028f7a89893caf607c0706c8d5ab7f4a67c4bfaf4d1677c46c811543"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hanshuaikang/NBping/archive/refs/tags/v0.7.1.tar.gz"
  end

  def install
    bin.install "nbping.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nbping --version")

    # Fails in Linux CI with "No such device or address (os error 2)"
    return if OS.linux? && ENV["HOMEBREW_GITHUB_ACTIONS"]

    system bin/"nbping", "--count", "2", "brew.sh"
  end
end
