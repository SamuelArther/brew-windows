class Lnk < Formula
  desc "Git-native dotfiles management that doesn't suck"
  homepage "https://github.com/yarlson/lnk"
  url "https://github.com/yarlson/lnk/releases/download/v0.9.1/lnk_Windows_x86_64.tar.gz"
  sha256 "0d950aa9bd5a9cce3dc105e463459b43c84dafef4128155cc26cdcd16a3aad05"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yarlson/lnk/archive/refs/tags/v0.9.1.tar.gz"
  end

  def install
    bin.install "lnk.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lnk --version")
    assert_match "Lnk repository not initialized", shell_output("#{bin}/lnk list 2>&1", 1)
  end
end
