class Diskus < Formula
  desc "Minimal, fast alternative to 'du -sh'"
  homepage "https://github.com/sharkdp/diskus"
  url "https://github.com/sharkdp/diskus/releases/download/v0.9.0/diskus-v0.9.0-x86_64-pc-windows-gnu.zip"
  sha256 "80f116817cb30c7f748424b605a2990471afb2fef06b0cce91468c6182f6d69c"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sharkdp/diskus/archive/refs/tags/v0.9.0.tar.gz"
  end

  def install
    bin.install "diskus.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/diskus --version 2>&1")
  end
end
