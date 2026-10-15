# Upstream ships one zip with builds for every platform; the official Windows x64
# build inside it is x86_64-pc-windows-gnu/broot.exe.
class Broot < Formula
  desc "New way to see and navigate directory trees"
  homepage "https://dystroy.org/broot/"
  version "1.61.0"
  url "https://github.com/Canop/broot/releases/download/v1.61.0/broot_1.61.0.zip"
  sha256 "6cafe8e993fb8e5684eb25549703a4fb9dec61ac6a8f1947cbed6566cb4e58e5"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/x86_64-pc-windows-gnu/broot.exe", "broot"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/broot --version")
  end
end
